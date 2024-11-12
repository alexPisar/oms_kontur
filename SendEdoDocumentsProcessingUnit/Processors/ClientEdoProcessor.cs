﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Cryptography.WinApi;
using System.Security.Cryptography.X509Certificates;
using UtilitesLibrary.Logger;
using EdiProcessingUnit.Edo.Models;
using EdiProcessingUnit.Infrastructure;
using DataContextManagementUnit.DataAccess.Contexts.Abt;

namespace SendEdoDocumentsProcessingUnit.Processors
{
    public class ClientEdoProcessor : EdiProcessingUnit.Infrastructure.EdoProcessor
    {
        private AbtDbContext _abt;
        private Utils.XmlCertificateUtil _utils;
        private List<X509Certificate2> _personalCertificates = null;
        private Dictionary<decimal, Kontragent> _consignors;

        public ClientEdoProcessor():base()
        {
            var client = new System.Net.WebClient();

            if (_conf.ProxyEnabled)
            {
                var webProxy = new System.Net.WebProxy();

                webProxy.Address = new Uri("http://" + _conf.ProxyAddress);
                webProxy.Credentials = new System.Net.NetworkCredential(_conf.ProxyUserName, _conf.ProxyUserPassword);

                client.Proxy = webProxy;
            }
            _utils = new Utils.XmlCertificateUtil(client);
            _consignors = new Dictionary<decimal, Kontragent>();
        }

        private List<X509Certificate2> GetPersonalCertificates()
        {
            _log.Log("GetPersonalCertificates: загрузка сертификатов из хранилища Личные.");
            var crypto = new WinApiCryptWrapper();

            if (_personalCertificates == null)
                _personalCertificates = crypto.GetAllGostPersonalCertificates()?.Where(c => c.NotAfter > DateTime.Now)?.ToList();

            _log.Log("GetPersonalCertificates: выполнено.");
            return _personalCertificates;
        }

        private int FilterByStatuses(string status)
        {
            switch (status)
            {
                case "5":
                    return 5;
                case "6":
                    return 6;
                default:
                    return 5;
            };
        }

