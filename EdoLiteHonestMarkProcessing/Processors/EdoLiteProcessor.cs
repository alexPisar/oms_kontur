using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EdiProcessingUnit.Edo.Models;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using UtilitesLibrary.ConfigSet;
using UtilitesLibrary.Logger;
using System.Security.Cryptography.X509Certificates;

namespace EdoLiteHonestMarkProcessing.Processors
{
    public class EdoLiteProcessor
    {
        private const string edoFilesPath = "Files";
        private const string _edoPrefix = "2LT";
        private string _prefixBuyerFileName = "ON_NSCHFDOPPOK";
        internal AbtDbContext _abtDbContext;
        protected UtilityLog _log = UtilityLog.GetInstance();
        protected Config _conf = Config.GetInstance();
        private List<X509Certificate2> _personalCertificates = null;
        private Cryptography.Utils.XmlCertificate _utils;
        private const string _applicationName = "Вирэй Приходная";
        private string _currentVersion => System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString();


        public string EdoProgramVersion => $"{_applicationName} {_currentVersion}";

        public EdoLiteProcessor()
        {
            var client = new System.Net.WebClient();

            if (_conf.ProxyEnabled)
            {
                var webProxy = new System.Net.WebProxy();

                webProxy.Address = new Uri("http://" + _conf.ProxyAddress);
                webProxy.Credentials = new System.Net.NetworkCredential(_conf.ProxyUserName, _conf.ProxyUserPassword);

                client.Proxy = webProxy;
            }
            _utils = new Cryptography.Utils.XmlCertificate(client);
        }