        public override void Run()
        {
            List<string> connectionStringList = new EdiProcessingUnit.UsersConfig().GetAllConnectionStrings();
            List<X509Certificate2> personalCertificates = GetPersonalCertificates();

            foreach (var connStr in connectionStringList)
            {
                using (_abt = new AbtDbContext(connStr, true))
                {
                    var dateTimeFrom = DateTime.Now.AddDays(-7);
                    var updDocType = (int)EdiProcessingUnit.Enums.DocEdoType.Upd;

                    var dataBaseUser = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser;

                    try
                    {
                        var docJournals = _abt.Set<DocJournal>().Include("DocGoodsDetailsIs");
                        string employee = _abt.SelectSingleValue("select const_value from ref_const where id = 1200");

                        var orgs = (from a in _abt.RefAuthoritySignDocuments
                                    where a.EmchdEndDate != null && a.IsMainDefault
                                    join c in _abt.RefCustomers
                                    on a.IdCustomer equals (c.Id)
                                    join refUser in _abt.RefUsersByOrgEdo
                                    on c.Id equals refUser.IdCustomer
                                    where refUser.UserName == dataBaseUser
                                    select new Kontragent
                                    {
                                        Name = c.Name,
                                        Inn = c.Inn,
                                        Kpp = c.Kpp,
                                        EmchdId = a.EmchdId,
                                        EmchdBeginDate = a.EmchdBeginDate,
                                        EmchdEndDate = a.EmchdEndDate,
                                        EmchdPersonInn = a.Inn,
                                        EmchdPersonSurname = a.Surname,
                                        EmchdPersonName = a.Name,
                                        EmchdPersonPatronymicSurname = a.PatronymicSurname,
                                        EmchdPersonPosition = a.Position
                                    }).ToList();

                        foreach (var myOrganization in orgs)
                        {
                            try
                            {
                                if (!string.IsNullOrEmpty(myOrganization.EmchdPersonInn))
                                {
                                    var certs = personalCertificates.Where(c => myOrganization.EmchdPersonInn == _utils.ParseCertAttribute(c.Subject, "ИНН").TrimStart('0') && _utils.IsCertificateValid(c)).OrderByDescending(c => c.NotBefore);
                                    myOrganization.Certificate = certs.FirstOrDefault(c => string.IsNullOrEmpty(_utils.GetOrgInnFromCertificate(c)));
                                }

                                if (myOrganization.Certificate == null || string.IsNullOrEmpty(myOrganization.EmchdPersonInn))
                                {
                                    if (myOrganization.Certificate == null && !string.IsNullOrEmpty(myOrganization.EmchdPersonInn))
                                        myOrganization.SetNullEmchdValues();

                                    var certs = personalCertificates.Where(c => myOrganization.Inn == _utils.GetOrgInnFromCertificate(c) && _utils.IsCertificateValid(c)).OrderByDescending(c => c.NotBefore);
                                    myOrganization.Certificate = certs.FirstOrDefault();
                                }

                                if (myOrganization.EmchdEndDate != null && myOrganization.EmchdEndDate.Value < DateTime.Now)
                                    throw new Exception($"Срок доверенности истёк для физ лица с ИНН {myOrganization.EmchdPersonInn}");

                                if (myOrganization.Certificate == null)
                                    throw new Exception($"Не найден сертификат организации с ИНН {myOrganization.Inn}");

                                if (!_edo.Authenticate(false, myOrganization.Certificate, myOrganization.Inn))
                                    throw new Exception("Не удалось авторизоваться в системе по сертификату.");

                                _edo.SetOrganizationParameters(myOrganization);

                                var addressSender = myOrganization?.Address?.RussianAddress?.ZipCode +
                                    (string.IsNullOrEmpty(myOrganization?.Address?.RussianAddress?.City) ? "" : $", {myOrganization.Address.RussianAddress.City}") +
                                    (string.IsNullOrEmpty(myOrganization?.Address?.RussianAddress?.Street) ? "" : $", {myOrganization.Address.RussianAddress.Street}") +
                                    (string.IsNullOrEmpty(myOrganization?.Address?.RussianAddress?.Building) ? "" : $", {myOrganization.Address.RussianAddress.Building}");

                                var signerDetails = _edo.GetExtendedSignerDetails(Diadoc.Api.Proto.Invoicing.Signers.DocumentTitleType.UtdSeller);
                                var counteragents = _edo.GetOrganizations(myOrganization.OrgId);

                                IEnumerable<UniversalTransferDocument> docs = (from doc in docJournals
                                            where doc != null && doc.DocGoodsI != null && doc.DocMaster != null && doc.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && doc.DocDatetime >= dateTimeFrom
                                            join docMaster in _abt.DocJournals on doc.IdDocMaster equals docMaster.Id
                                            where docMaster != null && docMaster.DocGoods != null && docMaster.DocDatetime >= dateTimeFrom
                                            join docGoods in _abt.DocGoods on docMaster.Id equals docGoods.IdDoc
                                            join customer in _abt.RefCustomers on docGoods.IdSeller equals customer.IdContractor
                                            where customer.Inn == myOrganization.Inn && customer.Kpp == myOrganization.Kpp
                                            let isMarked = (from label in _abt.DocGoodsDetailsLabels where label.IdDocSale == docMaster.Id select label).Count() > 0
                                            let honestMarkStatus = (from docComissionEdoProcessing in _abt.DocComissionEdoProcessings
                                                                    where isMarked && docComissionEdoProcessing.IdDoc == docMaster.Id
                                                                    orderby docComissionEdoProcessing.DocDate descending
                                                                    select docComissionEdoProcessing)
                                            let docEdoProcessing = (from docEdo in _abt.DocEdoProcessings
                                                                    where docEdo.IdDoc == docMaster.Id && docEdo.DocType == updDocType
                                                                    orderby docEdo.DocDate descending
                                                                    select docEdo)
                                            where docEdoProcessing.Count() == 0
                                            join buyerContractor in _abt.RefContractors
                                            on docGoods.IdCustomer equals buyerContractor.Id
                                            join buyerCustomer in _abt.RefCustomers
                                            on buyerContractor.DefaultCustomer equals buyerCustomer.Id
                                            join refRefTag in _abt.RefRefTags
                                            on buyerCustomer.Id equals refRefTag.IdObject
                                            where refRefTag.IdTag == 242
                                            let refRefTagValue = refRefTag.TagValue
                                            select new UniversalTransferDocument
                                            {
                                                Total = doc.DocGoodsI.TotalSumm,
                                                Vat = doc.DocGoodsI.TaxSumm,
                                                TotalWithVatExcluded = (doc.DocGoodsI.TotalSumm - doc.DocGoodsI.TaxSumm),
                                                DocJournal = doc,
                                                DocJournalNumber = docMaster.Code,
                                                DocumentNumber = doc.Code,
                                                ActStatus = docMaster.ActStatus,
                                                OrgId = myOrganization.OrgId,
                                                SenderName = myOrganization.Name,
                                                SenderInnKpp = myOrganization.Inn + "/" + myOrganization.Kpp,
                                                SenderAddress = addressSender,
                                                BuyerCustomer = buyerCustomer,
                                                SellerContractor = docMaster.DocGoods.Seller,
                                                BuyerContractor = buyerContractor,
                                                ProcessingStatus = honestMarkStatus,
                                                IsMarked = isMarked,
                                                ActStatusForSendFromTraderStr = refRefTagValue
                                            })?.ToList();

                                docs = docs?.AsParallel()?.Where(u => u.ActStatus >= FilterByStatuses(u.ActStatusForSendFromTraderStr))?.ToList();

                                foreach (var doc in docs ?? new List<UniversalTransferDocument>())
                                {
                                    try
                                    {
                                        var receiver = counteragents.FirstOrDefault(r => $"{r.Inn}/{r.Kpp}" == doc.BuyerInnKpp);

                                        if (receiver == null)
                                        {
                                            var length = doc.BuyerInnKpp.LastIndexOf('/');
                                            var buyerInn = doc.BuyerInnKpp.Substring(0, length).Trim();
                                            receiver = counteragents.FirstOrDefault(r => r.Inn == buyerInn);
                                        }

                                        if (receiver == null)
                                            continue;

                                        RefEdoGoodChannel refEdoGoodChannel = null;
                                        var idChannel = doc?.DocJournal?.DocMaster?.DocGoods?.Customer?.IdChannel;

                                        if (idChannel != null && idChannel != 99001)
                                            refEdoGoodChannel = (from r in _abt.RefContractors
                                                                 where r.IdChannel == idChannel
                                                                 join s in (from c in _abt.RefContractors
                                                                            where c.DefaultCustomer != null
                                                                            join refEdo in _abt.RefEdoGoodChannels on c.IdChannel equals (refEdo.IdChannel)
                                                                            select new { RefEdoGoodChannel = refEdo, RefContractor = c })
                                                                            on r.DefaultCustomer equals (s.RefContractor.DefaultCustomer)
                                                                 where s != null
                                                                 select s.RefEdoGoodChannel)?.FirstOrDefault();

                                        DocComissionEdoProcessing docComissionEdoProcessing = null;
                                        if (doc.IsMarked)
                                            if ((doc.ProcessingStatus as DocComissionEdoProcessing)?.DocStatus != 2 && (doc.ProcessingStatus as DocComissionEdoProcessing)?.DocStatus != 1)
                                                docComissionEdoProcessing = SendComissionDocumentForHonestMark(myOrganization, doc);


                                        var universalDocument = GetUniversalDocument(doc.DocJournal, myOrganization, employee, signerDetails, refEdoGoodChannel);

                                        if (universalDocument == null)
                                            throw new Exception("Не удалось сформировать документ.");

                                        var message = 
                                            new Utils.XmlCertificateUtil().SignAndSend(myOrganization.Certificate, myOrganization, receiver, new List<Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphens>
                                            (new Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphens[]{ universalDocument }));

                                        DocEdoProcessing docProcessing = null;
                                        if (message != null)
                                        {
                                            _log.Log($"Сохранение в базе данных документа, Id сообщения {message.MessageId}");

                                            var documentNumber = universalDocument.DocumentNumber;
                                            var entity = message.Entities.FirstOrDefault(t => t.AttachmentType == Diadoc.Api.Proto.Events.AttachmentType.UniversalTransferDocument &&
                                            t?.DocumentInfo?.DocumentNumber == documentNumber);

                                            var fileNameLength = entity.FileName.LastIndexOf('.');

                                            if (fileNameLength < 0)
                                                fileNameLength = entity.FileName.Length;

                                            var fileName = entity.FileName.Substring(0, fileNameLength);

                                            docProcessing = new DocEdoProcessing
                                            {
                                                Id = Guid.NewGuid().ToString(),
                                                MessageId = message.MessageId,
                                                EntityId = entity.EntityId,
                                                FileName = fileName,
                                                IsReprocessingStatus = 0,
                                                IdDoc = doc.CurrentDocJournalId,
                                                DocDate = DateTime.Now,
                                                UserName = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser,
                                                ReceiverName = receiver.Name,
                                                ReceiverInn = receiver.Inn,
                                                DocType = (int)EdiProcessingUnit.Enums.DocEdoType.Upd
                                            };

                                            if (docComissionEdoProcessing != null)
                                            {
                                                docProcessing.IdComissionDocument = docComissionEdoProcessing.Id;
                                                docProcessing.ComissionDocument = docComissionEdoProcessing;
                                                docComissionEdoProcessing.MainDocuments.Add(docProcessing);

                                                if (!_abt.DocEdoProcessings.Any(d => d.Id == docProcessing.Id))
                                                    _abt.DocEdoProcessings.Add(docProcessing);
                                            }
                                            else
                                            {
                                                _abt.DocEdoProcessings.Add(docProcessing);
                                            }
                                            MailReporter.Add($"Документ {doc.DocJournal.Code} успешно отправлен и сохранён.");
                                            _abt.SaveChanges();
                                        }
                                        else
                                        {
                                            _log.Log($"Не удалось идентифицировать отправленное сообщение.");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _log.Log(ex);
                                        MailReporter.Add(ex, $"Произошла ошибка при отправке документа {doc.DocJournal.Code}:\r\n");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _log.Log(ex);
                                MailReporter.Add(ex, $"Произошла ошибка перед отправкой документов организации {myOrganization.Name}:\r\n");
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        _log.Log(ex);
                        MailReporter.Add(ex);
                    }
                }
            }
        }

        private Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphens GetUniversalDocument(
            DocJournal d, Kontragent organization, string employee = null, Diadoc.Api.Proto.Invoicing.Signers.ExtendedSignerDetails signerDetails = null, RefEdoGoodChannel edoGoodChannel = null)
        {
            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && d.DocMaster == null)
                return null;

            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && d.DocGoodsI == null)
                return null;

            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Translocation && d.DocGoods == null)
                return null;

            var document = new Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphens()
            {
                Function = Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphensFunction.СЧФДОП,
                DocumentNumber = d.Code,
                DocumentDate = d.DeliveryDate?.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy"),
                Currency = Properties.Settings.Default.DefaultCurrency,
                Table = new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTable
                {
                    TotalSpecified = true,
                    TotalWithVatExcludedSpecified = true
                },
                TransferInfo = new Diadoc.Api.DataXml.Utd820.Hyphens.TransferInfo
                {
                    OperationInfo = "Товары переданы",
                    TransferDate = d.DeliveryDate?.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy")
                }
            };

            if (!string.IsNullOrEmpty(organization.EmchdId))
                document.DocumentCreator = $"{organization.EmchdPersonSurname} {organization.EmchdPersonName} {organization.EmchdPersonPatronymicSurname}";
            else
                document.DocumentCreator = _utils.ParseCertAttribute(organization.Certificate.Subject, "SN") + " " + _utils.ParseCertAttribute(organization.Certificate.Subject, "G");

            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
            {
                document.Table.VatSpecified = true;
                document.Table.Total = (decimal)d.DocGoodsI.TotalSumm;
                document.Table.Vat = (decimal)d.DocGoodsI.TaxSumm;
                document.Table.TotalWithVatExcluded = (decimal)(d.DocGoodsI.TotalSumm - d.DocGoodsI.TaxSumm);
            }
            else
            {
                document.Table.Total = (decimal)(d?.DocGoods?.TotalSumm ?? 0);
                document.Table.TotalWithVatExcluded = (decimal)(d?.DocGoods?.TotalSumm ?? 0);
                document.Table.WithoutVat = Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableWithoutVat.True;
            }

            if (!string.IsNullOrEmpty(employee))
            {
                document.TransferInfo.Employee = new Diadoc.Api.DataXml.Utd820.Hyphens.Employee
                {
                    Position = Properties.Settings.Default.DefaultEmployePosition,
                    EmployeeInfo = employee,
                    LastName = employee.Substring(0, employee.IndexOf(' ')),
                    FirstName = employee.Substring(employee.IndexOf(' ') + 1)
                };
            }


            Diadoc.Api.DataXml.RussianAddress senderAddress = organization?.Address?.RussianAddress != null ?
                            new Diadoc.Api.DataXml.RussianAddress
                            {
                                ZipCode = organization.Address.RussianAddress.ZipCode,
                                Region = organization.Address.RussianAddress.Region,
                                Street = string.IsNullOrEmpty(organization?.Address?.RussianAddress?.Street) ? null : organization.Address.RussianAddress.Street,
                                City = string.IsNullOrEmpty(organization?.Address?.RussianAddress?.City) ? null : organization.Address.RussianAddress.City,
                                Locality = string.IsNullOrEmpty(organization?.Address?.RussianAddress?.Locality) ? null : organization.Address.RussianAddress.Locality,
                                Territory = string.IsNullOrEmpty(organization?.Address?.RussianAddress?.Territory) ? null : organization.Address.RussianAddress.Territory,
                                Building = string.IsNullOrEmpty(organization?.Address?.RussianAddress?.Building) ? null : organization.Address.RussianAddress.Building
                            } : null;

            document.Sellers = new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationInfoWithHyphens[]
            {
                        new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationInfoWithHyphens
                        {
                            Item = new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationDetailsWithHyphens
                            {
                                Inn = organization.Inn,
                                Kpp = organization.Kpp,
                                OrgType = organization.Inn.Length == 12 ? Diadoc.Api.DataXml.OrganizationType.IndividualEntity : Diadoc.Api.DataXml.OrganizationType.LegalEntity,
                                OrgName = organization.Name,
                                Address = new Diadoc.Api.DataXml.Address
                                {
                                    Item = senderAddress
                                }
                            }
                        }
            };

            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
            {
                var sellerContractor = _abt.RefContractors?
                .Where(s => s.Id == d.DocMaster.DocGoods.IdSeller)?
                .FirstOrDefault();

                if (sellerContractor != null)
                    document.Shippers = new Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphensShipper[]
                    {
                            new Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphensShipper
                            {
                                Item = new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationDetails
                                {
                                    Inn = organization.Inn,
                                    OrgType = organization.Inn.Length == 12 ? Diadoc.Api.DataXml.OrganizationType.IndividualEntity : Diadoc.Api.DataXml.OrganizationType.LegalEntity,
                                    Address = new Diadoc.Api.DataXml.Address
                                    {
                                        Item = new Diadoc.Api.DataXml.ForeignAddress
                                        {
                                            Country = Properties.Settings.Default.DefaultOrgCountryCode,
                                            Address = sellerContractor.Address
                                        }
                                    },
                                    OrgName = sellerContractor.Name
                                }
                            }
                    };
            }

            var idCustomer = d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice ? d.DocMaster?.DocGoods?.IdCustomer : d?.DocGoods?.IdCustomer;
            if (idCustomer > 0)
            {
                var buyerContractor = _abt?.RefContractors?
                .Where(r => r.Id == idCustomer)?
                .FirstOrDefault();

                if (buyerContractor != null)
                {

                    if (buyerContractor.DefaultCustomer != null)
                    {
                        var buyerCustomer = _abt.RefCustomers?
                        .Where(r => r.Id == buyerContractor.DefaultCustomer)?
                        .FirstOrDefault();

                        if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
                        {
                            document.Consignees = new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationInfoWithHyphens[]
                        {
                                        new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationInfoWithHyphens
                                        {
                                            Item = new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationDetailsWithHyphens
                                            {
                                                Inn = buyerCustomer.Inn,
                                                OrgType = buyerCustomer.Inn.Length == 12 ? Diadoc.Api.DataXml.OrganizationType.IndividualEntity : Diadoc.Api.DataXml.OrganizationType.LegalEntity,
                                                Address = new Diadoc.Api.DataXml.Address
                                                {
                                                    Item = new Diadoc.Api.DataXml.ForeignAddress
                                                    {
                                                        Country = Properties.Settings.Default.DefaultOrgCountryCode,
                                                        Address = buyerContractor.Address
                                                    }
                                                },
                                                OrgName = buyerContractor.Name
                                            }
                                        }
                        };
                        }

                        document.Buyers = new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationInfoWithHyphens[]
                        {
                                    new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationInfoWithHyphens
                                    {
                                        Item = new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationDetailsWithHyphens
                                        {
                                            Inn = buyerCustomer.Inn,
                                            Kpp = buyerCustomer.Kpp,
                                            OrgType = buyerCustomer.Inn.Length == 12 ? Diadoc.Api.DataXml.OrganizationType.IndividualEntity : Diadoc.Api.DataXml.OrganizationType.LegalEntity,
                                            OrgName = buyerCustomer.Name,
                                            Address = new Diadoc.Api.DataXml.Address
                                            {
                                                Item = new Diadoc.Api.DataXml.ForeignAddress
                                                {
                                                    Country = Properties.Settings.Default.DefaultOrgCountryCode,
                                                    Address = buyerCustomer.Address
                                                }
                                            }
                                        }
                                    }
                        };
                    }
                }
            }

            Diadoc.Api.DataXml.ExtendedSignerDetails_SellerTitle[] signer;

            if (string.IsNullOrEmpty(organization.EmchdId))
            {
                var firstMiddleName = _utils.ParseCertAttribute(organization.Certificate.Subject, "G");
                string signerFirstName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
                string signerMiddleName = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;

                signer = new[]
                {
                                new Diadoc.Api.DataXml.ExtendedSignerDetails_SellerTitle
                                {
                                    FirstName = signerFirstName,
                                    MiddleName = signerMiddleName,
                                    LastName = _utils.ParseCertAttribute(organization.Certificate.Subject, "SN"),
                                    SignerOrganizationName = _utils.ParseCertAttribute(organization.Certificate.Subject, "CN"),
                                    Inn = _utils.ParseCertAttribute(organization.Certificate.Subject, "ИНН").TrimStart('0'),
                                    Position = _utils.ParseCertAttribute(organization.Certificate.Subject, "T")
                                }
                            };
            }
            else
            {
                signer = new[]
                {
                    new Diadoc.Api.DataXml.ExtendedSignerDetails_SellerTitle
                    {
                        FirstName = organization.EmchdPersonName,
                        MiddleName = organization.EmchdPersonPatronymicSurname,
                        LastName = organization.EmchdPersonSurname,
                        SignerOrganizationName = organization.Name,
                        Inn = organization.EmchdPersonInn,
                        Position = organization.EmchdPersonPosition,
                        SignerPowersBase = "Доверенность"
                    }
                };
            }

            if (signer.First().Inn == organization.Inn)
                signer.First().SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.LegalEntity;
            else if (signer.First().Inn?.Length == 12)
            {
                signer.First().SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.PhysicalPerson;

                if (string.IsNullOrEmpty(signer.First().SignerPowersBase))
                    signer.First().SignerPowersBase = signer.First().Position;
            }
            else
                signer.First().SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.IndividualEntity;

            if (signerDetails != null)
            {
                signer.First().Inn = organization.Inn;
                signer.First().SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.LegalEntity;
                signer.First().SignerPowers = (Diadoc.Api.DataXml.ExtendedSignerDetails_SellerTitleSignerPowers)Convert.ToInt32(signerDetails.SignerPowers);

                if (signerDetails.SignerStatus == Diadoc.Api.Proto.Invoicing.Signers.SignerStatus.SellerEmployee)
                    signer.First().SignerStatus = Diadoc.Api.DataXml.ExtendedSignerDetailsSignerStatus.SellerEmployee;
                else if (signerDetails.SignerStatus == Diadoc.Api.Proto.Invoicing.Signers.SignerStatus.InformationCreatorEmployee)
                    signer.First().SignerStatus = Diadoc.Api.DataXml.ExtendedSignerDetailsSignerStatus.InformationCreatorEmployee;
                else if (signerDetails.SignerStatus == Diadoc.Api.Proto.Invoicing.Signers.SignerStatus.OtherOrganizationEmployee)
                    signer.First().SignerStatus = Diadoc.Api.DataXml.ExtendedSignerDetailsSignerStatus.OtherOrganizationEmployee;
                else if (signerDetails.SignerStatus == Diadoc.Api.Proto.Invoicing.Signers.SignerStatus.AuthorizedPerson)
                    signer.First().SignerStatus = Diadoc.Api.DataXml.ExtendedSignerDetailsSignerStatus.AuthorizedPerson;
            }

            document.UseSignerDetails(signer);

            int docLineCount = d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice ? d.DocGoodsDetailsIs.Count : d.Details.Count;
            document.DocumentShipments = new Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphensDocumentShipment[]
            {
                                new Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphensDocumentShipment
                                {
                                    Name = "Реализация (акт, накладная, УПД)",
                                    Number = $"п/п 1-{docLineCount}, №{d.Code}",
                                    Date = d.DeliveryDate?.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy")
                                }
            };

            var details = new List<Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItem>();

            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
            {
                var additionalInfoList = new List<Diadoc.Api.DataXml.Utd820.Hyphens.AdditionalInfo>();

                if (edoGoodChannel != null)
                {
                    if (!string.IsNullOrEmpty(edoGoodChannel.NumberUpdId))
                        additionalInfoList.Add(new Diadoc.Api.DataXml.Utd820.Hyphens.AdditionalInfo { Id = edoGoodChannel.NumberUpdId, Value = d.Code });

                    if (!string.IsNullOrEmpty(edoGoodChannel.OrderNumberUpdId))
                    {
                        var docJournalTag = _abt.DocJournalTags.FirstOrDefault(t => t.IdDoc == d.IdDocMaster && t.IdTad == 137);

                        if (docJournalTag == null)
                            throw new Exception("Отсутствует номер заказа покупателя.");

                        additionalInfoList.Add(new Diadoc.Api.DataXml.Utd820.Hyphens.AdditionalInfo { Id = edoGoodChannel.OrderNumberUpdId, Value = docJournalTag?.TagValue ?? string.Empty });
                    }

                    if (!string.IsNullOrEmpty(edoGoodChannel.OrderDateUpdId))
                        additionalInfoList.Add(new Diadoc.Api.DataXml.Utd820.Hyphens.AdditionalInfo { Id = edoGoodChannel.OrderDateUpdId, Value = d.DocMaster.DocDatetime.ToString("dd.MM.yyyy") });

                    if (!string.IsNullOrEmpty(edoGoodChannel.GlnShipToUpdId))
                    {
                        string glnShipTo = null;
                        var shipToGlnJournalTag = _abt.DocJournalTags.FirstOrDefault(t => t.IdDoc == d.IdDocMaster && t.IdTad == 222);

                        if (shipToGlnJournalTag != null)
                        {
                            glnShipTo = shipToGlnJournalTag.TagValue;
                        }
                        else if (WebService.Controllers.FinDbController.GetInstance().LoadedConfig)
                        {
                            var docOrderInfo = WebService.Controllers.FinDbController.GetInstance().GetDocOrderInfoByIdDocAndOrderStatus(d.IdDocMaster.Value);
                            glnShipTo = docOrderInfo?.GlnShipTo;
                        }

                        if (!string.IsNullOrEmpty(glnShipTo))
                            additionalInfoList.Add(new Diadoc.Api.DataXml.Utd820.Hyphens.AdditionalInfo { Id = edoGoodChannel.GlnShipToUpdId, Value = glnShipTo });
                    }

                    foreach (var keyValuePair in edoGoodChannel.EdoValuesPairs)
                        additionalInfoList.Add(new Diadoc.Api.DataXml.Utd820.Hyphens.AdditionalInfo { Id = keyValuePair.Key, Value = keyValuePair.Value });
                }

                if (additionalInfoList.Count > 0)
                    document.AdditionalInfoId = new Diadoc.Api.DataXml.Utd820.Hyphens.AdditionalInfoId { AdditionalInfo = additionalInfoList.ToArray() };

                int number = 1;
                foreach (var docJournalDetail in d.DocGoodsDetailsIs)
                {
                    var refGood = _abt.RefGoods?
                    .FirstOrDefault(r => r.Id == docJournalDetail.IdGood);

                    if (refGood == null)
                        continue;

                    var barCode = _abt.RefBarCodes?
                        .FirstOrDefault(b => b.IdGood == docJournalDetail.IdGood && b.IsPrimary == false)?
                        .BarCode;

                    string countryCode = _abt.SelectSingleValue("select NUM_CODE from REF_COUNTRIES where id in" +
                        $"(select ID_COUNTRY from REF_GOODS where ID = {refGood.Id})");

                    if (countryCode.Length == 1)
                        countryCode = "00" + countryCode;
                    else if (countryCode.Length == 2)
                        countryCode = "0" + countryCode;

                    var subtotal = Math.Round(docJournalDetail.Quantity * ((decimal)docJournalDetail.Price - (decimal)docJournalDetail.DiscountSumm), 2);
                    var vat = (decimal)Math.Round(subtotal * docJournalDetail.TaxRate / (docJournalDetail.TaxRate + 100), 2, MidpointRounding.AwayFromZero);

                    decimal price = 0;

                    if (docJournalDetail.Quantity > 0)
                        price = (decimal)Math.Round((subtotal - vat) / docJournalDetail.Quantity, 2, MidpointRounding.AwayFromZero);
                    else
                        price = (decimal)Math.Round(docJournalDetail.Price - docJournalDetail.DiscountSumm - docJournalDetail.TaxSumm, 2);

                    var detail = new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItem
                    {
                        Product = refGood.Name,
                        Unit = Properties.Settings.Default.DefaultUnit,
                        Quantity = docJournalDetail.Quantity,
                        QuantitySpecified = true,
                        VatSpecified = true,
                        Vat = vat,
                        PriceSpecified = true,
                        Price = price,
                        SubtotalSpecified = true,
                        Subtotal = subtotal,
                        SubtotalWithVatExcludedSpecified = true,
                        SubtotalWithVatExcluded = subtotal - vat,
                        ItemVendorCode = barCode,
                        CustomsDeclarations = new Diadoc.Api.DataXml.Utd820.Hyphens.CustomsDeclarationWithHyphens[]
                        {
                                        new Diadoc.Api.DataXml.Utd820.Hyphens.CustomsDeclarationWithHyphens
                                        {
                                            Country = countryCode
                                        }
                        }
                    };

                    if (!string.IsNullOrEmpty(refGood.CustomsNo))
                        detail.CustomsDeclarations.First().DeclarationNumber = refGood.CustomsNo;

                    switch (docJournalDetail.TaxRate)
                    {
                        case 0:
                            detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateWithTwentyPercentAndTaxedByAgent.Zero;
                            break;
                        case 10:
                            detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateWithTwentyPercentAndTaxedByAgent.TenPercent;
                            break;
                        case 18:
                            detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateWithTwentyPercentAndTaxedByAgent.EighteenPercent;
                            break;
                        case 20:
                            detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateWithTwentyPercentAndTaxedByAgent.TwentyPercent;
                            break;
                        default:
                            detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateWithTwentyPercentAndTaxedByAgent.Zero;
                            break;
                    }

                    decimal idGood = docJournalDetail.IdGood, idDoc = (decimal)d.IdDocMaster;

                    var docGoodDetailLabels = _abt?.Database?
                    .SqlQuery<string>($"select DM_LABEL from doc_goods_details_labels where id_doc_sale = {idDoc} and id_good = {idGood}")?
                    .ToList() ?? new List<string>();

                    if (docGoodDetailLabels.Count > 0)
                    {
                        detail.ItemMark = Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemItemMark.PropertyRights;
                        detail.ItemIdentificationNumbers = new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemItemIdentificationNumber[1];
                        detail.ItemIdentificationNumbers[0] = new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemItemIdentificationNumber
                        {
                            ItemsElementName = new Diadoc.Api.DataXml.ItemsChoiceType[docGoodDetailLabels.Count],
                            Items = new string[docGoodDetailLabels.Count]
                        };

                        int j = 0;
                        foreach (var doc in docGoodDetailLabels)
                        {
                            detail.ItemIdentificationNumbers[0].ItemsElementName[j] = Diadoc.Api.DataXml.ItemsChoiceType.Unit;
                            detail.ItemIdentificationNumbers[0].Items[j] = doc;
                            j++;
                        }
                    }


                    var detailAdditionalInfos = new List<Diadoc.Api.DataXml.Utd820.Hyphens.AdditionalInfo>();
                    if (edoGoodChannel != null)
                    {
                        var idChannel = edoGoodChannel.IdChannel;
                        if (!string.IsNullOrEmpty(edoGoodChannel.DetailBuyerCodeUpdId))
                        {
                            var goodMatching = _abt.RefGoodMatchings.FirstOrDefault(r => r.IdChannel == idChannel && r.IdGood == refGood.Id && r.Disabled == 0);

                            if (goodMatching == null)
                            {
                                var docDateTime = d.DocMaster.DocDatetime.Date;

                                goodMatching = _abt.RefGoodMatchings.FirstOrDefault(r => r.DisabledDatetime != null && r.IdChannel == idChannel &&
                                r.IdGood == refGood.Id && r.Disabled == 1 && r.DisabledDatetime.Value >= docDateTime);
                            }

                            if (goodMatching == null)
                                throw new Exception("Не все товары сопоставлены с кодами покупателя.");

                            if (!string.IsNullOrEmpty(goodMatching?.CustomerArticle))
                                detailAdditionalInfos.Add(new Diadoc.Api.DataXml.Utd820.Hyphens.AdditionalInfo { Id = edoGoodChannel.DetailBuyerCodeUpdId, Value = goodMatching.CustomerArticle });
                            else
                                throw new Exception("Не для всех товаров заданы коды покупателя.");
                        }

                        if (!string.IsNullOrEmpty(edoGoodChannel.DetailBarCodeUpdId))
                            detailAdditionalInfos.Add(new Diadoc.Api.DataXml.Utd820.Hyphens.AdditionalInfo { Id = edoGoodChannel.DetailBarCodeUpdId, Value = barCode });

                        if (!string.IsNullOrEmpty(edoGoodChannel.DetailPositionUpdId))
                            detailAdditionalInfos.Add(new Diadoc.Api.DataXml.Utd820.Hyphens.AdditionalInfo { Id = edoGoodChannel.DetailPositionUpdId, Value = number.ToString() });
                    }

                    if (detailAdditionalInfos.Count > 0)
                        detail.AdditionalInfos = detailAdditionalInfos.ToArray();

                    details.Add(detail);
                    number++;
                }
                document.Table.Total = details.Sum(i => i.Subtotal);
                document.Table.Vat = details.Sum(i => i.Vat);
                document.Table.TotalWithVatExcluded = document.Table.Total - document.Table.Vat;
            }
            else
            {
                foreach (var docJournalDetail in d.Details)
                {
                    var refGood = _abt.RefGoods?
                    .FirstOrDefault(r => r.Id == docJournalDetail.IdGood);

                    if (refGood == null)
                        continue;

                    var barCode = _abt.RefBarCodes?
                        .FirstOrDefault(b => b.IdGood == docJournalDetail.IdGood && b.IsPrimary == false)?
                        .BarCode;

                    string countryCode = _abt.SelectSingleValue("select NUM_CODE from REF_COUNTRIES where id in" +
                        $"(select ID_COUNTRY from REF_GOODS where ID = {refGood.Id})");

                    if (countryCode.Length == 1)
                        countryCode = "00" + countryCode;
                    else if (countryCode.Length == 2)
                        countryCode = "0" + countryCode;

                    var subtotal = Math.Round(docJournalDetail.Quantity * ((decimal)docJournalDetail.Price - (decimal)docJournalDetail.DiscountSumm), 2);
                    var detail = new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItem
                    {
                        Product = refGood.Name,
                        Unit = Properties.Settings.Default.DefaultUnit,
                        Quantity = docJournalDetail.Quantity,
                        QuantitySpecified = true,
                        PriceSpecified = true,
                        Price = (decimal)Math.Round(docJournalDetail.Price - docJournalDetail.DiscountSumm, 2),
                        SubtotalSpecified = true,
                        Subtotal = subtotal,
                        SubtotalWithVatExcludedSpecified = true,
                        WithoutVat = Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemWithoutVat.True,
                        SubtotalWithVatExcluded = subtotal,
                        ItemVendorCode = barCode,
                        CustomsDeclarations = new Diadoc.Api.DataXml.Utd820.Hyphens.CustomsDeclarationWithHyphens[]
                        {
                                        new Diadoc.Api.DataXml.Utd820.Hyphens.CustomsDeclarationWithHyphens
                                        {
                                            Country = countryCode
                                        }
                        }
                    };

                    if (!string.IsNullOrEmpty(refGood.CustomsNo))
                        detail.CustomsDeclarations.First().DeclarationNumber = refGood.CustomsNo;

                    decimal idGood = docJournalDetail.IdGood, idDoc = (decimal)d.Id;

                    var docGoodDetailLabels = _abt?.Database?
                    .SqlQuery<string>($"select DM_LABEL from doc_goods_details_labels where id_doc_sale = {idDoc} and id_good = {idGood}")?
                    .ToList() ?? new List<string>();

                    if (docGoodDetailLabels.Count > 0)
                    {
                        detail.ItemMark = Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemItemMark.PropertyRights;
                        detail.ItemIdentificationNumbers = new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemItemIdentificationNumber[1];
                        detail.ItemIdentificationNumbers[0] = new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemItemIdentificationNumber
                        {
                            ItemsElementName = new Diadoc.Api.DataXml.ItemsChoiceType[docGoodDetailLabels.Count],
                            Items = new string[docGoodDetailLabels.Count]
                        };

                        int j = 0;
                        foreach (var doc in docGoodDetailLabels)
                        {
                            detail.ItemIdentificationNumbers[0].ItemsElementName[j] = Diadoc.Api.DataXml.ItemsChoiceType.Unit;
                            detail.ItemIdentificationNumbers[0].Items[j] = doc;
                            j++;
                        }
                    }

                    details.Add(detail);
                }
            }
            document.Table.Item = details.ToArray();

            return document;
        }

        private Diadoc.Api.DataXml.Utd820.UniversalTransferDocumentBuyerTitle CreateBuyerShipmentDocument(Kontragent receiverOrganization)
        {
            Diadoc.Api.DataXml.ExtendedSignerDetails_BuyerTitle820[] signers;

            if (string.IsNullOrEmpty(receiverOrganization.EmchdId))
            {
                var firstMiddleName = _utils.ParseCertAttribute(receiverOrganization.Certificate.Subject, "G");
                string signerFirstName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
                string signerMiddleName = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;

                signers = new[]
                {
                new Diadoc.Api.DataXml.ExtendedSignerDetails_BuyerTitle820
                {
                    FirstName = signerFirstName,
                    MiddleName = signerMiddleName,
                    LastName = _utils.ParseCertAttribute(receiverOrganization.Certificate.Subject, "SN"),
                    SignerOrganizationName = _utils.ParseCertAttribute(receiverOrganization.Certificate.Subject, "CN"),
                    Inn = _utils.ParseCertAttribute(receiverOrganization.Certificate.Subject, "ИНН").TrimStart('0'),
                    Position = _utils.ParseCertAttribute(receiverOrganization.Certificate.Subject, "T"),
                    SignerStatus = Diadoc.Api.DataXml.ExtendedSignerDetails_BuyerTitle820SignerStatus.AuthorizedPerson,
                    SignerPowersBase = "Должностные обязанности"
                }
            };
            }
            else
            {
                signers = new[]
                {
                    new Diadoc.Api.DataXml.ExtendedSignerDetails_BuyerTitle820
                    {
                        FirstName = receiverOrganization.EmchdPersonName,
                        MiddleName = receiverOrganization.EmchdPersonPatronymicSurname,
                        LastName = receiverOrganization.EmchdPersonSurname,
                        SignerOrganizationName = receiverOrganization.Name,
                        Inn = receiverOrganization.EmchdPersonInn,
                        Position = receiverOrganization.EmchdPersonPosition,
                        SignerStatus = Diadoc.Api.DataXml.ExtendedSignerDetails_BuyerTitle820SignerStatus.AuthorizedPerson,
                        SignerPowersBase = "Доверенность"
                    }
                };
            }

            if (signers.First().Inn == receiverOrganization.Inn)
                signers.First().SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.LegalEntity;
            else if (signers.First().Inn?.Length == 12)
            {
                signers.First().SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.PhysicalPerson;

                if (string.IsNullOrEmpty(signers.First().SignerPowersBase))
                    signers.First().SignerPowersBase = signers.First().Position;
            }
            else
                signers.First().SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.IndividualEntity;

            var document = new Diadoc.Api.DataXml.Utd820.UniversalTransferDocumentBuyerTitle()
            {
                Signers = signers,
                OperationContent = "Товары переданы"
            };

            if (!string.IsNullOrEmpty(receiverOrganization.EmchdId))
                document.DocumentCreator = $"{receiverOrganization.EmchdPersonSurname} {receiverOrganization.EmchdPersonName} {receiverOrganization.EmchdPersonPatronymicSurname}";
            else
                document.DocumentCreator = _utils.ParseCertAttribute(receiverOrganization.Certificate.Subject, "SN") + " " + _utils.ParseCertAttribute(receiverOrganization.Certificate.Subject, "G");

            return document;
        }

        private Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphens CreateShipmentDocument(
            DocJournal d, Kontragent senderOrganization, Kontragent receiverOrganization, List<DocGoodsDetailsLabels> detailsLabels, string documentNumber, string employee = null, bool considerOnlyLabeledGoods = false)
        {
            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && d.DocMaster == null)
                return null;

            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && d.DocGoodsI == null)
                return null;

            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Translocation && d.DocGoods == null)
                return null;