        public void SignDocuments(Kontragent myOrganization)
        {
            var fileController = new WebService.Controllers.FileController();
            var dateTimeLastPeriod = fileController.GetApplicationConfigParameter<DateTime>("KonturEdo", "DocsDateTime");

            var docsByBuyers = (from docJournal in _abtDbContext.DocJournals
                                where docJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && docJournal.IdDocMaster == null
                                join docGoodsI in _abtDbContext.DocGoodsIs on docJournal.Id equals docGoodsI.IdDoc
                                join docEdoProcessing in _abtDbContext.DocEdoProcessings
                                on docJournal.Id equals docEdoProcessing.IdDoc
                                where docEdoProcessing.DocStatus == (int)EdiProcessingUnit.Enums.DocEdoSendStatus.Sent
                                join sellerCustomer in _abtDbContext.RefCustomers on docGoodsI.IdSeller equals sellerCustomer.Id
                                where sellerCustomer.Inn == myOrganization.Inn && sellerCustomer.Kpp == myOrganization.Kpp
                                join buyerCustomer in _abtDbContext.RefCustomers on docEdoProcessing.ReceiverInn equals buyerCustomer.Inn
                                join buyerFnsIdRefTag in _abtDbContext.RefRefTags on buyerCustomer.Id equals buyerFnsIdRefTag.IdObject
                                where buyerFnsIdRefTag.IdTag == 223
                                select new UniversalTransferBuyerDocument
                                {
                                    DocJournal = docJournal,
                                    DocEdoProcessing = docEdoProcessing,
                                    SellerName = sellerCustomer.Name,
                                    SellerInn = sellerCustomer.Inn,
                                    SellerKpp = sellerCustomer.Kpp,
                                    BuyerName = buyerCustomer.Name,
                                    BuyerInn = buyerCustomer.Inn,
                                    BuyerKpp = buyerCustomer.Kpp,
                                    BuyerEdoId = buyerFnsIdRefTag.TagValue
                                }).GroupBy(u => u.BuyerInn).ToList();

            foreach(var docsByBuyer in docsByBuyers)
            {
                var buyerInn = docsByBuyer.Key;

                if (string.IsNullOrEmpty(buyerInn))
                    continue;

                try
                {
                    var certs = GetPersonalCertificates().Where(c => (buyerInn.Length == 10 && buyerInn == _utils.GetOrgInnFromCertificate(c) ||
                    buyerInn.Length == 12 && buyerInn == _utils.GetPersonInnFromCertificate(c)) && _utils.IsCertificateValid(c)).OrderByDescending(c => c.NotBefore);
                    var buyerCertificate = certs.FirstOrDefault();

                    if (buyerCertificate == null)
                        continue;

                    var signerInfo = new Reporter.Entities.SignerInfo
                    {
                        SignType = Reporter.Enums.SignTypeEnum.QualifiedElectronicDigitalSignature,
                        SignDate = DateTime.Now.Date
                    };

                    signerInfo.MethodOfConfirmingAuthorityEnum = Reporter.Enums.MethodOfConfirmingAuthorityEnum.DigitalSignature;

                    signerInfo.Position = _utils.ParseCertAttribute(buyerCertificate.Subject, "T");
                    signerInfo.Surname = _utils.ParseCertAttribute(buyerCertificate.Subject, "SN");
                    var firstMiddleName = _utils.ParseCertAttribute(buyerCertificate.Subject, "G");
                    signerInfo.Name = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
                    signerInfo.Patronymic = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;

                    if (!Edo.EdoLiteClient.GetInstance().Authorization(buyerCertificate))
                        throw new Exception("Не удалось авторизоваться в ЭДО Лайт по сертификату.");

                    var docsFromEdoLite = Edo.EdoLiteClient.GetInstance().GetAllIncomingDocuments(dateTimeLastPeriod, null, myOrganization.Inn, Enums.EdoLiteDocTypeEnum.UpdInvoiceDop970);

                    foreach (var document in docsByBuyer)
                    {
                        try
                        {
                            if (!(document.BuyerEdoId?.StartsWith(_edoPrefix) ?? false))
                                continue;

                            var endOfSellerFile = document.DocEdoProcessing.FileName.Substring(document.DocEdoProcessing.FileName.Length - 13);

                            var isMarked = endOfSellerFile[3] == '1';

                            var universalBuyerTransferDocument = new Reporter.Reports.UniversalTransferBuyerDocumentUtd970
                            {
                                AcceptResult = Reporter.Enums.AcceptResultEnum.GoodsAcceptedWithoutDiscrepancy,
                                FinSubjectCreator = $"{document.BuyerName}, ИНН: {document.BuyerInn}",
                                SignerInfo = signerInfo,
                                CreateBuyerFileDate = DateTime.Now,
                                SellerFileId = document.DocEdoProcessing.FileName,
                                Function = "СЧФДОП",
                                CreateSellerFileDate = document.DocEdoProcessing.DocDate,
                                DocName = "Универсальный передаточный документ",
                                SellerInvoiceNumber = document.DocJournal.Code,
                                SellerInvoiceDate = document?.DocJournal?.DeliveryDate?.Date ?? document.DocEdoProcessing.DocDate.Date,
                                FileName = $"{_prefixBuyerFileName}_{myOrganization.FnsParticipantId}_{document.BuyerEdoId}_{DateTime.Now.ToString("yyyyMMdd")}_{Guid.NewGuid().ToString()}{endOfSellerFile}",
                                EdoProgramVersion = this.EdoProgramVersion,
                                DateReceive = DateTime.Now,
                                ContentOperationText = "Товары (работы, услуги, права) приняты без расхождений (претензий)"
                            };

                            if (document?.BuyerInn?.Length == 10)
                            {
                                universalBuyerTransferDocument.OrganizationEmployeeOrAnotherPerson = new List<object>(new object[] { new Reporter.Entities.OrganizationEmployee() });

                                ((Reporter.Entities.OrganizationEmployee)universalBuyerTransferDocument.OrganizationEmployeeOrAnotherPerson.First()).Position = signerInfo.Position;
                                ((Reporter.Entities.OrganizationEmployee)universalBuyerTransferDocument.OrganizationEmployeeOrAnotherPerson.First()).Surname = signerInfo.Surname;
                                ((Reporter.Entities.OrganizationEmployee)universalBuyerTransferDocument.OrganizationEmployeeOrAnotherPerson.First()).Name = signerInfo.Name;
                                ((Reporter.Entities.OrganizationEmployee)universalBuyerTransferDocument.OrganizationEmployeeOrAnotherPerson.First()).Patronymic = signerInfo.Patronymic;
                            }
                            else if(document?.BuyerInn?.Length == 12)
                            {
                                if (string.IsNullOrEmpty(signerInfo.Position))
                                    signerInfo.Position = "Индивидуальный предприниматель";
                            }

                            var docFromEdoLite = docsFromEdoLite.FirstOrDefault(d => d?.Documents?.FirstOrDefault()?.Number?.Contains(document.DocJournal.Code) ?? false);

                            if (docFromEdoLite == null)
                                continue;

                            if (docFromEdoLite.Status == (int)Enums.EdoLiteDocumentStatus.Signed || docFromEdoLite.Status == (int)Enums.EdoLiteDocumentStatus.SignedAndSend ||
                                docFromEdoLite.Status == (int)Enums.EdoLiteDocumentStatus.SignedNotReceived || docFromEdoLite.Status == (int)Enums.EdoLiteDocumentStatus.SignedSending)
                            {
                                document.DocEdoProcessing.DocStatus = (int)EdiProcessingUnit.Enums.DocEdoSendStatus.Signed;

                                if (isMarked)
                                    document.DocEdoProcessing.HonestMarkStatus = (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Sent;

                                _abtDbContext.SaveChanges();
                                continue;
                            }

                            var sellerContent = Edo.EdoLiteClient.GetInstance().GetIncomingDocumentContent(docFromEdoLite.EdoId);

                            if (!System.IO.Directory.Exists($"{edoFilesPath}//{docFromEdoLite.EdoId}"))
                                System.IO.Directory.CreateDirectory($"{edoFilesPath}//{docFromEdoLite.EdoId}");

                            var zipDocumentBytes = Edo.EdoLiteClient.GetInstance().GetIncomingZipDocument(docFromEdoLite.EdoId);

                            var sellerFileName = universalBuyerTransferDocument.SellerFileId;
                            using (System.IO.MemoryStream ms = new System.IO.MemoryStream(zipDocumentBytes))
                            {
                                using (var zipArchive = new System.IO.Compression.ZipArchive(ms))
                                {
                                    var signedEntry = zipArchive.Entries.FirstOrDefault(x => x.Name.StartsWith(sellerFileName) && x.Name != $"{sellerFileName}.xml");

                                    if (signedEntry == null)
                                        throw new Exception("Не найден файл подписи продавца.");

                                    using (var stream = new System.IO.MemoryStream())
                                    {
                                        signedEntry.Open().CopyTo(stream);
                                        var signedContent = stream.ToArray();
                                        universalBuyerTransferDocument.Signature = Convert.ToBase64String(signedContent);
                                    }
                                }
                            }

                            var xml = universalBuyerTransferDocument.GetXmlContent();
                            var fileBytes = Encoding.GetEncoding(1251).GetBytes(xml);

                            var cryptoUtil = new Cryptography.WinApi.WinApiCryptWrapper(buyerCertificate);
                            var signature = cryptoUtil.Sign(fileBytes, true);

                            var fileName = universalBuyerTransferDocument.FileName;

                            System.IO.File.WriteAllBytes($"{edoFilesPath}//{docFromEdoLite.EdoId}//{fileName}.xml", fileBytes);
                            System.IO.File.WriteAllBytes($"{edoFilesPath}//{docFromEdoLite.EdoId}//{fileName}.xml.sig", signature);

                            var directory = new System.IO.DirectoryInfo(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
                            string localPath = directory.Name;
                            while (directory.Parent != null)
                            {
                                directory = directory.Parent;

                                if (directory.Parent == null)
                                    localPath = $"{directory.Name.Replace(":\\", ":")}/{localPath}";
                                else
                                    localPath = $"{directory.Name}/{localPath}";
                            }

                            string content = $"{localPath}/{edoFilesPath}/{docFromEdoLite.EdoId}/{fileName}.xml";
                            var signatureAsBase64 = Convert.ToBase64String(signature);
                            var result = Edo.EdoLiteClient.GetInstance().LoadTitleDocument(content, docFromEdoLite.EdoId, signatureAsBase64);

                            if (!string.IsNullOrEmpty(result))
                            {
                                document.DocEdoProcessing.DocStatus = (int)EdiProcessingUnit.Enums.DocEdoSendStatus.Signed;

                                if (isMarked)
                                    document.DocEdoProcessing.HonestMarkStatus = (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Sent;

                                _abtDbContext.SaveChanges();
                                EdiProcessingUnit.Infrastructure.MailReporter.Add($"Документ {universalBuyerTransferDocument.SellerInvoiceNumber} успешно подписан в ЭДО Lite");
                            }
                        }
                        catch (System.Net.WebException webEx)
                        {
                            EdiProcessingUnit.Infrastructure.MailReporter.Add($"EdoLiteProcessorException \r\n Произолшла ошибка приёма либо подписания документа {document.DocJournal.Code} на удалённом сервере \r\n" + _log.GetRecursiveInnerException(webEx));
                        }
                        catch (Exception ex)
                        {
                            EdiProcessingUnit.Infrastructure.MailReporter.Add($"EdoLiteProcessorException \r\n Произолшла ошибка приёма либо подписания документа {document.DocJournal.Code} \r\n" + _log.GetRecursiveInnerException(ex));
                        }
                    }
                }
                catch(System.Net.WebException webEx)
                {
                    EdiProcessingUnit.Infrastructure.MailReporter.Add($"EdoLiteProcessorException \r\n Произолшла ошибка получения документов для организации {docsByBuyer.Key} на удалённом сервере\r\n" + _log.GetRecursiveInnerException(webEx));
                }
                catch (Exception ex)
                {
                    EdiProcessingUnit.Infrastructure.MailReporter.Add($"EdoLiteProcessorException \r\n Произолшла ошибка получения документов для организации {docsByBuyer.Key} \r\n" + _log.GetRecursiveInnerException(ex));
                }
            }
        }

        private List<X509Certificate2> GetPersonalCertificates()
        {
            _log.Log("GetPersonalCertificates: загрузка сертификатов из хранилища Личные.");
            var crypto = new Cryptography.WinApi.WinApiCryptWrapper();

            if (_personalCertificates == null)
                _personalCertificates = crypto.GetAllGostPersonalCertificates()?.Where(c => c.NotAfter > DateTime.Now)?.ToList();

            _log.Log("GetPersonalCertificates: выполнено.");
            return _personalCertificates;
        }

        public void Run()
        {
            List<string> connectionStringList = new EdiProcessingUnit.UsersConfig().GetAllConnectionStrings();
            var dataBaseUser = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser;

            foreach (var connectionString in connectionStringList)
            {
                try
                {
                    using (_abtDbContext = new AbtDbContext(connectionString, true))
                    {
                        _abtDbContext.Database.CommandTimeout = 500;
                        var docJournals = _abtDbContext.Set<DocJournal>().Include("DocMaster").Include("DocGoodsI").Include("DocGoodsDetailsIs");

                        var orgs = (from c in _abtDbContext.RefCustomers
                                    join refUser in _abtDbContext.RefUsersByOrgEdo
                                    on c.Id equals refUser.IdCustomer
                                    where refUser.UserName == dataBaseUser
                                    join r in _abtDbContext.RefRefTags
                                    on c.Id equals r.IdObject
                                    where r.IdTag == 215 && r.TagValue == "1"
                                    join fnsTag in _abtDbContext.RefRefTags
                                    on c.Id equals fnsTag.IdObject
                                    where fnsTag.IdTag == 223
                                    select new Kontragent
                                    {
                                        Name = c.Name,
                                        Inn = c.Inn,
                                        Kpp = c.Kpp,
                                        FnsParticipantId = fnsTag.TagValue,
                                        IsEdoApiConnected = true
                                    }).ToList();

                        foreach (var org in orgs)
                        {
                            try
                            {
                                SignDocuments(org);
                            }
                            catch (Exception ex)
                            {
                                EdiProcessingUnit.Infrastructure.MailReporter.Add($"EdoLiteProcessorException \r\n Произолшла ошибка отправки документов от организации {org.Inn} \r\n" + _log.GetRecursiveInnerException(ex));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    EdiProcessingUnit.Infrastructure.MailReporter.Add("EdoLiteProcessorException \r\n" + _log.GetRecursiveInnerException(ex));
                }
            }
        }
    }
}