            var document = new Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphens()
            {
                Function = Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphensFunction.ДОП,
                DocumentNumber = documentNumber,
                DocumentDate = d.DeliveryDate?.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy"),
                Currency = Properties.Settings.Default.DefaultCurrency,
                Table = new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTable
                {
                    TotalSpecified = true,
                    TotalWithVatExcludedSpecified = true
                },
                TransferInfo = new Diadoc.Api.DataXml.Utd820.Hyphens.TransferInfo
                {
                    OperationInfo = "Товары переданы",
                    TransferDate = d.DeliveryDate?.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy")
                }
            };

            if (!string.IsNullOrEmpty(senderOrganization.EmchdId))
                document.DocumentCreator = $"{senderOrganization.EmchdPersonSurname} {senderOrganization.EmchdPersonName} {senderOrganization.EmchdPersonPatronymicSurname}";
            else
                document.DocumentCreator = _utils.ParseCertAttribute(senderOrganization.Certificate.Subject, "SN") + " " + _utils.ParseCertAttribute(senderOrganization.Certificate.Subject, "G");

            if (!string.IsNullOrEmpty(employee))
            {
                document.TransferInfo.Employee = new Diadoc.Api.DataXml.Utd820.Hyphens.Employee
                {
                    Position = Properties.Settings.Default.DefaultEmployePosition,
                    EmployeeInfo = employee,
                    LastName = employee.Substring(0, employee.IndexOf(' ')),
                    FirstName = employee.Substring(employee.IndexOf(' ') + 1)
                };
            }


            Diadoc.Api.DataXml.RussianAddress senderAddress = senderOrganization?.Address?.RussianAddress != null ?
                            new Diadoc.Api.DataXml.RussianAddress
                            {
                                ZipCode = senderOrganization.Address.RussianAddress.ZipCode,
                                Region = senderOrganization.Address.RussianAddress.Region,
                                Street = string.IsNullOrEmpty(senderOrganization?.Address?.RussianAddress?.Street) ? null : senderOrganization.Address.RussianAddress.Street,
                                City = string.IsNullOrEmpty(senderOrganization?.Address?.RussianAddress?.City) ? null : senderOrganization.Address.RussianAddress.City,
                                Locality = string.IsNullOrEmpty(senderOrganization?.Address?.RussianAddress?.Locality) ? null : senderOrganization.Address.RussianAddress.Locality,
                                Territory = string.IsNullOrEmpty(senderOrganization?.Address?.RussianAddress?.Territory) ? null : senderOrganization.Address.RussianAddress.Territory,
                                Building = string.IsNullOrEmpty(senderOrganization?.Address?.RussianAddress?.Building) ? null : senderOrganization.Address.RussianAddress.Building
                            } : null;

            document.Sellers = new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationInfoWithHyphens[]
            {
                new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationInfoWithHyphens
                {
                    Item = new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationDetailsWithHyphens
                    {
                        Inn = senderOrganization.Inn,
                        Kpp = senderOrganization.Kpp,
                        OrgType = senderOrganization.Inn.Length == 12 ? Diadoc.Api.DataXml.OrganizationType.IndividualEntity : Diadoc.Api.DataXml.OrganizationType.LegalEntity,
                        OrgName = senderOrganization.Name,
                        Address = new Diadoc.Api.DataXml.Address
                        {
                            Item = senderAddress
                        }
                    }
                }
            };

            Diadoc.Api.DataXml.RussianAddress receiverAddress = receiverOrganization?.Address?.RussianAddress != null ?
                new Diadoc.Api.DataXml.RussianAddress
                {
                    ZipCode = receiverOrganization.Address.RussianAddress.ZipCode,
                    Region = receiverOrganization.Address.RussianAddress.Region,
                    Street = string.IsNullOrEmpty(receiverOrganization?.Address?.RussianAddress?.Street) ? null : receiverOrganization.Address.RussianAddress.Street,
                    City = string.IsNullOrEmpty(receiverOrganization?.Address?.RussianAddress?.City) ? null : receiverOrganization.Address.RussianAddress.City,
                    Locality = string.IsNullOrEmpty(receiverOrganization?.Address?.RussianAddress?.Locality) ? null : receiverOrganization.Address.RussianAddress.Locality,
                    Territory = string.IsNullOrEmpty(receiverOrganization?.Address?.RussianAddress?.Territory) ? null : receiverOrganization.Address.RussianAddress.Territory,
                    Building = string.IsNullOrEmpty(receiverOrganization?.Address?.RussianAddress?.Building) ? null : receiverOrganization.Address.RussianAddress.Building
                } : null;

            document.Buyers = new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationInfoWithHyphens[]
            {
                new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationInfoWithHyphens
                {
                    Item = new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationDetailsWithHyphens
                    {
                        Inn = receiverOrganization.Inn,
                        Kpp = receiverOrganization.Kpp,
                        OrgType = receiverOrganization.Inn.Length == 12 ? Diadoc.Api.DataXml.OrganizationType.IndividualEntity : Diadoc.Api.DataXml.OrganizationType.LegalEntity,
                        OrgName = receiverOrganization.Name,
                        Address = new Diadoc.Api.DataXml.Address
                        {
                            Item = receiverAddress
                        }
                    }
                }
            };

            Diadoc.Api.DataXml.ExtendedSignerDetails_SellerTitle[] signer;
            if (string.IsNullOrEmpty(senderOrganization.EmchdId))
            {
                var firstMiddleName = _utils.ParseCertAttribute(senderOrganization.Certificate.Subject, "G");
                string signerFirstName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
                string signerMiddleName = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;

                signer = new[]
                {
                                new Diadoc.Api.DataXml.ExtendedSignerDetails_SellerTitle
                                {
                                    SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.LegalEntity,
                                    FirstName = signerFirstName,
                                    MiddleName = signerMiddleName,
                                    LastName = _utils.ParseCertAttribute(senderOrganization.Certificate.Subject, "SN"),
                                    SignerOrganizationName = _utils.ParseCertAttribute(senderOrganization.Certificate.Subject, "CN"),
                                    Inn = _utils.GetOrgInnFromCertificate(senderOrganization.Certificate),
                                    Position = _utils.ParseCertAttribute(senderOrganization.Certificate.Subject, "T")
                                }
                            };
            }
            else
            {
                signer = new[]
                {
                    new Diadoc.Api.DataXml.ExtendedSignerDetails_SellerTitle
                    {
                        SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.PhysicalPerson,
                        FirstName = senderOrganization.EmchdPersonName,
                        MiddleName = senderOrganization.EmchdPersonPatronymicSurname,
                        LastName = senderOrganization.EmchdPersonSurname,
                        SignerOrganizationName = senderOrganization.Name,
                        Inn = senderOrganization.EmchdPersonInn,
                        Position = senderOrganization.EmchdPersonPosition,
                        SignerPowersBase = "Доверенность"
                    }
                };
            }

            document.UseSignerDetails(signer);

            int docLineCount = d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice ? d.DocGoodsDetailsIs.Count : d.Details.Count;
            document.DocumentShipments = new Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphensDocumentShipment[]
            {
                                new Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphensDocumentShipment
                                {
                                    Name = "Реализация (акт, накладная, УПД)",
                                    Number = $"п/п 1-{docLineCount}, №{documentNumber}",
                                    Date = d.DeliveryDate?.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy")
                                }
            };

            var details = new List<Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItem>();

            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
            {
                foreach (var docJournalDetail in d.DocGoodsDetailsIs)
                {
                    var refGood = _abt.RefGoods?
                    .FirstOrDefault(r => r.Id == docJournalDetail.IdGood);

                    if (refGood == null)
                        continue;

                    var docGoodDetailLabels = detailsLabels.Where(l => l.IdGood == docJournalDetail.IdGood).ToList();

                    if (considerOnlyLabeledGoods && docGoodDetailLabels.Count == 0)
                        continue;

                    var barCode = _abt.RefBarCodes?
                        .FirstOrDefault(b => b.IdGood == docJournalDetail.IdGood && b.IsPrimary == false)?
                        .BarCode;

                    string countryCode = _abt.SelectSingleValue("select NUM_CODE from REF_COUNTRIES where id in" +
                        $"(select ID_COUNTRY from REF_GOODS where ID = {refGood.Id})");

                    int quantity;

                    if (considerOnlyLabeledGoods)
                        quantity = docGoodDetailLabels.Count;
                    else
                        quantity = docJournalDetail.Quantity;

                    var vat = (decimal)Math.Round(docJournalDetail.TaxSumm * quantity, 2);
                    var subtotal = Math.Round(quantity * ((decimal)docJournalDetail.Price - (decimal)docJournalDetail.DiscountSumm), 2);

                    var detail = new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItem
                    {
                        Product = refGood.Name,
                        Unit = Properties.Settings.Default.DefaultUnit,
                        Quantity = quantity,
                        QuantitySpecified = true,
                        VatSpecified = true,
                        Vat = vat,
                        PriceSpecified = true,
                        Price = (decimal)Math.Round(docJournalDetail.Price - docJournalDetail.DiscountSumm - docJournalDetail.TaxSumm, 2),
                        SubtotalSpecified = true,
                        Subtotal = subtotal,
                        SubtotalWithVatExcludedSpecified = true,
                        SubtotalWithVatExcluded = subtotal - vat,
                        ItemVendorCode = barCode,
                        CustomsDeclarations = new Diadoc.Api.DataXml.Utd820.Hyphens.CustomsDeclarationWithHyphens[]
                        {
                                        new Diadoc.Api.DataXml.Utd820.Hyphens.CustomsDeclarationWithHyphens
                                        {
                                            Country = countryCode
                                        }
                        }
                    };

                    if (!string.IsNullOrEmpty(refGood.CustomsNo))
                        detail.CustomsDeclarations.First().DeclarationNumber = refGood.CustomsNo;

                    switch (docJournalDetail.TaxRate)
                    {
                        case 0:
                            detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateWithTwentyPercentAndTaxedByAgent.Zero;
                            break;
                        case 10:
                            detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateWithTwentyPercentAndTaxedByAgent.TenPercent;
                            break;
                        case 18:
                            detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateWithTwentyPercentAndTaxedByAgent.EighteenPercent;
                            break;
                        case 20:
                            detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateWithTwentyPercentAndTaxedByAgent.TwentyPercent;
                            break;
                        default:
                            detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateWithTwentyPercentAndTaxedByAgent.Zero;
                            break;
                    }

                    decimal idGood = docJournalDetail.IdGood;

                    if (docGoodDetailLabels.Count > 0)
                    {
                        detail.ItemMark = Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemItemMark.PropertyRights;
                        detail.ItemIdentificationNumbers = new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemItemIdentificationNumber[1];
                        detail.ItemIdentificationNumbers[0] = new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemItemIdentificationNumber
                        {
                            ItemsElementName = new Diadoc.Api.DataXml.ItemsChoiceType[docGoodDetailLabels.Count],
                            Items = new string[docGoodDetailLabels.Count]
                        };

                        int j = 0;
                        foreach (var doc in docGoodDetailLabels)
                        {
                            detail.ItemIdentificationNumbers[0].ItemsElementName[j] = Diadoc.Api.DataXml.ItemsChoiceType.Unit;
                            detail.ItemIdentificationNumbers[0].Items[j] = doc.DmLabel;
                            j++;
                        }
                    }

                    details.Add(detail);
                }
            }
            else
            {
                foreach (var docJournalDetail in d.Details)
                {
                    var refGood = _abt.RefGoods?
                    .FirstOrDefault(r => r.Id == docJournalDetail.IdGood);

                    if (refGood == null)
                        continue;

                    var docGoodDetailLabels = detailsLabels.Where(l => l.IdGood == docJournalDetail.IdGood).ToList();

                    if (considerOnlyLabeledGoods && docGoodDetailLabels.Count == 0)
                        continue;

                    var barCode = _abt.RefBarCodes?
                        .FirstOrDefault(b => b.IdGood == docJournalDetail.IdGood && b.IsPrimary == false)?
                        .BarCode;

                    string countryCode = _abt.SelectSingleValue("select NUM_CODE from REF_COUNTRIES where id in" +
                        $"(select ID_COUNTRY from REF_GOODS where ID = {refGood.Id})");

                    int quantity;

                    if (considerOnlyLabeledGoods)
                        quantity = docGoodDetailLabels.Count;
                    else
                        quantity = docJournalDetail.Quantity;

                    var subtotal = Math.Round(quantity * ((decimal)docJournalDetail.Price - (decimal)docJournalDetail.DiscountSumm), 2);
                    var detail = new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItem
                    {
                        Product = refGood.Name,
                        Unit = Properties.Settings.Default.DefaultUnit,
                        Quantity = docJournalDetail.Quantity,
                        QuantitySpecified = true,
                        PriceSpecified = true,
                        Price = (decimal)Math.Round(docJournalDetail.Price - docJournalDetail.DiscountSumm, 2),
                        SubtotalSpecified = true,
                        Subtotal = subtotal,
                        SubtotalWithVatExcludedSpecified = true,
                        WithoutVat = Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemWithoutVat.True,
                        SubtotalWithVatExcluded = subtotal,
                        ItemVendorCode = barCode,
                        CustomsDeclarations = new Diadoc.Api.DataXml.Utd820.Hyphens.CustomsDeclarationWithHyphens[]
                        {
                                        new Diadoc.Api.DataXml.Utd820.Hyphens.CustomsDeclarationWithHyphens
                                        {
                                            Country = countryCode
                                        }
                        }
                    };

                    if (!string.IsNullOrEmpty(refGood.CustomsNo))
                        detail.CustomsDeclarations.First().DeclarationNumber = refGood.CustomsNo;

                    if (docGoodDetailLabels.Count > 0)
                    {
                        detail.ItemMark = Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemItemMark.PropertyRights;
                        detail.ItemIdentificationNumbers = new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemItemIdentificationNumber[1];
                        detail.ItemIdentificationNumbers[0] = new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemItemIdentificationNumber
                        {
                            ItemsElementName = new Diadoc.Api.DataXml.ItemsChoiceType[docGoodDetailLabels.Count],
                            Items = new string[docGoodDetailLabels.Count]
                        };

                        int j = 0;
                        foreach (var doc in docGoodDetailLabels)
                        {
                            detail.ItemIdentificationNumbers[0].ItemsElementName[j] = Diadoc.Api.DataXml.ItemsChoiceType.Unit;
                            detail.ItemIdentificationNumbers[0].Items[j] = doc.DmLabel;
                            j++;
                        }
                    }

                    details.Add(detail);
                }
            }

            document.Table.Item = details.ToArray();

            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
                document.Table.VatSpecified = true;
            else
                document.Table.WithoutVat = Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableWithoutVat.True;

            if (considerOnlyLabeledGoods)
            {
                document.Table.Total = details.Select(i => i.Subtotal).Sum();
                document.Table.TotalWithVatExcluded = details.Select(i => i.SubtotalWithVatExcluded).Sum();

                if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
                    document.Table.Vat = details.Select(i => i.Vat).Sum();
            }
            else
            {
                if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
                {
                    document.Table.Total = (decimal)d.DocGoodsI.TotalSumm;
                    document.Table.Vat = (decimal)d.DocGoodsI.TaxSumm;
                    document.Table.TotalWithVatExcluded = (decimal)(d.DocGoodsI.TotalSumm - d.DocGoodsI.TaxSumm);
                }
                else
                {
                    document.Table.Total = (decimal)(d?.DocGoods?.TotalSumm ?? 0);
                    document.Table.TotalWithVatExcluded = (decimal)(d?.DocGoods?.TotalSumm ?? 0);
                }
            }

            return document;
        }

        public Kontragent GetMyKontragent(string inn, string kpp = null)
        {
            var kontragent = _edo.GetKontragentByInnKpp(inn, kpp);

            List<X509Certificate2> personalCertificates = GetPersonalCertificates();

            var authoritySignDocuments = (from cust in _abt.RefCustomers
                                          where cust.Inn == inn && (cust.Kpp == kpp || kpp == null)
                                          join a in _abt.RefAuthoritySignDocuments
                                          on cust.Id equals a.IdCustomer
                                          where a.IsMainDefault
                                          select a)?.FirstOrDefault();

            if (authoritySignDocuments != null && !string.IsNullOrEmpty(authoritySignDocuments?.EmchdId))
            {
                kontragent.Certificate = personalCertificates
                    .Where(c => authoritySignDocuments.Inn == _utils.ParseCertAttribute(c.Subject, "ИНН").TrimStart('0') && _utils.IsCertificateValid(c))
                    .OrderByDescending(c => c.NotBefore).FirstOrDefault(c => string.IsNullOrEmpty(_utils.GetOrgInnFromCertificate(c)));
                kontragent.EmchdId = authoritySignDocuments.EmchdId;
                kontragent.EmchdBeginDate = authoritySignDocuments.EmchdBeginDate;
                kontragent.EmchdEndDate = authoritySignDocuments.EmchdEndDate;
                kontragent.EmchdPersonInn = authoritySignDocuments.Inn;
                kontragent.EmchdPersonSurname = authoritySignDocuments.Surname;
                kontragent.EmchdPersonName = authoritySignDocuments.Name;
                kontragent.EmchdPersonPatronymicSurname = authoritySignDocuments.PatronymicSurname;
                kontragent.EmchdPersonPosition = authoritySignDocuments.Position;
            }

            if (kontragent.Certificate == null || string.IsNullOrEmpty(authoritySignDocuments?.EmchdId))
            {
                kontragent.Certificate = personalCertificates
                    .Where(c => inn == _utils.GetOrgInnFromCertificate(c) && _utils.IsCertificateValid(c))
                    .OrderByDescending(c => c.NotBefore).FirstOrDefault();

                kontragent.SetNullEmchdValues();
            }

            if (kontragent.Certificate == null)
                throw new Exception($"Не найден сертификат организации с ИНН {kontragent.Inn}.");

            return kontragent;
        }

        public Kontragent GetConsignorFromDocument(UniversalTransferDocument document, decimal? idSubdivision = null)
        {
            if (idSubdivision == null)
                idSubdivision = document?.IdSubdivision;

            if (idSubdivision == null)
                throw new Exception("Не найдена организация");

            Kontragent kontragent = null;

            if (_consignors.Any(c => c.Key == idSubdivision.Value))
            {
                kontragent = _consignors.First(c => c.Key == idSubdivision.Value).Value;
            }
            else
            {
                RefCustomer consignor = (from s in _abt.RefSubdivisions
                                         where s.Id == idSubdivision join c in _abt.RefContractors on s.OldId equals c.Id
                                         where c.DefaultCustomer != null join r in _abt.RefCustomers on c.DefaultCustomer equals r.Id
                                         select r)?.FirstOrDefault();

                if (consignor == null)
                    throw new Exception("Не найден комитент");

                kontragent = GetMyKontragent(consignor.Inn, consignor.Kpp);

                if (kontragent == null)
                    throw new Exception("Не найден контрагент");

                _consignors.Add(idSubdivision.Value, kontragent);
            }

            if (kontragent == null)
                throw new Exception("Не найден контрагент");

            return kontragent;
        }

        private DocComissionEdoProcessing SendComissionDocumentForHonestMark(Kontragent myOrganization, UniversalTransferDocument document)
        {
            if (myOrganization == null)
                throw new Exception("Не задана организация.");

            if (myOrganization.Certificate == null)
                throw new Exception("Не задан сертификат организации.");

            if (document == null)
                throw new Exception("Не задан документ.");

            var idDocType = document.DocJournal.IdDocType;

            var labels = from label in _abt.DocGoodsDetailsLabels where label.IdDocSale == document.CurrentDocJournalId select label;

            if (labels.Count() == 0)
                return null;

            if ((document.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice &&
                (document?.DocJournal?.DocGoodsDetailsIs?.Exists(g => _abt.RefItems.Any(r => r.IdName == 30071 && r.IdGood == g.IdGood && r.Quantity == 1) &&
                (labels?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) < g.Quantity) ?? false)) ||
                (document.DocJournal.IdDocType != (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice &&
                (document?.DocJournal?.Details?.Exists(g => _abt.RefItems.Any(r => r.IdName == 30071 && r.IdGood == g.IdGood && r.Quantity == 1) &&
                (labels?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) < g.Quantity) ?? false)))
            {
                throw new Exception("Для некоторых товаров отсутствует маркировка.");
            }
            else if ((document.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice &&
                (document?.DocJournal?.DocGoodsDetailsIs?.Exists(g => _abt.RefItems.Any(r => r.IdName == 30071 && r.IdGood == g.IdGood && r.Quantity == 1) &&
                (labels?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) > g.Quantity) ?? false)) ||
                (document.DocJournal.IdDocType != (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice &&
                (document?.DocJournal?.Details?.Exists(g => _abt.RefItems.Any(r => r.IdName == 30071 && r.IdGood == g.IdGood && r.Quantity == 1) &&
                (labels?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) > g.Quantity) ?? false)))
            {
                throw new Exception("В данном документе есть избыток кодов маркировки.");
            }

            if (idDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
            {
                if (document.IdSubdivision == null)
                    throw new Exception("Не задана организация-комитент");

                var consignor = GetConsignorFromDocument(null, document.IdSubdivision);

                if (consignor == null)
                    throw new Exception("Не найден комитент.");

                var crypt = new WinApiCryptWrapper(consignor.Certificate);
                string employee = _abt.SelectSingleValue("select const_value from ref_const where id = 1200");
                _edo.Authenticate(false, consignor.Certificate, consignor.Inn);

                string documentNumber = document.DocJournal?.Code;
                var comissionDocument = CreateShipmentDocument(document.DocJournal, consignor, myOrganization, labels.ToList(), documentNumber, employee, true);
                var generatedFile = _edo.GenerateTitleXml("UniversalTransferDocument", "ДОП", "utd820_05_01_01_hyphen", 0, document);
                byte[] signature = crypt.Sign(generatedFile.Content, true);

                var signedContent = new Diadoc.Api.Proto.Events.SignedContent
                {
                    Content = generatedFile.Content
                };

                signedContent.Signature = signature ?? throw new Exception("Не удалось вычислить подпись.");

                Diadoc.Api.Proto.Events.PowerOfAttorneyToPost consignorPowerOfAttorneyToPost = null;

                if (!string.IsNullOrEmpty(consignor.EmchdId))
                    consignorPowerOfAttorneyToPost = new Diadoc.Api.Proto.Events.PowerOfAttorneyToPost
                    {
                        UseDefault = false,
                        FullId = new Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyFullId
                        {
                            RegistrationNumber = consignor.EmchdId,
                            IssuerInn = consignor.Inn
                        }
                    };

                var message = _edo.SendXmlDocument(consignor.OrgId, myOrganization.OrgId, false, new List<Diadoc.Api.Proto.Events.SignedContent>(new Diadoc.Api.Proto.Events.SignedContent[] { signedContent }), "ДОП", consignorPowerOfAttorneyToPost);

                _edo.Authenticate(false, myOrganization.Certificate, myOrganization.Inn);
                var buyerDocument = CreateBuyerShipmentDocument(myOrganization);
                crypt.InitializeCertificate(myOrganization.Certificate);

                var attachments = message.Entities.Where(t => t.AttachmentType == Diadoc.Api.Proto.Events.AttachmentType.UniversalTransferDocument).Select(ent =>
                {
                    var generatedBuyerFile = _edo.GenerateTitleXml("UniversalTransferDocument", "ДОП", "utd820_05_01_01_hyphen", 1, buyerDocument, message.MessageId, ent.EntityId);
                    return new Diadoc.Api.Proto.Events.RecipientTitleAttachment
                    {
                        ParentEntityId = ent.EntityId,
                        SignedContent = new Diadoc.Api.Proto.Events.SignedContent
                        {
                            Content = generatedBuyerFile.Content,
                            Signature = crypt.Sign(generatedBuyerFile.Content, true)
                        }
                    };
                });

                Diadoc.Api.Proto.Events.PowerOfAttorneyToPost myOrganizationPowerOfAttorneyToPost = null;

                if (!string.IsNullOrEmpty(myOrganization.EmchdId))
                    myOrganizationPowerOfAttorneyToPost = new Diadoc.Api.Proto.Events.PowerOfAttorneyToPost
                    {
                        UseDefault = false,
                        FullId = new Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyFullId
                        {
                            RegistrationNumber = myOrganization.EmchdId,
                            IssuerInn = myOrganization.Inn
                        }
                    };

                _edo.SendPatchRecipientXmlDocument(message.MessageId, (int)Diadoc.Api.Proto.DocumentType.UniversalTransferDocument,
                    attachments, myOrganizationPowerOfAttorneyToPost);

                var entity = message.Entities.FirstOrDefault(t => t.AttachmentType == Diadoc.Api.Proto.Events.AttachmentType.UniversalTransferDocument &&
                               t?.DocumentInfo?.DocumentNumber == document.DocJournal.Code);

                var fileNameLength = entity.DocumentInfo.FileName.LastIndexOf('.');

                if (fileNameLength < 0)
                    fileNameLength = entity.DocumentInfo.FileName.Length;

                var docComissionProcessing = new DocComissionEdoProcessing
                {
                    Id = Guid.NewGuid().ToString(),
                    MessageId = message.MessageId,
                    EntityId = entity.EntityId,
                    IdDoc = document.CurrentDocJournalId,
                    SenderInn = consignor.Inn,
                    ReceiverInn = myOrganization.Inn,
                    DocStatus = 1,
                    UserName = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser,
                    FileName = entity.DocumentInfo.FileName.Substring(0, fileNameLength),
                    DocDate = DateTime.Now,
                    DeliveryDate = document?.DocJournal?.DeliveryDate
                };

                if (!document.IsMarked)
                    docComissionProcessing.DocStatus = 2;

                document.ProcessingStatus = docComissionProcessing;
                _abt.DocComissionEdoProcessings.Add(docComissionProcessing);
                _abt.SaveChanges();
                return docComissionProcessing;
            }
            else
            {
                return null;
            }
        }
    }
}
