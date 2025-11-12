using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using EdiProcessingUnit.Edo;
using EdiProcessingUnit.Edo.Models;
using KonturEdoClient.Models.Implementations;
using UtilitesLibrary.Service;
using System.Xml;

namespace KonturEdoClient.Models
{
    public class MainModel : Base.ModelBase
    {
        private LoadModel _loadContext;
        private Utils.XmlSignUtils _utils;
        private AbtDbContext _abt;
        private System.Windows.Window _owner;
        private List<UniversalTransferDocument>[] _loadedDocuments;
        private UtilitesLibrary.Logger.UtilityLog _log = UtilitesLibrary.Logger.UtilityLog.GetInstance();
        private EdiProcessingUnit.UsersConfig _config;
        private Dictionary<decimal, Kontragent> _consignors;
        private bool _authInHonestMark;
        private List<X509Certificate2> _personalCertificates = null;

        public RelayCommand RefreshCommand => new RelayCommand((o) => { Refresh(); });
        public RelayCommand LoadXmlCommand => new RelayCommand((o) => { LoadXml(); });
        public RelayCommand SendCommand => new RelayCommand((o) => { Send(); });
        public RelayCommand ShowMarkedCodesCommand => new RelayCommand((o) => { ShowMarkedCodes(); });
        public RelayCommand ReprocessDocumentCommand => new RelayCommand((o) => { ReprocessDocument(); });
        public RelayCommand AnnulmentDocumentCommand => new RelayCommand((o) => { AnnulmentDocument(); });
        public RelayCommand GoodsMatchingCommand => new RelayCommand((o) => { GoodsMatching(); });
        public RelayCommand ShowDocumentSendHistoryCommand => new RelayCommand((o) => { ShowDocumentSendHistory(); });
        public RelayCommand LoadUnsentDocumentCommand => new RelayCommand((o) => { LoadUnsentDocument(); });
        public RelayCommand ShowCorrectionDocumentsCommand => new RelayCommand((o) => { ShowCorrectionDocuments(); });
        public RelayCommand UploadProductsAndCodesToEdoLiteCommand => new RelayCommand((o) => { UploadProductsAndCodesToEdoLite(); });
        public List<Kontragent> Organizations { get; set; }
        public Kontragent SelectedOrganization { get; set; }

        public DateTime DateTo { get; set; } = DateTime.Now;
        public DateTime DateFrom { get; set; } = DateTime.Today;
        public bool OnlyMarkedOrders { get; set; }
        public bool IsDocumentMarked { get { return SelectedDocument?.IsMarked ?? false; } }
        public bool WorkWithDocumentsPermission { get; set; }
        public bool PermissionShowMarkedCodes { get; set; }
        public bool PermissionReturnMarkedCodes { get; set; }
        public bool PermissionCompareGoods { get; set; }
        public bool PermissionChannelsList { get; set; }
        public bool PermissionChannelsSettings { get; set; }
        public bool DocumentWithErrorStatus => SelectedDocument?.IsMarkedDocumentProcessingError ?? false;
        public bool IsSended => WorkWithDocumentsPermission && SelectedDocument?.DocEdoSendStatus != null && SelectedDocument?.DocEdoSendStatus != "-";
        public bool IsSigned => WorkWithDocumentsPermission && (SelectedDocument?.DocEdoSendStatus == "Подписан контрагентом" || SelectedDocument?.DocEdoSendStatus == "Корректирован" ||
            SelectedDocument?.DocEdoSendStatus == "Подписан с расхождениями");
        

        public List<UniversalTransferDocument> Documents { get; set; }
        public List<UniversalTransferDocument> SelectedDocuments { get; set; }
        public UniversalTransferDocument SelectedDocument => SelectedDocuments?.FirstOrDefault();

        public List<KeyValuePair<DataContextManagementUnit.DataAccess.DocJournalType, string>> DocTypes { get; set; }
        public DataContextManagementUnit.DataAccess.DocJournalType? SelectedDocType => (DataContextManagementUnit.DataAccess.DocJournalType?)(_owner as MainWindow)?.DocTypesBar?.EditValue;

        public List<EdiProcessingUnit.User> Filials { get; set; }
        public EdiProcessingUnit.User SelectedFilial { get; set; }

        public List<UniversalTransferDocumentDetail> DocumentDetails { get; set; }
        public UniversalTransferDocumentDetail SelectedDetail { get; set; }

        public MainModel(AbtDbContext abt, Utils.XmlSignUtils utils = null)
        {
            _loadContext = new LoadModel();
            Organizations = new List<Kontragent>();
            _config = new EdiProcessingUnit.UsersConfig();
            _utils = utils ?? new Utils.XmlSignUtils();
            Filials = _config.Users;
            _abt = abt;
            SetDocTypes();
            SetPermissions();
            SetMyOrganizations();
            _consignors = new Dictionary<decimal, Kontragent>();
            SetFinDbConfiguration();
        }

        private void SetFinDbConfiguration()
        {
            _log.Log("SetFinDbConfiguration: начало метода");
            var finDbController = WebService.Controllers.FinDbController.GetInstance();

            try
            {
                var data = finDbController.GetCipherContentForConnect(Properties.Settings.Default.ApplicationName);
                var encBytes = Convert.FromBase64String(data);

                var passwordFileName = finDbController.GetConfigFileName();
                string currentDirectoryPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var passwordBytes = System.IO.File.ReadAllBytes($"{currentDirectoryPath}\\{passwordFileName}");
                var password = Encoding.UTF8.GetString(passwordBytes);

                var key = System.Security.Cryptography.SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(password));
                var iv = System.Security.Cryptography.MD5.Create().ComputeHash(Encoding.UTF8.GetBytes("dm8432n8t392m4x"));
                var aes = System.Security.Cryptography.Aes.Create();

                aes.Key = key;
                aes.IV = iv;
                aes.Mode = System.Security.Cryptography.CipherMode.CBC;
                aes.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
                var contentStr = Cryptography.Tools.SymmetricAlgoritm.Decrypt(encBytes, aes);
                finDbController.InitConfig(contentStr);
                _log.Log("SetFinDbConfiguration: успешно");
            }
            catch(Exception ex)
            {
                _log.Log("Ошибка в методе SetFinDbConfiguration. Exception: " + _log.GetRecursiveInnerException(ex));
            }
        }

        public void SetOwner(System.Windows.Window owner)
        {
            _owner = owner;
        }

        public void SetDocumentDetails()
        {
            if (SelectedDocument == null)
                return;

            if (SelectedDocument.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice
                && SelectedDocument.DocJournal != null)
            {
                List<DocGoodsDetailsI> col = SelectedDocument.DocJournal.DocGoodsDetailsIs;
                //&& !_abt.Entry(SelectedDocument.DocJournal).Collection(d => d.DocGoodsDetailsIs).IsLoaded)
                //_abt.Entry(SelectedDocument.DocJournal).Collection(d => d.DocGoodsDetailsIs).Load();
            }
            else if(SelectedDocument.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Translocation
                && SelectedDocument.DocJournal != null)
            {
                List<DocGoodsDetail> col = SelectedDocument.DocJournal.Details;
                //&& !_abt.Entry(SelectedDocument.DocJournal).Collection(d => d.Details).IsLoaded)
                //_abt.Entry(SelectedDocument.DocJournal).Collection(d => d.Details).Load();
            }

            DocumentDetails = SelectedDocument?.Details?.ToList();
            SelectedDetail = null;

            if (SelectedDocument.RefEdoGoodChannel as RefEdoGoodChannel == null)
            {
                var idChannel = SelectedDocument?.DocJournal?.DocMaster?.DocGoods?.Customer?.IdChannel;

                if(idChannel != null && idChannel != 99001)
                    SelectedDocument.RefEdoGoodChannel = (from r in _abt.RefContractors
                                                         where r.IdChannel == idChannel
                                                         join s in (from c in _abt.RefContractors
                                                                    where c.DefaultCustomer != null
                                                                    join refEdo in _abt.RefEdoGoodChannels on c.IdChannel equals (refEdo.IdChannel)
                                                                    select new { RefEdoGoodChannel = refEdo, RefContractor = c })
                                                                    on r.DefaultCustomer equals (s.RefContractor.DefaultCustomer)
                                                         where s != null select s.RefEdoGoodChannel);
            }

            if (SelectedDocument.RefEdoGoodChannel as RefEdoGoodChannel != null)
            {
                var refEdoGoodChannel = SelectedDocument.RefEdoGoodChannel as RefEdoGoodChannel;

                if (!string.IsNullOrEmpty(refEdoGoodChannel.DetailBuyerCodeUpdId))
                {
                    foreach (var detail in DocumentDetails)
                    {
                        detail.NotMapped = false;

                        if (detail.DocDetailI == null)
                            continue;

                        if (detail.GoodMatching == null)
                            detail.GoodMatching = (from refGoodMatching in _abt.RefGoodMatchings
                                                  where refGoodMatching.IdChannel == refEdoGoodChannel.IdChannel && refGoodMatching.IdGood == detail.DocDetailI.IdGood && refGoodMatching.Disabled == 0
                                                   select refGoodMatching).FirstOrDefault();

                        if(detail.GoodMatching == null && SelectedDocument?.DocJournal?.DocMaster != null)
                        {
                            var docDateTime = SelectedDocument.DocJournal.DocMaster.DocDatetime.Date;

                            detail.GoodMatching = (from refGoodMatching in _abt.RefGoodMatchings.Where(r => r.DisabledDatetime != null)
                                                   where refGoodMatching.IdChannel == refEdoGoodChannel.IdChannel && refGoodMatching.IdGood == detail.DocDetailI.IdGood && 
                                                   refGoodMatching.Disabled == 1 && refGoodMatching.DisabledDatetime.Value >= docDateTime
                                                   select refGoodMatching).FirstOrDefault();
                        }
                    }
                }
            }

            OnPropertyChanged("DocumentDetails");
            OnPropertyChanged("SelectedDetail");
            OnPropertyChanged("IsDocumentMarked");
            OnPropertyChanged("DocumentWithErrorStatus");
            OnPropertyChanged("IsSended");
            OnPropertyChanged("IsSigned");
        }

        private void Refresh()
        {
            if (_abt != null)
            {
                _abt.Dispose();
                _abt = null;
                UniversalTransferDocument.DbContext = null;
            }

            if (SelectedFilial != null)
            {
                _abt = new AbtDbContext(_config.GetConnectionStringByUser(SelectedFilial), true);
                SetPermissions();

                if(Organizations.Count == 0)
                    SetMyOrganizations();
                else
                {
                    var dataBaseUser = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser;
                    var customers = from cust in _abt.RefCustomers
                                    join refUser in _abt.RefUsersByOrgEdo
                                    on cust.Id equals refUser.IdCustomer
                                    where refUser.UserName == dataBaseUser
                                    select cust;

                    foreach (var org in Organizations)
                        org.Kpp = customers?.FirstOrDefault(c => c.Inn == org.Inn)?.Kpp;

                    SelectedOrganization = null;
                    OnAllPropertyChanged();
                }
            }
            else
                _abt = new AbtDbContext();

            var appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            _abt.ExecuteProcedure("DBMS_APPLICATION_INFO.set_client_info", new Oracle.ManagedDataAccess.Client.OracleParameter("client_info", appVersion));

            GetDocuments();
            SelectedOrganization = null;
            SelectedDocuments = new List<UniversalTransferDocument>();
            Documents = new List<UniversalTransferDocument>();
            DocumentDetails = new List<UniversalTransferDocumentDetail>();
            OnPropertyChanged("Documents");
            OnPropertyChanged("DocumentDetails");
            OnPropertyChanged("SelectedOrganization");
            OnPropertyChanged("SelectedDocuments");
        }

        private void SetDocTypes()
        {
            DocTypes = new List<KeyValuePair<DataContextManagementUnit.DataAccess.DocJournalType, string>>();

            DocTypes.Add(new KeyValuePair<DataContextManagementUnit.DataAccess.DocJournalType, string>(DataContextManagementUnit.DataAccess.DocJournalType.Invoice, "Счёт-фактура"));
            DocTypes.Add(new KeyValuePair<DataContextManagementUnit.DataAccess.DocJournalType, string>(DataContextManagementUnit.DataAccess.DocJournalType.Translocation, "Перемещение"));
            //DocTypes.Add(new KeyValuePair<DataContextManagementUnit.DataAccess.DocJournalType, string>(DataContextManagementUnit.DataAccess.DocJournalType.Correction, "Корректировка"));
        }

        private void SetPermissions()
        {
            var param = new Oracle.ManagedDataAccess.Client.OracleParameter("UName", UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser);
            param.OracleDbType = Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2;

            var userEdoPermission = _abt.Database.SqlQuery<RefUserEdoPermissions>("select USER_NAME as UserName," +
                "WORK_WITH_DOCUMENTS_PERMISSION as WorkWithDocuments, PERMISSION_SHOW_MARKED_CODES as ShowMarkedCodes," +
                $"PERMISSION_RETURN_MARKED_CODES as ReturnMarkedCodes, PERMISSION_COMPARE_GOODS as PermissionCompareGoods," +
                $"PERMISSION_CHANNELS_LIST as PermissionChannelsList, PERMISSION_CHANNELS_SETTINGS as PermissionChannelsSettings " +
                $"from EDI.REF_USER_EDO_PERMISSIONS where USER_NAME = :UName", param).FirstOrDefault();

            if(userEdoPermission != null)
            {
                WorkWithDocumentsPermission = userEdoPermission.WorkWithDocuments != 0;
                PermissionShowMarkedCodes = userEdoPermission.ShowMarkedCodes != 0;
                PermissionReturnMarkedCodes = userEdoPermission.ReturnMarkedCodes != 0;
                PermissionCompareGoods = userEdoPermission.PermissionCompareGoods != 0;
                PermissionChannelsList = userEdoPermission.PermissionChannelsList != 0;
                PermissionChannelsSettings = userEdoPermission.PermissionChannelsSettings != 0;
            }
        }

        private Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.UniversalTransferDocumentBuyerTitle CreateBuyerShipmentDocument(Kontragent receiverOrganization)
        {
            Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.Signer signer;
            Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.EmployeeUtd970 employee;

            if (string.IsNullOrEmpty(receiverOrganization.EmchdId))
            {
                var firstMiddleName = _utils.ParseCertAttribute(receiverOrganization.Certificate.Subject, "G");
                string signerFirstName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
                string signerMiddleName = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;

                signer = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.Signer
                {
                    Fio = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.Fio
                    {
                        FirstName = signerFirstName,
                        MiddleName = signerMiddleName,
                        LastName = _utils.ParseCertAttribute(receiverOrganization.Certificate.Subject, "SN")
                    },
                    Position = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.SignerPosition
                    {
                        PositionSource = Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.SignerPositionPositionSource.Manual,
                        Value = _utils.ParseCertAttribute(receiverOrganization.Certificate.Subject, "T")
                    },
                    SignatureTypeSpecified = true,
                    SignatureType = Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.SignerSignatureType.Item1,
                    SigningDate = DateTime.Now.ToString("dd.MM.yyyy"),
                    SignerPowersConfirmationMethodSpecified = true,
                    SignerPowersConfirmationMethod = Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.SignerSignerPowersConfirmationMethod.Item1
                };

                employee = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.EmployeeUtd970
                {
                    Position = signer.Position.Value,
                    LastName = signer.Fio.LastName,
                    MiddleName = signer.Fio.MiddleName,
                    FirstName = signer.Fio.FirstName
                };
            }
            else
            {
                signer = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.Signer
                {
                    Fio = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.Fio
                    {
                        FirstName = receiverOrganization.EmchdPersonName,
                        MiddleName = receiverOrganization.EmchdPersonPatronymicSurname,
                        LastName = receiverOrganization.EmchdPersonSurname
                    },
                    Position = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.SignerPosition
                    {
                        PositionSource = Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.SignerPositionPositionSource.Manual,
                        Value = receiverOrganization.EmchdPersonPosition
                    },
                    SignatureTypeSpecified = true,
                    SignatureType = Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.SignerSignatureType.Item1,
                    SigningDate = DateTime.Now.ToString("dd.MM.yyyy"),
                    SignerPowersConfirmationMethodSpecified = true,
                    SignerPowersConfirmationMethod = Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.SignerSignerPowersConfirmationMethod.Item3,
                    PowerOfAttorney = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.PowerOfAttorney
                    {
                        Electronic = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.Electronic
                        {
                            Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.Storage
                            {
                                UseDefault = Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.StorageUseDefault.@false,
                                FullId = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.StorageFullId
                                {
                                    RegistrationNumber = receiverOrganization.EmchdId,
                                    IssuerInn = receiverOrganization.Inn
                                }
                            }
                        }
                    }
                };
                employee = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.EmployeeUtd970
                {
                    Position = receiverOrganization.EmchdPersonPosition,
                    FirstName = receiverOrganization.EmchdPersonName,
                    MiddleName = receiverOrganization.EmchdPersonPatronymicSurname,
                    LastName = receiverOrganization.EmchdPersonSurname
                };
            }

            var document = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.UniversalTransferDocumentBuyerTitle()
            {
                Signers = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.Signers
                {
                    Signer = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.Signer[] 
                    {
                        signer
                    }
                },
                OperationContent = "Товары (работы, услуги, права) приняты без расхождений (претензий)",
                ContentOperCode = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.UniversalTransferDocumentBuyerTitleContentOperCode
                {
                    TotalCode = Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.UniversalTransferDocumentBuyerTitleContentOperCodeTotalCode.Item1
                },
                Employee = employee,
                AcceptanceDate = DateTime.Now.ToString("dd.MM.yyyy")
            };

            if (!string.IsNullOrEmpty(receiverOrganization.EmchdId))
                document.DocumentCreator = $"{receiverOrganization.EmchdPersonSurname} {receiverOrganization.EmchdPersonName} {receiverOrganization.EmchdPersonPatronymicSurname}";
            else
                document.DocumentCreator = _utils.ParseCertAttribute(receiverOrganization.Certificate.Subject, "SN") + " " + _utils.ParseCertAttribute(receiverOrganization.Certificate.Subject, "G");

            return document;
        }

        private Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument CreateShipmentDocument(
            DocJournal d, Kontragent senderOrganization, Kontragent receiverOrganization, List<DocGoodsDetailsLabels> detailsLabels, string documentNumber, string employee = null, bool considerOnlyLabeledGoods = false)
        {
            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && d.DocMaster == null)
                return null;

            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && d.DocGoodsI == null)
                return null;

            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Translocation && d.DocGoods == null)
                return null;

            var document = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument()
            {
                Function = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocumentFunction.ДОП,
                DocumentNumber = documentNumber,
                DocumentDate = d.DeliveryDate?.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy"),
                Currency = Properties.Settings.Default.DefaultCurrency,
                Table = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTable
                {
                    TotalSpecified = true,
                    TotalWithVatExcludedSpecified = true
                },
                TransferInfo = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TransferInfo
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
                document.TransferInfo.Employee = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.EmployeeUtd970
                {
                    Position = Properties.Settings.Default.DefaultEmployePosition,
                    EmployeeInfo = employee,
                    LastName = employee.Substring(0, employee.IndexOf(' ')),
                    FirstName = employee.Substring(employee.IndexOf(' ') + 1)
                };
            }


            Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.RussianAddressUtd970 senderAddress = senderOrganization?.Address?.RussianAddress != null ?
                            new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.RussianAddressUtd970
                            {
                                ZipCode = senderOrganization.Address.RussianAddress.ZipCode,
                                Region = senderOrganization.Address.RussianAddress.Region,
                                Street = string.IsNullOrEmpty(senderOrganization?.Address?.RussianAddress?.Street) ? null : senderOrganization.Address.RussianAddress.Street,
                                City = string.IsNullOrEmpty(senderOrganization?.Address?.RussianAddress?.City) ? null : senderOrganization.Address.RussianAddress.City,
                                Locality = string.IsNullOrEmpty(senderOrganization?.Address?.RussianAddress?.Locality) ? null : senderOrganization.Address.RussianAddress.Locality,
                                Territory = string.IsNullOrEmpty(senderOrganization?.Address?.RussianAddress?.Territory) ? null : senderOrganization.Address.RussianAddress.Territory,
                                Building = string.IsNullOrEmpty(senderOrganization?.Address?.RussianAddress?.Building) ? null : senderOrganization.Address.RussianAddress.Building
                            } : null;

            document.Sellers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970[] 
            {
                new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970
                {
                    Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationDetailsUtd970
                    {
                        Inn = senderOrganization.Inn,
                        Kpp = senderOrganization.Kpp,
                        OrgType = senderOrganization.Inn.Length == 12 ? Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item1 : Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item2,
                        OrgName = senderOrganization.Name,
                        Address = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AddressUtd970
                        {
                            Item = senderAddress
                        }
                    }
                }
            };

            Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.RussianAddressUtd970 receiverAddress = receiverOrganization?.Address?.RussianAddress != null ?
                new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.RussianAddressUtd970
                {
                    ZipCode = receiverOrganization.Address.RussianAddress.ZipCode,
                    Region = receiverOrganization.Address.RussianAddress.Region,
                    Street = string.IsNullOrEmpty(receiverOrganization?.Address?.RussianAddress?.Street) ? null : receiverOrganization.Address.RussianAddress.Street,
                    City = string.IsNullOrEmpty(receiverOrganization?.Address?.RussianAddress?.City) ? null : receiverOrganization.Address.RussianAddress.City,
                    Locality = string.IsNullOrEmpty(receiverOrganization?.Address?.RussianAddress?.Locality) ? null : receiverOrganization.Address.RussianAddress.Locality,
                    Territory = string.IsNullOrEmpty(receiverOrganization?.Address?.RussianAddress?.Territory) ? null : receiverOrganization.Address.RussianAddress.Territory,
                    Building = string.IsNullOrEmpty(receiverOrganization?.Address?.RussianAddress?.Building) ? null : receiverOrganization.Address.RussianAddress.Building
                } : null;

            document.Buyers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970[] 
            {
                new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970
                {
                    Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationDetailsUtd970
                    {
                        Inn = receiverOrganization.Inn,
                        Kpp = receiverOrganization.Kpp,
                        OrgType = receiverOrganization.Inn.Length == 12 ? Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item1 : Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item2,
                        OrgName = receiverOrganization.Name,
                        Address = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AddressUtd970
                        {
                            Item = receiverAddress
                        }
                    }
                }
            };

            Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signer signer;
            if (string.IsNullOrEmpty(senderOrganization.EmchdId))
            {
                var firstMiddleName = _utils.ParseCertAttribute(senderOrganization.Certificate.Subject, "G");
                string signerFirstName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
                string signerMiddleName = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;

                signer = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signer
                {
                    Fio = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Fio
                    {
                        FirstName = signerFirstName,
                        MiddleName = signerMiddleName,
                        LastName = _utils.ParseCertAttribute(senderOrganization.Certificate.Subject, "SN")
                    },
                    Position = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerPosition
                    {
                        PositionSource = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerPositionPositionSource.Manual,
                        Value = _utils.ParseCertAttribute(senderOrganization.Certificate.Subject, "T")
                    },
                    SignatureTypeSpecified = true,
                    SignatureType = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerSignatureType.Item1,
                    SigningDate = DateTime.Now.ToString("dd.MM.yyyy"),
                    SignerPowersConfirmationMethodSpecified = true,
                    SignerPowersConfirmationMethod = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerSignerPowersConfirmationMethod.Item1
                };
            }
            else
            {
                signer = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signer
                {
                    Fio = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Fio
                    {
                        FirstName = senderOrganization.EmchdPersonName,
                        MiddleName = senderOrganization.EmchdPersonPatronymicSurname,
                        LastName = senderOrganization.EmchdPersonSurname
                    },
                    Position = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerPosition
                    {
                        PositionSource = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerPositionPositionSource.Manual,
                        Value = senderOrganization.EmchdPersonPosition
                    },
                    SignatureTypeSpecified = true,
                    SignatureType = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerSignatureType.Item1,
                    SigningDate = DateTime.Now.ToString("dd.MM.yyyy"),
                    SignerPowersConfirmationMethodSpecified = true,
                    SignerPowersConfirmationMethod = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerSignerPowersConfirmationMethod.Item3,
                    PowerOfAttorney = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.PowerOfAttorney
                    {
                        Electronic = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Electronic
                        {
                            Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Storage
                            {
                                UseDefault = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.StorageUseDefault.@false,
                                FullId = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.StorageFullId
                                {
                                    RegistrationNumber = senderOrganization.EmchdId,
                                    IssuerInn = senderOrganization.Inn
                                }
                            }
                        }
                    }
                };
            }

            document.Signers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signers
            {
                Signer = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signer[]
                {
                    signer
                }
            };

            int docLineCount = d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice ? d.DocGoodsDetailsIs.Count : d.Details.Count;
            document.DocumentShipments = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.DocumentRequisitesType[]
            {
                                new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.DocumentRequisitesType
                                {
                                    DocumentName = "Документ об отгрузке товаров (выполнении работ), передаче имущественных прав (документ об оказании услуг)",
                                    DocumentNumber = documentNumber,
                                    DocumentDate = d.DeliveryDate?.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy")
                                }
            };

            var details = new List<Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItem>();

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

                    var detail = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItem
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
                        ItemVendorCode = barCode
                    };

                    if (!string.IsNullOrEmpty(refGood.CustomsNo))
                    {
                        detail.CustomsDeclarations = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemCustomsDeclaration[]
                        {
                            new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemCustomsDeclaration
                            {
                                Country = countryCode,
                                DeclarationNumber = refGood.CustomsNo
                            }
                        };
                    }

                    switch (docJournalDetail.TaxRate)
                    {
                        case 0:
                            detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateUtd970.ZeroPercent;
                            break;
                        case 10:
                            detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateUtd970.TenPercent;
                            break;
                        //case 18:
                        //    detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateUcd736AndUtd820.Item18;
                        //    break;
                        case 20:
                            detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateUtd970.TwentyPercent;
                            break;
                        default:
                            detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateUtd970.NoVat;
                            break;
                    }

                    decimal idGood = docJournalDetail.IdGood;

                    if (docGoodDetailLabels.Count > 0)
                    {
                        detail.ItemMark = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemMark.Item4;
                        detail.ItemIdentificationNumbers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemIdentificationNumber[1];
                        detail.ItemIdentificationNumbers[0] = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemIdentificationNumber
                        {
                            ItemsElementName = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ItemsChoiceType[docGoodDetailLabels.Count],
                            Items = new string[docGoodDetailLabels.Count]
                        };

                        int j = 0;
                        foreach (var doc in docGoodDetailLabels)
                        {
                            detail.ItemIdentificationNumbers[0].ItemsElementName[j] = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ItemsChoiceType.Unit;
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
                    var detail = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItem
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
                        WithoutVat = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemWithoutVat.@true,
                        SubtotalWithVatExcluded = subtotal,
                        ItemVendorCode = barCode
                    };

                    if (!string.IsNullOrEmpty(refGood.CustomsNo))
                    {
                        detail.CustomsDeclarations = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemCustomsDeclaration[]
                        {
                            new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemCustomsDeclaration
                            {
                                Country = countryCode,
                                DeclarationNumber = refGood.CustomsNo
                            }
                        };
                    }

                    if (docGoodDetailLabels.Count > 0)
                    {
                        detail.ItemMark = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemMark.Item4;
                        detail.ItemIdentificationNumbers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemIdentificationNumber[1];
                        detail.ItemIdentificationNumbers[0] = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemIdentificationNumber
                        {
                            ItemsElementName = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ItemsChoiceType[docGoodDetailLabels.Count],
                            Items = new string[docGoodDetailLabels.Count]
                        };

                        int j = 0;
                        foreach (var doc in docGoodDetailLabels)
                        {
                            detail.ItemIdentificationNumbers[0].ItemsElementName[j] = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ItemsChoiceType.Unit;
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
                document.Table.WithoutVat = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableWithoutVat.@true;

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

        public Kontragent TryGetConsignorFromDocument(UniversalTransferDocument document, decimal? idSubdivision = null)
        {
            try
            {
                var kontragent = GetConsignorFromDocument(document, idSubdivision);
                return kontragent;
            }
            catch (System.Net.WebException webEx)
            {
                var errorWindow = new ErrorWindow(
                    "Произошла ошибка на удалённом сервере.",
                    new List<string>(
                        new string[]
                        {
                                    webEx.Message,
                                    webEx.StackTrace
                        }
                        ));

                errorWindow.ShowDialog();
                _log.Log("Ошибка на удалённом сервере: " + _log.GetRecursiveInnerException(webEx));
                return null;
            }
            catch (Exception ex)
            {
                var errorWindow = new ErrorWindow(
                    "Произошла ошибка поиска комитента.",
                    new List<string>(
                        new string[]
                        {
                                    ex.Message,
                                    ex.StackTrace
                        }
                        ));

                errorWindow.ShowDialog();
                _log.Log("Ошибка поиска комитента: " + _log.GetRecursiveInnerException(ex));
                return null;
            }
            finally
            {
                _log.Log($"Результат авторизации в честном знаке: {_authInHonestMark.ToString()}");
            }
        }

        public RefCustomer GetConsignor(decimal idSubdivision)
        {
            var consignor = (from s in _abt.RefSubdivisions where s.Id == idSubdivision
                             join c in _abt.RefContractors on s.OldId equals c.Id
                             where c.DefaultCustomer != null join r in _abt.RefCustomers
                             on c.DefaultCustomer equals r.Id select r)?.FirstOrDefault();

            if (consignor == null)
                throw new Exception("Не найден комитент");

            return consignor;
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
                RefCustomer consignor = GetConsignor(idSubdivision.Value);

                if (consignor == null)
                    throw new Exception("Не найден комитент");

                kontragent = GetMyKontragent(consignor.Inn, consignor.Kpp);

                if (kontragent == null)
                    throw new Exception("Не найден контрагент");

                _consignors.Add(idSubdivision.Value, kontragent);
            }

            if (kontragent == null)
                throw new Exception("Не найден контрагент");

            try
            {
                _authInHonestMark = EdiProcessingUnit.HonestMark.HonestMarkClient.GetInstance().Authorization(kontragent.Certificate, kontragent);
            }
            catch (Exception ex)
            {
                _authInHonestMark = false;
                throw ex;
            }

            if (!_authInHonestMark)
                throw new Exception("Не удалось авторизоваться в честном знаке");

            return kontragent;
        }

        public Kontragent GetMyKontragent(string inn, string kpp = null)
        {
            var kontragent = Edo.GetInstance().GetKontragentByInnKpp(inn, kpp);

            List<X509Certificate2> personalCertificates = GetPersonalCertificates();

            var authoritySignDocuments = (from cust in _abt.RefCustomers
                            where cust.Inn == inn && (cust.Kpp == kpp || kpp == null)
                            join a in _abt.RefAuthoritySignDocuments
                            on cust.Id equals a.IdCustomer
                            where a.IsMainDefault
                            select a)?.FirstOrDefault();

            if(authoritySignDocuments != null && !string.IsNullOrEmpty(authoritySignDocuments?.EmchdId))
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
            
            if(kontragent.Certificate == null || string.IsNullOrEmpty(authoritySignDocuments?.EmchdId))
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

        public async void GetDocuments(bool loadOnlyUnsentDocuments = false)
        {
            _log.Log($"GetDocuments: загрузка документов");
            _loadContext = new LoadModel();
            var loadWindow = new LoadWindow();
            loadWindow.DataContext = _loadContext;

            if(_owner != null)
                loadWindow.Owner = _owner;

            loadWindow.Show();

            _loadedDocuments = new List<UniversalTransferDocument>[Organizations.Count];
            Exception exception = null;

            var errorsList = new List<Exception>();

            await Task.Run(() =>
            {
                try
                {
                    _log.Log($"Загрузка документов для организаций");
                    int index = 0;
                    var toDate = DateTo.AddDays(1).ToString();

                    var docs = _abt.Set<DocJournal>().Include("DocGoodsDetailsIs");

                    foreach (var organization in Organizations)
                    {
                        _log.Log($"Загрузка документов для организации {organization.Name}");
                        organization.Index = index;
                        _loadedDocuments[index] = new List<UniversalTransferDocument>();

                        if (organization.Certificate == null)
                        {
                            _log.Log($"Для организации не найден сертификат.");
                            index++;
                            continue;
                        }

                        try
                        {
                            var result = Edo.GetInstance().Authenticate(false, organization.Certificate, organization.Inn);

                            if (!result)
                                throw new Exception($"Не удалось авторизоваться в системе по сертификату {organization.Name}.");
                        }
                        catch(Exception ex)
                        {
                            string errorMessage = $"Ошибка авторизации по сертификату в Диадоке, организация {organization.Name} \nException: {_log.GetRecursiveInnerException(ex)}";
                            _log.Log(errorMessage);

                            errorsList.Add(ex);
                            index++;
                            continue;
                        }

                        var addressSender = organization?.Address?.RussianAddress?.ZipCode +
                                        (string.IsNullOrEmpty(organization?.Address?.RussianAddress?.City) ? "" : $", {organization.Address.RussianAddress.City}") +
                                        (string.IsNullOrEmpty(organization?.Address?.RussianAddress?.Street) ? "" : $", {organization.Address.RussianAddress.Street}") +
                                        (string.IsNullOrEmpty(organization?.Address?.RussianAddress?.Building) ? "" : $", {organization.Address.RussianAddress.Building}");

                        var senderInnKpp = organization.Inn + "/" + organization.Kpp;
                        var updDocType = (int)EdiProcessingUnit.Enums.DocEdoType.Upd;
                        var ucdDocType = (int)EdiProcessingUnit.Enums.DocEdoType.Ucd;

                        if(SelectedDocType == DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
                        {
                            UniversalTransferDocument.DbContext = _abt;
                            _loadedDocuments[index] = (from doc in docs
                                                       where doc != null && doc.DocGoodsI != null && doc.DocMaster != null && doc.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && doc.DocDatetime >= DateFrom
                                                       join docMaster in _abt.DocJournals on doc.IdDocMaster equals docMaster.Id
                                                       where docMaster != null && docMaster.DocGoods != null && docMaster.DocDatetime >= DateFrom && docMaster.DocDatetime < DateTo
                                                       join docGoods in _abt.DocGoods on docMaster.Id equals docGoods.IdDoc
                                                       join customer in _abt.RefCustomers on docGoods.IdSeller equals customer.IdContractor
                                                       where customer.Inn == organization.Inn && customer.Kpp == organization.Kpp
                                                       let isMarked = (from label in _abt.DocGoodsDetailsLabels where label.IdDocSale == docMaster.Id select label).Count() > 0
                                                       let docEdoProcessing = (from docEdo in _abt.DocEdoProcessings
                                                                               where docEdo.IdDoc == docMaster.Id && docEdo.DocType == updDocType
                                                                               orderby docEdo.DocDate descending
                                                                               select docEdo)
                                                       where isMarked || !OnlyMarkedOrders
                                                       where (!loadOnlyUnsentDocuments) || docEdoProcessing.Count() == 0
                                                       join buyerContractor in _abt.RefContractors
                                                       on docGoods.IdCustomer equals buyerContractor.Id
                                                       //let edoGoodChannel = (from refEdoGoodChannel in _abt.RefEdoGoodChannels
                                                       //                      where refEdoGoodChannel.IdChannel == buyerContractor.IdChannel
                                                       //                      select refEdoGoodChannel)
                                                       join buyerCustomer in _abt.RefCustomers
                                                   on buyerContractor.DefaultCustomer equals buyerCustomer.Id
                                                       select new UniversalTransferDocument
                                                       {
                                                           Total = doc.DocGoodsI.TotalSumm,
                                                           Vat = doc.DocGoodsI.TaxSumm,
                                                           TotalWithVatExcluded = (doc.DocGoodsI.TotalSumm - doc.DocGoodsI.TaxSumm),
                                                           EdoProcessing = docEdoProcessing,
                                                           DocJournal = doc,
                                                           DocJournalNumber = docMaster.Code,
                                                           DocumentNumber = doc.Code,
                                                           ActStatus = docMaster.ActStatus,
                                                           OrgId = organization.OrgId,

                                                           SenderName = organization.Name,

                                                           SenderInnKpp = senderInnKpp,

                                                           SenderAddress = addressSender,

                                                           BuyerCustomer = buyerCustomer,

                                                           SellerContractor = docMaster.DocGoods.Seller,

                                                           BuyerContractor = buyerContractor,

                                                           //RefEdoGoodChannel = edoGoodChannel,

                                                           IsMarked = isMarked
                                                       }).ToList();
                        }
                        else if(SelectedDocType == DataContextManagementUnit.DataAccess.DocJournalType.Translocation)
                            _loadedDocuments[index] = (from doc in docs
                                                       where doc != null && doc.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Translocation && doc.DocDatetime >= DateFrom && doc.DocDatetime < DateTo
                                                       join docGoods in _abt.DocGoods on doc.Id equals docGoods.IdDoc
                                                       where docGoods.IdCustomer != 0
                                                       join customer in _abt.RefCustomers on docGoods.IdSeller equals customer.IdContractor
                                                       where customer.Inn == organization.Inn && customer.Kpp == organization.Kpp
                                                       let isMarked = (from label in _abt.DocGoodsDetailsLabels where label.IdDocSale == doc.Id select label).Count() > 0
                                                       where isMarked || !OnlyMarkedOrders
                                                       join storeRecipient in _abt.RefStores
                                                       on docGoods.IdStoreReciepient equals storeRecipient.Id
                                                       join storeSender in _abt.RefStores
                                                       on docGoods.IdStoreSender equals storeSender.Id
                                                       join buyerCustomer in _abt.RefCustomers
                                                       on docGoods.IdCustomer equals buyerCustomer.IdContractor
                                                       select new UniversalTransferDocument
                                                       {
                                                           Total = doc.DocGoods.TotalSumm ?? 0,
                                                           //Vat = doc.DocGoods.TaxSumm,
                                                           TotalWithVatExcluded = doc.DocGoods.TotalSumm ?? 0,
                                                           DocJournal = doc,
                                                           DocJournalNumber = doc.Code,
                                                           DocumentNumber = doc.Code,
                                                           ActStatus = doc.ActStatus,
                                                           OrgId = organization.OrgId,

                                                           SenderName = organization.Name,

                                                           SenderInnKpp = senderInnKpp,

                                                           SenderAddress = addressSender,

                                                           BuyerCustomer = buyerCustomer,

                                                           StoreSender = storeSender,

                                                           StoreRecipient = storeRecipient,

                                                           IsMarked = isMarked
                                                       }).ToList();

                        if (loadOnlyUnsentDocuments)
                        {
                            _log.Log($"Для организации {organization.Name} получено {_loadedDocuments[index].Count} ранее не отправленных документов.");
                            index++;
                            continue;
                        }

                        Exception authInHonestMarkException = null;
                        try
                        {
                            _authInHonestMark = EdiProcessingUnit.HonestMark.HonestMarkClient.GetInstance().Authorization(organization.Certificate, organization);

                            if (!_authInHonestMark)
                                _log.Log("Не удалось авторизоваться в Честном знаке");
                        }
                        catch (Exception ex)
                        {
                            _authInHonestMark = false;
                            _log.Log($"Ошибка авторизации в Честном знаке: {_log.GetRecursiveInnerException(ex)}");
                            authInHonestMarkException = ex;
                        }

                        if (authInHonestMarkException == null && !_authInHonestMark)
                        {
                            _log.Log($"Для организации {organization.Name} получено документов {_loadedDocuments[index].Count}");
                            index++;
                            continue;
                        }

                        try
                        {
                            if (authInHonestMarkException != null)
                                throw authInHonestMarkException;

                            var docProcessingsForChecking = (from doc in _loadedDocuments[index]
                                                             where doc.EdoProcessing as DocEdoProcessing != null &&
                                                             (doc.EdoProcessing as DocEdoProcessing).HonestMarkStatus == (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Processed
                                                             select (DocEdoProcessing)doc.EdoProcessing)
                                                             .Where(m => m.AnnulmentStatus == (int)EdiProcessingUnit.HonestMark.AnnulmentDocumentStatus.RevokedWaitProcessing);

                            if ((docProcessingsForChecking?.Count() ?? 0) != 0)
                            {
                                foreach (var docProcessing in docProcessingsForChecking)
                                {
                                    try
                                    {
                                        if (string.IsNullOrEmpty(docProcessing.AnnulmentFileName))
                                            throw new Exception($"Не указан файл аннулирования для документа {docProcessing.FileName}");

                                        var docProcessingInfo = EdiProcessingUnit.HonestMark.HonestMarkClient.GetInstance().GetEdoDocumentProcessInfo(docProcessing.AnnulmentFileName);

                                        if(docProcessingInfo.Code == EdiProcessingUnit.HonestMark.HonestMarkProcessResultStatus.FAILED)
                                        {
                                            docProcessing.AnnulmentStatus = (int)EdiProcessingUnit.HonestMark.AnnulmentDocumentStatus.Error;
                                            errorsList.Add(new Exception($"Возникла ошибка обработки документа {docProcessing.AnnulmentFileName} в Честном знаке"));
                                        }
                                        else if(docProcessingInfo.Code == EdiProcessingUnit.HonestMark.HonestMarkProcessResultStatus.SUCCESS)
                                        {
                                            docProcessing.AnnulmentStatus = (int)EdiProcessingUnit.HonestMark.AnnulmentDocumentStatus.RevokedAndProcessed;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        string errorMessage = $"Ошибка получения статуса обработки документа {docProcessing.AnnulmentFileName} \nException: {_log.GetRecursiveInnerException(ex)}";
                                        _log.Log(errorMessage);
                                        errorsList.Add(ex);
                                    }
                                }
                                _abt.SaveChanges();
                            }
                        }
                        catch(Exception ex)
                        {
                            string errorMessage = $"Произошла ошибка. \nException: {_log.GetRecursiveInnerException(ex)}";
                            _log.Log(errorMessage);
                            errorsList.Add(ex);
                        }

                        _log.Log($"Для организации {organization.Name} получено документов {_loadedDocuments[index].Count}");
                        index++;
                    }

                    loadWindow.SetSuccessFullLoad(_loadContext);
                    _log.Log($"GetDocuments: выполнено");
                }
                catch(Exception ex)
                {
                    exception = ex;
                    _log.Log($"GetDocuments Exception: {_log.GetRecursiveInnerException(exception)}");
                }
            });

            if (exception != null)
            {
                loadWindow.Close();

                var errorWindow = new ErrorWindow(
                            "Произошла ошибка загрузки документов.",
                            new List<string>(
                                new string[]
                                {
                                    exception.Message,
                                    exception.StackTrace
                                }
                                ));

                errorWindow.ShowDialog();
            }
            else if(errorsList.Count > 0)
            {
                var errorWindow = new ErrorWindow("Произошла ошибка в процессе обновления списка документов.", errorsList.Select(e => e.Message).ToList());

                errorWindow.ShowDialog();
            }
        }

        public void ChangeSelectedOrganization()
        {
            if (SelectedOrganization != null)
            {
                Documents = _loadedDocuments[SelectedOrganization.Index];
                DocumentDetails = new List<UniversalTransferDocumentDetail>();
                SelectedDocuments = new List<UniversalTransferDocument>();
                SelectedDetail = null;
            }

            OnPropertyChanged("Documents");
            OnPropertyChanged("DocumentDetails");
            OnPropertyChanged("SelectedDocuments");
            OnPropertyChanged("SelectedDetail");
            OnPropertyChanged("IsDocumentMarked");
            OnPropertyChanged("IsSended");
            OnPropertyChanged("IsSigned");
        }

        public void SetSelectedFilial(string userId)
        {
            SelectedFilial = Filials.FirstOrDefault(f => f.UserGLN == userId);
        }

        public void Dispose()
        {
            _abt?.Dispose();
            _abt = null;
            UniversalTransferDocument.DbContext = null;
        }

        private List<Kontragent> SetCounteragents(string boxIdGuid)
        {
            _log.Log($"SetCounteragents: загрузка контрагентов для организации {SelectedOrganization.Name}, OrgId: {SelectedOrganization.OrgId}");
            var counteragents = Edo.GetInstance().GetOrganizations(boxIdGuid);
            OnAllPropertyChanged();
            _log.Log("SetCounteragents: выполнено");
            return counteragents;
        }

        private void SetMyOrganizations()
        {
            _log.Log($"SetMyOrganizations: загрузка организаций для пользователя с именем {UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser}");

            var dataBaseUser = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser;
            var customers = from cust in _abt.RefCustomers
                            join refUser in _abt.RefUsersByOrgEdo
                            on cust.Id equals refUser.IdCustomer
                            where refUser.UserName == dataBaseUser
                            select cust;

            var authoritySignDocuments = from a in _abt.RefAuthoritySignDocuments
                                         where a.EmchdEndDate != null && a.IsMainDefault
                                         join c in customers
                                         on a.IdCustomer equals (c.Id)
                                         select a;

            var orgs = customers.ToArray().Select(c => 
            {
                var authoritySignDocument = authoritySignDocuments?.FirstOrDefault(a => a.IdCustomer == c.Id);

                if (authoritySignDocument == null)
                    return new Kontragent(c.Name, c.Inn, c.Kpp);

                return new Kontragent(c.Name, c.Inn, c.Kpp)
                {
                    EmchdId = authoritySignDocument?.EmchdId,
                    EmchdBeginDate = authoritySignDocument?.EmchdBeginDate,
                    EmchdEndDate = authoritySignDocument?.EmchdEndDate,
                    EmchdPersonInn = authoritySignDocument?.Inn,
                    EmchdPersonSurname = authoritySignDocument?.Surname,
                    EmchdPersonName = authoritySignDocument?.Name,
                    EmchdPersonPatronymicSurname = authoritySignDocument?.PatronymicSurname,
                    EmchdPersonPosition = authoritySignDocument?.Position
                };
            }).ToList();

            List < X509Certificate2 > personalCertificates = GetPersonalCertificates();

            foreach (var org in orgs)
            {
                if (!string.IsNullOrEmpty(org.EmchdPersonInn))
                {
                    var certs = personalCertificates.Where(c => org.EmchdPersonInn == _utils.ParseCertAttribute(c.Subject, "ИНН").TrimStart('0') && _utils.IsCertificateValid(c)).OrderByDescending(c => c.NotBefore);
                    org.Certificate = certs.FirstOrDefault(c => string.IsNullOrEmpty(_utils.GetOrgInnFromCertificate(c)));
                }
                
                if(org.Certificate == null || string.IsNullOrEmpty(org.EmchdPersonInn))
                {
                    if (org.Certificate == null && !string.IsNullOrEmpty(org.EmchdPersonInn))
                        org.SetNullEmchdValues();

                    var certs = personalCertificates.Where(c => org.Inn == _utils.GetOrgInnFromCertificate(c) && _utils.IsCertificateValid(c)).OrderByDescending(c => c.NotBefore);
                    org.Certificate = certs.FirstOrDefault();
                }

                if (org.Certificate != null)
                {
                    try
                    {
                        if (org.EmchdEndDate != null && org.EmchdEndDate.Value < DateTime.Now)
                            throw new Exception($"Срок доверенности истёк для физ лица с ИНН {org.EmchdPersonInn}");

                        Edo.GetInstance().Authenticate(false, org.Certificate, org.Inn);
                        Edo.GetInstance().SetOrganizationParameters(org);
                    }
                    catch(System.Net.WebException webEx)
                    {
                        var errorWindow = new ErrorWindow(
                            $"Произошла ошибка авторизации Диадока по сертификату {org.Certificate.Thumbprint} организации {org.Name}.",
                            new List<string>(
                                new string[]
                                {
                                    webEx.Message,
                                    webEx.StackTrace
                                }
                                ));

                        org.Certificate = null;
                        errorWindow.ShowDialog();
                        _log.Log("WebException: " + _log.GetRecursiveInnerException(webEx));
                    }
                    catch (Exception ex)
                    {
                        var errorWindow = new ErrorWindow(
                            $"Произошла ошибка авторизации Диадока по сертификату {org.Certificate.Thumbprint} организации {org.Name}.",
                            new List<string>(
                                new string[]
                                {
                                    ex.Message,
                                    ex.StackTrace
                                }
                                ));

                        org.Certificate = null;
                        errorWindow.ShowDialog();
                        _log.Log("Exception: " + _log.GetRecursiveInnerException(ex));
                    }
                }
            }

            Organizations = orgs.ToList();
            SelectedOrganization = null;
            OnAllPropertyChanged();
            _log.Log("SetMyOrganizations: выполнено");
        }

        private List<X509Certificate2> GetPersonalCertificates()
        {
            _log.Log("GetPersonalCertificates: загрузка сертификатов из хранилища Личные.");
            var crypto = new Cryptography.WinApi.WinApiCryptWrapper();

            if(_personalCertificates == null)
                _personalCertificates = crypto.GetAllGostPersonalCertificates()?.Where(c => c.NotAfter > DateTime.Now)?.ToList();

            _log.Log("GetPersonalCertificates: выполнено.");
            return _personalCertificates;
        }

        private void LoadXml()
        {
            _log.Log("LoadXml: открытие окна выбора файла.");
            if (SelectedDocument == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбран документ.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedDocuments.Count > 1)
            {
                System.Windows.MessageBox.Show(
                    "Для выгрузки нужно выбрать один документ.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedOrganization == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбрана организация.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedOrganization.Certificate == null)
            {
                System.Windows.MessageBox.Show(
                    "Не найден сертификат организации.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var changePathDialog = new Microsoft.Win32.SaveFileDialog();
            changePathDialog.Title = "Сохранение файла";
            changePathDialog.Filter = "XML Files|*.xml";

            try
            {
                bool result = Edo.GetInstance().Authenticate(false, SelectedOrganization.Certificate, SelectedOrganization.Inn);

                if (!result)
                    throw new Exception("Не удалось авторизоваться в системе по сертификату.");

                string employee = _abt.SelectSingleValue("select const_value from ref_const where id = 1200");

                var doc = GetUniversalDocument(SelectedDocument.DocJournal, SelectedOrganization, employee, SelectedDocument.RefEdoGoodChannel as RefEdoGoodChannel);
                var file = _utils.GetGeneratedFile(doc);
                changePathDialog.FileName = file.FileName;

                if (changePathDialog.ShowDialog() ?? false)
                {
                    file.SaveContentToFile(changePathDialog.FileName);
                    _loadContext = new LoadModel();
                    var loadWindow = new LoadWindow();
                    loadWindow.DataContext = _loadContext;
                    loadWindow.SetSuccessFullLoad(_loadContext, $"Файл xml успешно сохранён.");
                    loadWindow.ShowDialog();

                    _log.Log("LoadXml: успешно завершено.");

                }
            }
            catch (System.Net.WebException webEx)
            {
                var errorWindow = new ErrorWindow(
                        "Произошла ошибка на удалённом сервере.",
                        new List<string>(
                            new string[]
                            {
                                    webEx.Message,
                                    webEx.StackTrace
                            }
                            ));

                errorWindow.ShowDialog();
                _log.Log("WebException: " + _log.GetRecursiveInnerException(webEx));
            }
            catch (Exception ex)
            {
                var errorWindow = new ErrorWindow(
                        "Произошла ошибка.",
                        new List<string>(
                            new string[]
                            {
                                    ex.Message,
                                    ex.StackTrace
                            }
                            ));

                errorWindow.ShowDialog();
                _log.Log("Exception: " + _log.GetRecursiveInnerException(ex));
            }
        }

        private void Send()
        {
            _log.Log("Send: открытие окна отправки");
            if (SelectedOrganization == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбрана организация.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedOrganization.Certificate == null)
            {
                System.Windows.MessageBox.Show(
                    "Не найден сертификат отправителя.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedDocument == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбран документ.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedDocuments.Count > 1 && SelectedDocument.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Translocation)
            {
                System.Windows.MessageBox.Show(
                   "Не разрешается выбирать более одного документа перемещения для отправки.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if(SelectedDocuments.Count > 30)
            {
                System.Windows.MessageBox.Show(
                    "Превышен лимит по количеству документов - не более 30.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedDocuments.Exists(s => (s.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && s.DocJournal.DocMaster.ActStatus < 5) ||
            (s.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Translocation && s.DocJournal.ActStatus < 5)))
            {
                System.Windows.MessageBox.Show(
                    "Выбраны документы с невывезенным товаром.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                bool result = Edo.GetInstance().Authenticate(false, SelectedOrganization.Certificate, SelectedOrganization.Inn);

                if (!result)
                    throw new Exception("Не удалось авторизоваться в системе по сертификату.");

                var selectedOrganizationBoxIdGuid = Edo.GetInstance().ActualBoxIdGuid;
                SendWindow sendWindow = new SendWindow();

                string employee = _abt.SelectSingleValue("select const_value from ref_const where id = 1200");

                var refEdoGoodChannel = SelectedDocuments?.FirstOrDefault(s => s.RefEdoGoodChannel != null)?.RefEdoGoodChannel;

                var docs = SelectedDocuments.Where(s => s.CurrentDocJournalId != null)?
                    .Select(s => new KeyValuePair<decimal, Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument>(
                    s.CurrentDocJournalId.Value,
                    GetUniversalDocument(s.DocJournal, SelectedOrganization, employee, refEdoGoodChannel as RefEdoGoodChannel)));
                SendModel sendModel = new SendModel(_abt, SelectedOrganization, SelectedOrganization.Certificate, docs, (DataContextManagementUnit.DataAccess.DocJournalType)SelectedDocument.DocJournal.IdDocType, _authInHonestMark);
                sendModel.SetButtonsEnabled(true);

                if (SelectedDocuments.Exists(s => s.IsMarked))
                {
                    sendModel.IsReceivedMarkData = SelectedDocuments.All(s => 
                    (s.EdoProcessing as DocEdoProcessing)?.HonestMarkStatus == (int?)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Processed ||
                    (s.EdoProcessing as DocEdoProcessing)?.HonestMarkStatus == (int?)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Sent);

                    if (!sendModel.IsReceivedMarkData)
                        sendModel.BeforeSendEventHandler += (object s, EventArgs e) => { SendComissionDocumentForHonestMark(s); };
                }

                sendModel.Organizations = SetCounteragents(selectedOrganizationBoxIdGuid);
                sendWindow.DataContext = sendModel;
                sendModel.Owner = sendWindow;

                if(SelectedDocument.DocJournal?.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
                {
                    var buyerInfos = docs.Select(d => d.Value?.Buyers?.FirstOrDefault()?.Item as Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationDetailsUtd970)
                    .Where(d => d != null);

                    var buyerInns = buyerInfos.Select(b => b.Inn).Distinct();
                    if (buyerInns.Count() > 1)
                    {
                        System.Windows.MessageBox.Show(
                            "Выбраны документы более чем для одного отправителя.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return;
                    }

                    var buyerInn = buyerInns.FirstOrDefault();
                    var buyerKpp = buyerInfos.Select(b => b.Kpp).FirstOrDefault();

                    if ((!string.IsNullOrEmpty(buyerInn)) && (!string.IsNullOrEmpty(buyerKpp)))
                        sendModel.SelectedOrganization = sendModel.Organizations?.FirstOrDefault(o => o.Inn == buyerInn && o.Kpp == buyerKpp);

                    if (sendModel.SelectedOrganization == null && !string.IsNullOrEmpty(buyerInn))
                        sendModel.SelectedOrganization = sendModel.Organizations?.FirstOrDefault(o => o.Inn == buyerInn);
                }

                if (_owner != null)
                    sendWindow.Owner = _owner;
                
                sendWindow.ShowDialog();

                if (sendModel.EdoProcessing != null && sendModel.EdoProcessing.Count > 0)
                {
                    foreach(var s in SelectedDocuments)
                        if(sendModel.EdoProcessing.Exists(d => d.IdDoc == s.CurrentDocJournalId))
                            s.EdoProcessing = sendModel.EdoProcessing.First(d => d.IdDoc == s.CurrentDocJournalId);

                    OnPropertyChanged("Documents");
                    OnPropertyChanged("SelectedDocuments");
                }
            }
            catch (System.Net.WebException webEx)
            {
                var errorWindow = new ErrorWindow(
                        "Произошла ошибка на удалённом сервере.",
                        new List<string>(
                            new string[]
                            {
                                    webEx.Message,
                                    webEx.StackTrace
                            }
                            ));

                errorWindow.ShowDialog();
                _log.Log("WebException: " + _log.GetRecursiveInnerException(webEx));
            }
            catch (Exception ex)
            {
                var errorWindow = new ErrorWindow(
                        "Произошла ошибка.",
                        new List<string>(
                            new string[]
                            {
                                    ex.Message,
                                    ex.StackTrace
                            }
                            ));

                errorWindow.ShowDialog();
                _log.Log("Exception: " + _log.GetRecursiveInnerException(ex));
            }
        }

        private void SendComissionDocumentForHonestMark(object model, bool isSetSuccessfulLoad = false)
        {
            _log.Log("SendComissionDocumentForHonestMark: отправка данных в Диадок для Честного знака");
            if (SelectedOrganization == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбрана организация.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedOrganization.Certificate == null)
            {
                System.Windows.MessageBox.Show(
                    "Не задан сертификат отправителя.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedDocument == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбран документ.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var documents = SelectedDocuments.Where(s =>
                    (s.EdoProcessing as DocEdoProcessing)?.HonestMarkStatus != (int?)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Processed &&
                    (s.EdoProcessing as DocEdoProcessing)?.HonestMarkStatus != (int?)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Sent);

            if (documents.Count() == 0)
                return;

            var idDocType = documents.First().DocJournal.IdDocType;

            try
            {
                bool isSentData = false;

                var labelsByDocuments = (from selectedDocument in documents
                              let lbs = (from label in _abt.DocGoodsDetailsLabels where label.IdDocSale == selectedDocument.CurrentDocJournalId select label)
                              select new KeyValuePair<UniversalTransferDocument, IEnumerable<DocGoodsDetailsLabels>>(selectedDocument, lbs)) 
                              ?? new List<KeyValuePair<UniversalTransferDocument, IEnumerable<DocGoodsDetailsLabels>>>();

                labelsByDocuments = labelsByDocuments.Where(l => l.Value.Count() != 0);

                if (labelsByDocuments.Count() == 0)
                    return;

                if (labelsByDocuments.Any(labelByDoc => 
                (labelByDoc.Key.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice &&
                (labelByDoc.Key?.DocJournal?.DocGoodsDetailsIs?.Exists(g => (labelByDoc.Value?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) < g.Quantity) ?? false)) ||
                (labelByDoc.Key.DocJournal.IdDocType != (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice &&
                (labelByDoc.Key?.DocJournal?.Details?.Exists(g => (labelByDoc.Value?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) < g.Quantity) ?? false))))
                {
                    isSentData = System.Windows.MessageBox.Show(
                        "Некоторые коды маркированных товаров отсутствуют.\nВозможно, в списке присутствуют немаркированные товары.\nВы хотите отправить коды в Честный знак?",
                        "Внимание",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes;
                }
                else if (labelsByDocuments.Any(labelByDoc =>
                    (labelByDoc.Key.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice &&
                    (labelByDoc.Key?.DocJournal?.DocGoodsDetailsIs?.Exists(g => (labelByDoc.Value?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) > g.Quantity) ?? false)) ||
                    (labelByDoc.Key.DocJournal.IdDocType != (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice &&
                    (labelByDoc.Key?.DocJournal?.Details?.Exists(g => (labelByDoc.Value?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) > g.Quantity) ?? false))))
                {
                    isSentData = System.Windows.MessageBox.Show(
                        "В данном документе есть избыток кодов маркировки.\nКодов маркировки больше количества товаров.\nВы хотите отправить коды в Честный знак?",
                        "Внимание",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes;
                }
                else
                    isSentData = true;

                if (!isSentData)
                    throw new Exception("Попытка отправки данных в Честный знак была отвергнута пользователем.");

                var loadContext = new LoadModel();
                var loadWindow = new LoadWindow();
                loadWindow.DataContext = loadContext;

                loadContext.Text = "Начало процесса";

                if (model as SendModel != null && ((SendModel)model)?.Owner != null)
                {
                    loadWindow.Owner = ((SendModel)model).Owner;
                }
                else if (model as System.Windows.Window != null)
                {
                    loadWindow.Owner = model as System.Windows.Window;
                }

                List<DocComissionEdoProcessing> docProcessings = new List<DocComissionEdoProcessing>();
                System.ComponentModel.BackgroundWorker worker = new System.ComponentModel.BackgroundWorker();

                worker.DoWork += (object sender, System.ComponentModel.DoWorkEventArgs e) =>
                {
                    string employee = _abt.SelectSingleValue("select const_value from ref_const where id = 1200");

                    var orgsInnKpp = Organizations.Select(o => new KeyValuePair<string, string>(o.Inn, o.Kpp)).Distinct().ToList();
                    orgsInnKpp.Add(new KeyValuePair<string, string>("2539108495", "253901001"));
                    orgsInnKpp.Add(new KeyValuePair<string, string>("2536090987", "253901001"));

                    foreach (var labelsByDocument in labelsByDocuments)
                    {
                        var doc = labelsByDocument.Key;
                        var labelsByDoc = labelsByDocument.Value;

                        if (labelsByDoc.Count() == 0)
                            continue;

                        var markedCodesInfo = EdiProcessingUnit.HonestMark.HonestMarkClient.GetInstance()
                                    .GetMarkCodesInfo(EdiProcessingUnit.HonestMark.ProductGroupsEnum.None, labelsByDoc.Select(l => l.DmLabel).ToArray())
                                    .Where(m => m?.CisInfo?.OwnerInn != SelectedOrganization.Inn);

                        if (!markedCodesInfo.Any())
                            continue;

                        if (markedCodesInfo.Any(m => !orgsInnKpp.Exists(r => r.Key == m.CisInfo.OwnerInn)))
                            throw new Exception("Среди кодов есть не принадлежащие нашей организации.");

                        var markedCodesInfoByOrganizations = markedCodesInfo.GroupBy(m => m.CisInfo.OwnerInn);

                        foreach(var markedCodesInfoByOrganization in markedCodesInfoByOrganizations)
                        {
                            var orgInnKpp = orgsInnKpp.First(r => r.Key == markedCodesInfoByOrganization.Key);
                            var org = GetMyKontragent(orgInnKpp.Key, orgInnKpp.Value);
                            var crypt = new Cryptography.WinApi.WinApiCryptWrapper(org.Certificate);

                            loadContext.Text = "Авторизация";
                            Edo.GetInstance().Authenticate(false, org.Certificate, org.Inn);
                            loadContext.Text = "Формирование приходного УПД";

                            var labels = labelsByDoc.Where(l => markedCodesInfoByOrganization.Any(m => m.CisInfo.Cis == l.DmLabel));
                            string documentNumber = doc.DocJournal?.Code;

                            var document = CreateShipmentDocument(doc.DocJournal, org, SelectedOrganization, labels.ToList(), documentNumber, employee, true);

                            var generatedFile = Edo.GetInstance().GenerateTitleXml("UniversalTransferDocument", "ДОП", "utd970_05_03_01", 0, document);
                            byte[] signature = crypt.Sign(generatedFile.Content, true);

                            var signedContent = new Diadoc.Api.Proto.Events.SignedContent
                            {
                                Content = generatedFile.Content
                            };

                            if (signature != null)
                                signedContent.Signature = signature;

                            loadContext.Text = "Отправка";

                            Diadoc.Api.Proto.Events.PowerOfAttorneyToPost orgPowerOfAttorneyToPost = null;

                            if (!string.IsNullOrEmpty(org.EmchdId))
                                orgPowerOfAttorneyToPost = new Diadoc.Api.Proto.Events.PowerOfAttorneyToPost
                                {
                                    UseDefault = false,
                                    FullId = new Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyFullId
                                    {
                                        RegistrationNumber = org.EmchdId,
                                        IssuerInn = org.Inn,
                                        RepresentativeInn = org.EmchdPersonInn
                                    }
                                };

                            var message = Edo.GetInstance().SendXmlDocument(org.OrgId, SelectedOrganization.OrgId, false, new List<Diadoc.Api.Proto.Events.SignedContent>(new[] { signedContent }), "ДОП", orgPowerOfAttorneyToPost);

                            loadContext.Text = "Обработка приходного УПД";
                            Edo.GetInstance().Authenticate(false, SelectedOrganization.Certificate, SelectedOrganization.Inn);

                            var buyerDocument = CreateBuyerShipmentDocument(SelectedOrganization);
                            crypt.InitializeCertificate(SelectedOrganization.Certificate);

                            var attachments = message.Entities.Where(t => t.AttachmentType == Diadoc.Api.Proto.Events.AttachmentType.UniversalTransferDocument).Select(ent =>
                            {
                                var generatedBuyerFile = Edo.GetInstance().GenerateTitleXml("UniversalTransferDocument", "ДОП", "utd970_05_03_01", 1, buyerDocument, message.MessageId, ent.EntityId);
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

                            Diadoc.Api.Proto.Events.PowerOfAttorneyToPost selectedOrganizationPowerOfAttorneyToPost = null;

                            if (!string.IsNullOrEmpty(SelectedOrganization.EmchdId))
                                selectedOrganizationPowerOfAttorneyToPost = new Diadoc.Api.Proto.Events.PowerOfAttorneyToPost
                                {
                                    UseDefault = false,
                                    FullId = new Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyFullId
                                    {
                                        RegistrationNumber = SelectedOrganization.EmchdId,
                                        IssuerInn = SelectedOrganization.Inn,
                                        RepresentativeInn = SelectedOrganization.EmchdPersonInn
                                    }
                                };

                            Edo.GetInstance().SendPatchRecipientXmlDocument(message.MessageId, (int)Diadoc.Api.Proto.DocumentType.UniversalTransferDocument,
                                    attachments, selectedOrganizationPowerOfAttorneyToPost);

                            loadContext.Text = "Сохранение в базе данных.";

                            var entity = message.Entities.FirstOrDefault(t => t.AttachmentType == Diadoc.Api.Proto.Events.AttachmentType.UniversalTransferDocument &&
                                t?.DocumentInfo?.DocumentNumber == doc.DocJournal.Code);

                            var fileNameLength = entity.DocumentInfo.FileName.LastIndexOf('.');

                            if (fileNameLength < 0)
                                fileNameLength = entity.DocumentInfo.FileName.Length;

                            var docComissionProcessing = new DocComissionEdoProcessing
                            {
                                Id = Guid.NewGuid().ToString(),
                                MessageId = message.MessageId,
                                EntityId = entity.EntityId,
                                IdDoc = doc.CurrentDocJournalId,
                                SenderInn = org.Inn,
                                ReceiverInn = SelectedOrganization.Inn,
                                DocStatus = (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Sent,
                                UserName = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser,
                                FileName = entity.DocumentInfo.FileName.Substring(0, fileNameLength),
                                DocDate = DateTime.Now,
                                DeliveryDate = doc?.DocJournal?.DeliveryDate
                            };

                            if (!doc.IsMarked)
                                docComissionProcessing.DocStatus = (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Processed;

                            _abt.DocComissionEdoProcessings.Add(docComissionProcessing);
                            docProcessings.Add(docComissionProcessing);
                        }
                    }
                };

                worker.RunWorkerCompleted += (object sender, System.ComponentModel.RunWorkerCompletedEventArgs e) => 
                {
                    if (e.Error != null)
                    {
                        loadWindow.Close();
                        throw e.Error;
                    }

                    if (model as SendModel != null && docProcessings.Count > 0)
                        ((SendModel)model).ComissionDocuments.AddRange(docProcessings);

                    _abt.SaveChanges();

                    if (isSetSuccessfulLoad)
                        loadWindow.SetSuccessFullLoad(loadContext, "Данные успешно отправлены.");
                    else
                        loadWindow.Close();
                };

                worker.RunWorkerAsync();

                loadWindow.ShowDialog();
            }
            catch (System.Net.WebException webEx)
            {
                _log.Log("Отправка завершена с ошибкой. WebException: " + _log.GetRecursiveInnerException(webEx));
                throw webEx;
            }
            catch (Exception ex)
            {
                _log.Log("Отправка завершена с ошибкой. Exception: " + _log.GetRecursiveInnerException(ex));
                throw ex;
            }

            _log.Log("SendComissionDocumentForHonestMark: завершено успешно");
        }

        private void ShowMarkedCodes()
        {
            if (SelectedDocuments.Count > 1)
            {
                System.Windows.MessageBox.Show(
                    "Для просмотра списка кодов маркировки нужно выбрать только один документ.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var markedCodesWindow = new MarkedCodesWindow(PermissionReturnMarkedCodes);

            decimal idDoc = SelectedDocument.CurrentDocJournalId.Value;

            var markedCodes = _abt.DocGoodsDetailsLabels?
                .Where(l => l.IdDocSale == idDoc)?
                .ToList();

            if(SelectedDocument.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
                markedCodesWindow.SetMarkedItems(_abt, SelectedDocument.DocJournal.DocGoodsDetailsIs, markedCodes);
            else
                markedCodesWindow.SetMarkedItems(_abt, SelectedDocument.DocJournal.Details, markedCodes);

            markedCodesWindow.OnReturnCodesToStore += ReturnMarkedCodesToStore;

            markedCodesWindow.Show();
        }

        private void ReprocessDocument()
        {
            if (SelectedOrganization == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбрана организация.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (!WorkWithDocumentsPermission)
            {
                System.Windows.MessageBox.Show(
                    "Нет прав на работу с документами.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedDocument == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбран документ.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedDocuments.Count > 1)
            {
                System.Windows.MessageBox.Show(
                    "Для повторной обработки нужно выбрать только один документ.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (_abt.DocEdoProcessings.Any(d => d.IdDoc == SelectedDocument.CurrentDocJournalId && d.HonestMarkStatus == (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Processed))
            {
                System.Windows.MessageBox.Show(
                    "Данный документ уже был успешно обработан.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (_abt.DocEdoProcessings.Any(d => d.IdDoc == SelectedDocument.CurrentDocJournalId && d.HonestMarkStatus == (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Sent))
            {
                System.Windows.MessageBox.Show(
                    "Данный документ находится ещё пока на стадии обработки.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var processingStatus = SelectedDocument?.EdoProcessing as DocEdoProcessing;

            if(processingStatus == null)
            {
                System.Windows.MessageBox.Show(
                   "Не найден документ. Возможно, он не был ранее отправлен.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (processingStatus.DocStatus != (int)EdiProcessingUnit.Enums.DocEdoSendStatus.Signed)
            {
                System.Windows.MessageBox.Show(
                   "Документ не был ранее подписан контрагентом.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                _authInHonestMark = EdiProcessingUnit.HonestMark.HonestMarkClient.GetInstance().Authorization(SelectedOrganization.Certificate, SelectedOrganization);

                if (!_authInHonestMark)
                {
                    System.Windows.MessageBox.Show(
                        "Повторная обработка невозможна, так как авторизация в Честном знаке не была успешной.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                EdiProcessingUnit.HonestMark.HonestMarkClient.GetInstance().ReprocessDocument(processingStatus.FileName);

                processingStatus.HonestMarkStatus = (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Sent;

                _abt.SaveChanges();
            }
            catch (System.Net.WebException webEx)
            {
                var errorWindow = new ErrorWindow(
                       "Произошла ошибка повторной обработки документа на удалённом сервере.",
                       new List<string>(
                           new string[]
                           {
                                    webEx.Message,
                                    webEx.StackTrace
                           }
                           ));

                errorWindow.ShowDialog();
                _log.Log("Повторная обработка. WebException: " + _log.GetRecursiveInnerException(webEx));
            }
            catch (Exception ex)
            {
                var errorWindow = new ErrorWindow(
                       "Произошла ошибка повторной обработки документа.",
                       new List<string>(
                           new string[]
                           {
                                    ex.Message,
                                    ex.StackTrace
                           }
                           ));

                errorWindow.ShowDialog();
                _log.Log("Повторная обработка. Exception: " + _log.GetRecursiveInnerException(ex));
            }
        }

        private void AnnulmentDocument(DocEdoProcessing docProcessing = null)
        {
            if (SelectedDocument == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбран документ.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if(SelectedDocuments.Count > 1)
            {
                System.Windows.MessageBox.Show(
                    "Для аннулирования нужно выбрать один документ.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedOrganization?.Certificate == null)
            {
                System.Windows.MessageBox.Show(
                    "Не определён сертификат пользователя.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                bool result = Edo.GetInstance().Authenticate(false, SelectedOrganization.Certificate, SelectedOrganization.Inn);

                if (!result)
                    throw new Exception("Не удалось авторизоваться в системе по сертификату.");

                if (SelectedDocument.EdoProcessing == null)
                    throw new Exception("Документ не найден в базе. Возможно, он не был ранее отправлен.");

                Diadoc.Api.Proto.Documents.Document document = null;

                if (docProcessing == null)
                {
                    if (SelectedDocument?.EdoProcessing as DocEdoProcessing != null)
                    {
                        if (SelectedDocument.IsMarked)
                        {
                            var docProcessings = _abt.DocEdoProcessings.Where(d => d.IdDoc == SelectedDocument.CurrentDocJournalId);
                            foreach (var docProc in docProcessings)
                            {
                                if (docProcessing != null)
                                    break;

                                if (docProc.HonestMarkStatus == (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Processed)
                                {
                                    var doc = Edo.GetInstance().GetDocument(docProc.MessageId, docProc.EntityId);
                                    docProcessing = docProc;
                                    SelectedDocument.EdoProcessing = docProcessing;
                                    document = doc;
                                }
                            }
                        }

                        docProcessing = SelectedDocument.EdoProcessing as DocEdoProcessing;
                    }
                }

                if(document == null)
                    document = Edo.GetInstance().GetDocument(docProcessing.MessageId, docProcessing.EntityId);

                if (document == null)
                    throw new Exception("Документ не найден в системе ЭДО.");

                Diadoc.Api.Proto.Events.PowerOfAttorneyToPost powerOfAttorneyToPost = null;

                if (!string.IsNullOrEmpty(SelectedOrganization.EmchdId))
                    powerOfAttorneyToPost = new Diadoc.Api.Proto.Events.PowerOfAttorneyToPost
                    {
                        UseDefault = false,
                        FullId = new Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyFullId
                        {
                            RegistrationNumber = SelectedOrganization.EmchdId,
                            IssuerInn = SelectedOrganization.Inn,
                            RepresentativeInn = SelectedOrganization.EmchdPersonInn
                        }
                    };

                if (document.RevocationStatus == Diadoc.Api.Proto.Documents.RevocationStatus.RequestsMyRevocation)
                {
                    var message = Edo.GetInstance().GetMessage(docProcessing.MessageId, docProcessing.EntityId, true);
                    var crypt = new Cryptography.WinApi.WinApiCryptWrapper(SelectedOrganization.Certificate);

                    var entity = message?.Entities?.FirstOrDefault(c => c.AttachmentType == Diadoc.Api.Proto.Events.AttachmentType.RevocationRequest && c.RevocationRequestInfo?.InitiatorBoxId == message.ToBoxId);

                    if (entity == null)
                        throw new Exception("Не найден документ - Запрос на аннулирование.");

                    var buyerSignature = message?.Entities?
                        .FirstOrDefault(s => s.ParentEntityId == entity.EntityId && s.EntityType == Diadoc.Api.Proto.Events.EntityType.Signature)?.Content?.Data;

                    //Либо согласие на аннулирование с нашей стороны, либо отказ
                    var confirmAnnulmentWindow = new ConfirmAnnulmentWindow();

                    confirmAnnulmentWindow.ShowDialog();

                    if(confirmAnnulmentWindow.Result == AnnulmentRequestDialogResult.Revoke)
                    {
                        var signature = crypt.Sign(entity.Content.Data, true);
                        Edo.GetInstance().SendPatchSignedDocument(message.MessageId, entity.EntityId, signature, powerOfAttorneyToPost);

                        var fileNameLength = entity.FileName.LastIndexOf('.');

                        if (fileNameLength < 0)
                            fileNameLength = entity.FileName.Length;

                        var fileName = entity.FileName.Substring(0, fileNameLength);

                        docProcessing.AnnulmentFileName = fileName;

                        if(document.RecipientResponseStatus == Diadoc.Api.Proto.Documents.RecipientResponseStatus.WithRecipientSignature && SelectedDocument.IsMarked)
                            docProcessing.AnnulmentStatus = (int)EdiProcessingUnit.HonestMark.AnnulmentDocumentStatus.RevokedWaitProcessing;
                        else
                            docProcessing.AnnulmentStatus = (int)EdiProcessingUnit.HonestMark.AnnulmentDocumentStatus.Revoked;

                        _abt.SaveChanges();

                        var loadContext = new LoadModel();
                        var loadWindow = new LoadWindow();
                        loadWindow.DataContext = loadContext;

                        OnPropertyChanged("Documents");
                        OnPropertyChanged("SelectedDocument");

                        loadWindow.SetSuccessFullLoad(loadContext, "Документ аннулирован.");
                        loadWindow.ShowDialog();
                    }
                    else if(confirmAnnulmentWindow.Result == AnnulmentRequestDialogResult.Reject)
                    {
                        var reasonText = confirmAnnulmentWindow.RejectReasonText;

                        var loadContext = new LoadModel();
                        var loadWindow = new LoadWindow();
                        loadWindow.DataContext = loadContext;

                        if (_owner != null)
                            loadWindow.Owner = _owner;

                        System.ComponentModel.BackgroundWorker worker = new System.ComponentModel.BackgroundWorker();

                        worker.DoWork += (object sender, System.ComponentModel.DoWorkEventArgs e) =>
                        {
                            loadContext.Text = "Формирование файла отказа.";

                            Diadoc.Api.DataXml.TechnologicalSigner133UserContract.TechnologicalDocumentSigner133 signer;

                            if (string.IsNullOrEmpty(SelectedOrganization.EmchdId))
                            {
                                signer = new Diadoc.Api.DataXml.TechnologicalSigner133UserContract.TechnologicalDocumentSigner133
                                {
                                    Certificate = new Diadoc.Api.DataXml.TechnologicalSigner133UserContract.Certificate
                                    {
                                        CertificateBytes = SelectedOrganization.Certificate.RawData
                                    },
                                    Position = new Diadoc.Api.DataXml.TechnologicalSigner133UserContract.TechnologicalDocumentSigner133Position
                                    {
                                        PositionSource = Diadoc.Api.DataXml.TechnologicalSigner133UserContract.TechnologicalDocumentSigner133PositionPositionSource.Manual,
                                        Value = _utils.ParseCertAttribute(SelectedOrganization.Certificate.Subject, "T")
                                    },
                                    SignerStatusSpecified = true,
                                    SignerStatus = Diadoc.Api.DataXml.TechnologicalSigner133UserContract.TechnologicalDocumentSigner133SignerStatus.Item1
                                };
                            }
                            else
                            {
                                signer = new Diadoc.Api.DataXml.TechnologicalSigner133UserContract.TechnologicalDocumentSigner133
                                {
                                    Certificate = new Diadoc.Api.DataXml.TechnologicalSigner133UserContract.Certificate
                                    {
                                        CertificateBytes = SelectedOrganization.Certificate.RawData
                                    },
                                    Position = new Diadoc.Api.DataXml.TechnologicalSigner133UserContract.TechnologicalDocumentSigner133Position
                                    {
                                        PositionSource = Diadoc.Api.DataXml.TechnologicalSigner133UserContract.TechnologicalDocumentSigner133PositionPositionSource.Manual,
                                        Value = SelectedOrganization.EmchdPersonPosition
                                    },
                                    SignerStatusSpecified = true,
                                    SignerStatus = Diadoc.Api.DataXml.TechnologicalSigner133UserContract.TechnologicalDocumentSigner133SignerStatus.Item2,
                                    PowerOfAttorney = new Diadoc.Api.DataXml.TechnologicalSigner133UserContract.PowerOfAttorney
                                    {
                                        Item = new Diadoc.Api.DataXml.TechnologicalSigner133UserContract.Electronic
                                        {
                                            MethodOfProviding = Diadoc.Api.DataXml.TechnologicalSigner133UserContract.ElectronicMethodOfProviding.Item2,
                                            Item = new Diadoc.Api.DataXml.TechnologicalSigner133UserContract.Storage
                                            {
                                                UseDefault = Diadoc.Api.DataXml.TechnologicalSigner133UserContract.StorageUseDefault.@false,
                                                FullId = new Diadoc.Api.DataXml.TechnologicalSigner133UserContract.StorageFullId
                                                {
                                                    RegistrationNumber = SelectedOrganization.EmchdId,
                                                    IssuerInn = SelectedOrganization.Inn
                                                }
                                            }
                                        }
                                    }
                                };
                            }

                            var generatedFile = Edo.GetInstance().GenerateSignatureRejectionXml(message.MessageId, entity.EntityId, reasonText, signer);

                            loadContext.Text = "Подписание и отправка.";
                            var signature = crypt.Sign(generatedFile.Content, true);
                            Edo.GetInstance().SendRejectionDocument(message.MessageId, entity.EntityId, generatedFile.Content, signature, powerOfAttorneyToPost);

                            docProcessing.AnnulmentStatus = (int)EdiProcessingUnit.HonestMark.AnnulmentDocumentStatus.Rejected;
                            _abt.SaveChanges();
                        };

                        worker.RunWorkerCompleted += (object sender, System.ComponentModel.RunWorkerCompletedEventArgs e) =>
                        {
                            if (e.Error != null)
                            {
                                loadWindow.Close();
                                throw e.Error;
                            }

                            loadWindow.SetSuccessFullLoad(loadContext, "Запрос успешно отклонён.");
                        };

                        worker.RunWorkerAsync();
                        loadWindow.ShowDialog();
                    }
                }
                else if(document.RevocationStatus == Diadoc.Api.Proto.Documents.RevocationStatus.RevocationIsRequestedByMe)
                {
                    if (docProcessing.AnnulmentStatus != (int)EdiProcessingUnit.HonestMark.AnnulmentDocumentStatus.Requested)
                    {
                        docProcessing.AnnulmentStatus = (int)EdiProcessingUnit.HonestMark.AnnulmentDocumentStatus.Requested;
                        _abt.SaveChanges();
                    }

                    throw new Exception("Аннулирование уже было запрошено с нашей стороны.");
                }
                else if (document.RevocationStatus == Diadoc.Api.Proto.Documents.RevocationStatus.RevocationAccepted)
                {
                    throw new Exception("Документ аннулирован.");
                }
                else if (document.RevocationStatus == Diadoc.Api.Proto.Documents.RevocationStatus.RevocationRejected)
                {
                    throw new Exception("Был получен или отправлен отказ от аннулирования.");
                }
                else
                {
                    var annulmentWindow = new AnnulmentWindow();

                    if (annulmentWindow.ShowDialog() == true)
                    {
                        //Отправка запроса на аннуляцию
                        var text = annulmentWindow.Text;
                        var crypt = new Cryptography.WinApi.WinApiCryptWrapper(SelectedOrganization.Certificate);

                        var loadContext = new LoadModel();
                        var loadWindow = new LoadWindow();
                        loadWindow.DataContext = loadContext;

                        if (_owner != null)
                            loadWindow.Owner = _owner;

                        System.ComponentModel.BackgroundWorker worker = new System.ComponentModel.BackgroundWorker();

                        worker.DoWork += (object sender, System.ComponentModel.DoWorkEventArgs e) =>
                        {
                            loadContext.Text = "Формирование файла запроса.";

                            Diadoc.Api.Proto.Invoicing.Signer signer;

                            if (string.IsNullOrEmpty(SelectedOrganization.EmchdId))
                            {
                                var firstMiddleName = _utils.ParseCertAttribute(SelectedOrganization.Certificate.Subject, "G");
                                string signerFirstName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
                                string signerPatronymic = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;

                                signer = new Diadoc.Api.Proto.Invoicing.Signer
                                {
                                    SignerCertificate = SelectedOrganization.Certificate.RawData,
                                    SignerCertificateThumbprint = SelectedOrganization.Certificate.Thumbprint,
                                    SignerDetails = new Diadoc.Api.Proto.Invoicing.SignerDetails()
                                    {
                                        Surname = _utils.ParseCertAttribute(SelectedOrganization.Certificate.Subject, "SN"),
                                        FirstName = signerFirstName,
                                        Patronymic = signerPatronymic,
                                        Inn = _utils.ParseCertAttribute(SelectedOrganization.Certificate.Subject, "ИНН").TrimStart('0'),
                                        JobTitle = _utils.ParseCertAttribute(SelectedOrganization.Certificate.Subject, "T")
                                    }
                                };
                            }
                            else
                            {
                                signer = new Diadoc.Api.Proto.Invoicing.Signer
                                {
                                    SignerCertificate = SelectedOrganization.Certificate.RawData,
                                    SignerCertificateThumbprint = SelectedOrganization.Certificate.Thumbprint,
                                    SignerDetails = new Diadoc.Api.Proto.Invoicing.SignerDetails()
                                    {
                                        Surname = SelectedOrganization.EmchdPersonSurname,
                                        FirstName = SelectedOrganization.EmchdPersonName,
                                        Patronymic = SelectedOrganization.EmchdPersonPatronymicSurname,
                                        Inn = SelectedOrganization.EmchdPersonInn,
                                        JobTitle = SelectedOrganization.EmchdPersonPosition
                                    }
                                };
                            }

                            var generatedFile = Edo.GetInstance().GenerateRevocationRequestXml(docProcessing.MessageId, docProcessing.EntityId, text, signer);

                            if (generatedFile == null)
                                throw new Exception("Файл не был сгенерирован.");

                            loadContext.Text = "Подписание и отправка.";
                            var signature = crypt.Sign(generatedFile.Content, true);
                            Edo.GetInstance().SendRevocationDocument(docProcessing.MessageId, docProcessing.EntityId, generatedFile.Content, signature, powerOfAttorneyToPost);

                            loadContext.Text = "Сохранение в базе.";
                            var fileNameLength = generatedFile.FileName.LastIndexOf('.');

                            if (fileNameLength < 0)
                                fileNameLength = generatedFile.FileName.Length;

                            var fileName = generatedFile.FileName.Substring(0, fileNameLength);

                            docProcessing.AnnulmentFileName = fileName;
                            docProcessing.AnnulmentStatus = (int)EdiProcessingUnit.HonestMark.AnnulmentDocumentStatus.Requested;
                            _abt.SaveChanges();
                        };

                        worker.RunWorkerCompleted += (object sender, System.ComponentModel.RunWorkerCompletedEventArgs e) =>
                        {
                            if (e.Error != null)
                            {
                                loadWindow.Close();
                                throw e.Error;
                            }

                            loadWindow.SetSuccessFullLoad(loadContext, "Запрос успешно отправлен.");
                        };

                        worker.RunWorkerAsync();
                        loadWindow.ShowDialog();
                    }
                }
            }
            catch (System.Net.WebException webEx)
            {
                var errorWindow = new ErrorWindow(
                       "Произошла ошибка аннулирования документа на удалённом сервере.",
                       new List<string>(
                           new string[]
                           {
                                    webEx.Message,
                                    webEx.StackTrace
                           }
                           ));

                errorWindow.ShowDialog();
                _log.Log("Аннулирование. WebException: " + _log.GetRecursiveInnerException(webEx));
            }
            catch (Exception ex)
            {
                var errorWindow = new ErrorWindow(
                       "Произошла ошибка аннулирования документа.",
                       new List<string>(
                           new string[]
                           {
                                    ex.Message,
                                    ex.StackTrace
                           }
                           ));

                errorWindow.ShowDialog();
                _log.Log("Аннулирование. Exception: " + _log.GetRecursiveInnerException(ex));
            }
        }

        private void GoodsMatching()
        {
            if (!PermissionCompareGoods)
            {
                System.Windows.MessageBox.Show(
                    "У пользователя нет прав на сопоставление товаров.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var goodsMatchingModel = new GoodsMatchingModel(_abt, _config, Documents ?? new List<UniversalTransferDocument>());
            goodsMatchingModel.PermissionChannelsList = this.PermissionChannelsList;
            goodsMatchingModel.PermissionChannelsSettings = this.PermissionChannelsSettings;
            goodsMatchingModel.Filials = this.Filials;
            goodsMatchingModel.SaveAction = () => { this.OnPropertyChanged("DocumentDetails"); };

            var goodsMatchingWindow = new GoodsMatchingWindow();
            goodsMatchingWindow.DataContext = goodsMatchingModel;
            goodsMatchingWindow.Show();
        }

        private async void ReturnMarkedCodesToStore(object sender, EventArgs e)
        {
            if (SelectedDocument == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбран документ.", 
                    "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedDocument.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && SelectedDocument?.EdoProcessing == null)
            {
                System.Windows.MessageBox.Show(
                    "Документ с данными кодами маркировки не был ранее отправлен.\n" +
                    "Поэтому по ним невозможно осуществить возврат.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var docProcessing = SelectedDocument?.EdoProcessing as DocEdoProcessing;

            if(docProcessing != null)
            {
                if (docProcessing.AnnulmentStatus == (int)EdiProcessingUnit.HonestMark.AnnulmentDocumentStatus.RevokedWaitProcessing || docProcessing.AnnulmentStatus == (int)EdiProcessingUnit.HonestMark.AnnulmentDocumentStatus.Requested)
                {
                    System.Windows.MessageBox.Show(
                        "Документ с данными кодами маркировки находится в процессе аннулирования.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                if ((docProcessing.DocStatus == (int)EdiProcessingUnit.Enums.DocEdoSendStatus.Signed || docProcessing.DocStatus == (int)EdiProcessingUnit.Enums.DocEdoSendStatus.PartialSigned) && docProcessing.HonestMarkStatus == (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Sent)
                {
                    System.Windows.MessageBox.Show(
                        "Документ с данными кодами маркировки обрабатывается в Честном знаке.\n" +
                        "Дождитесь конца обработки и повторите попытку.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                if (docProcessing.HonestMarkStatus == (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.ProcessingError)
                {
                    System.Windows.MessageBox.Show(
                        "Документ с данными кодами маркировки был обработан с ошибками в Честном знаке.\n" +
                        "Поэтому по ним невозможно осуществить возврат.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
            }

            var labels = ((MarkedCodesWindow)sender).SelectedCodes;

            if(labels.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "Не выбраны коды маркировки.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (_authInHonestMark)
            {
                var markedCodesInfo = EdiProcessingUnit.HonestMark.HonestMarkClient.GetInstance()
                    .GetMarkCodesInfo(EdiProcessingUnit.HonestMark.ProductGroupsEnum.None, labels.Select(l => l.DmLabel).ToArray());

                if (SelectedDocument.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && 
                    markedCodesInfo.Any(m => m?.CisInfo?.OwnerInn != SelectedOrganization.Inn))
                {
                    System.Windows.MessageBox.Show(
                        "В списке кодов маркировки есть те, которые не принадлежат нашей организации.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                else if(SelectedDocument.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Translocation)
                {
                    var orgsInnKpp = Organizations.Select(o => new KeyValuePair<string, string>(o.Inn, o.Kpp)).Distinct().ToList();
                    orgsInnKpp.Add(new KeyValuePair<string, string>("2539108495", "253901001"));
                    orgsInnKpp.Add(new KeyValuePair<string, string>("2536090987", "253901001"));

                    if(markedCodesInfo.Any(m => !orgsInnKpp.Exists(r => r.Key == m.CisInfo?.OwnerInn)))
                    {
                        System.Windows.MessageBox.Show(
                        "В списке кодов маркировки есть те, которые не принадлежат одной из наших организаций.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return;
                    }
                }
            }
            else
            {
                if (System.Windows.MessageBox.Show(
                    "Авторизация в Честном знаке не была успешной.\nПоэтому невозможно проверить коды на владельцев в Честном знаке.\nВы хотите отправить обратно коды?", "Ошибка", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                    return;
            }

            Exception exception = null;
            var errorsList = new List<string>();
            var loadWindow = new LoadWindow();
            loadWindow.Show();

            exception = null;

            var loadContext = new LoadModel();
            loadWindow.DataContext = loadContext;
            loadWindow.Owner = (MarkedCodesWindow)sender;

            await Task.Run(() =>
            {
                try
                {
                    loadContext.Text = "Сохранение в базе";
                    foreach (var label in labels)
                    {
                        label.IdDocSale = null;
                        label.SaleDmLabel = null;
                        label.SaleDateTime = null;
                    }

                    _abt.SaveChanges();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            if (exception == null)
            {
                foreach (var label in labels)
                    ((MarkedCodesWindow)sender).MarkedCodes.Remove(label);

                if(SelectedDocument.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
                    ((MarkedCodesWindow)sender).SetMarkedItems(_abt, SelectedDocument.DocJournal.DocGoodsDetailsIs, ((MarkedCodesWindow)sender).MarkedCodes);
                else
                    ((MarkedCodesWindow)sender).SetMarkedItems(_abt, SelectedDocument.DocJournal.Details, ((MarkedCodesWindow)sender).MarkedCodes);
            }
            else
            {
                errorsList.Add($"Ошибка возврата\r\n{_log.GetRecursiveInnerException(exception)}");
                _log.Log("Exception: " + _log.GetRecursiveInnerException(exception));
            }

            if (errorsList.Count == 0)
                loadWindow.SetSuccessFullLoad(loadWindow.DataContext as LoadModel, "Возврат завершён успешно.");
            else
            {
                loadWindow.Close();
                var errorWindow = new ErrorWindow("Произошла ошибка при возврате кодов.", errorsList);

                errorWindow.ShowDialog();
                _log.Log("Exception: ReturnMarkedCodesToStore");
            }
        }

        private Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument GetUniversalDocument(
            DocJournal d, Kontragent organization, string employee = null, RefEdoGoodChannel edoGoodChannel = null)
        {
            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && d.DocMaster == null)
                return null;

            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && d.DocGoodsI == null)
                return null;

            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Translocation && d.DocGoods == null)
                return null;

            var document = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument()
            {
                Function = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocumentFunction.СЧФДОП,
                DocumentName = "Универсальный передаточный документ",
                DocumentNumber = d.Code,
                DocumentDate = d.DeliveryDate?.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy"),
                Currency = Properties.Settings.Default.DefaultCurrency,
                Table = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTable
                {
                    TotalSpecified = true,
                    TotalWithVatExcludedSpecified = true
                },
                TransferInfo = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TransferInfo
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
                document.Table.WithoutVat = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableWithoutVat.@true;
            }

            if (!string.IsNullOrEmpty(employee))
            {
                document.TransferInfo.Employee = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.EmployeeUtd970
                {
                    Position = Properties.Settings.Default.DefaultEmployePosition,
                    EmployeeInfo = employee,
                    LastName = employee.Substring(0, employee.IndexOf(' ')),
                    FirstName = employee.Substring(employee.IndexOf(' ') + 1)
                };
            }


            Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.RussianAddressUtd970 senderAddress = organization?.Address?.RussianAddress != null ?
                            new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.RussianAddressUtd970
                            {
                                ZipCode = organization.Address.RussianAddress.ZipCode,
                                Region = organization.Address.RussianAddress.Region,
                                Street = string.IsNullOrEmpty(organization?.Address?.RussianAddress?.Street) ? null : organization.Address.RussianAddress.Street,
                                City = string.IsNullOrEmpty(organization?.Address?.RussianAddress?.City) ? null : organization.Address.RussianAddress.City,
                                Locality = string.IsNullOrEmpty(organization?.Address?.RussianAddress?.Locality) ? null : organization.Address.RussianAddress.Locality,
                                Territory = string.IsNullOrEmpty(organization?.Address?.RussianAddress?.Territory) ? null : organization.Address.RussianAddress.Territory,
                                Building = string.IsNullOrEmpty(organization?.Address?.RussianAddress?.Building) ? null : organization.Address.RussianAddress.Building
                            } : null;

            document.Sellers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970[]
            {
                        new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970
                        {
                            Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationDetailsUtd970
                            {
                                Inn = organization.Inn,
                                Kpp = organization.Kpp,
                                OrgType = organization.Inn.Length == 12 ? Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item1 : Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item2,
                                OrgName = organization.Name,
                                Address = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AddressUtd970
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
                    document.Shippers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocumentShipper[]
                    {
                            new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocumentShipper
                            {
                                Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationDetailsUtd970
                                {
                                    Inn = organization.Inn,
                                    Kpp = organization.Kpp,
                                    OrgType = organization.Inn.Length == 12 ? Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item1 : Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item2,
                                    Address = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AddressUtd970
                                    {
                                        Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ForeignAddressUtd970
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
                            document.Consignees = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970[]
                        {
                                        new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970
                                        {
                                            Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationDetailsUtd970
                                            {
                                                Inn = buyerCustomer.Inn,
                                                Kpp = buyerCustomer.Kpp,
                                                OrgType = buyerCustomer.Inn.Length == 12 ? Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item1 : Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item2,
                                                Address = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AddressUtd970
                                                {
                                                    Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ForeignAddressUtd970
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

                        document.Buyers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970[]
                        {
                                    new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970
                                    {
                                        Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationDetailsUtd970
                                        {
                                            Inn = buyerCustomer.Inn,
                                            Kpp = buyerCustomer.Kpp,
                                            OrgType = buyerCustomer.Inn.Length == 12 ? Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item1 : Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item2,
                                            OrgName = buyerCustomer.Name,
                                            Address = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AddressUtd970
                                            {
                                                Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ForeignAddressUtd970
                                                {
                                                    Country = Properties.Settings.Default.DefaultOrgCountryCode,
                                                    Address = buyerCustomer.Address
                                                }
                                            }
                                        }
                                    }
                        };

                        var contractNumber = _abt.RefRefTags.FirstOrDefault(c => c.IdTag == 200 && c.IdObject == buyerCustomer.Id)?.TagValue;
                        var contractDate = _abt.RefRefTags.FirstOrDefault(c => c.IdTag == 199 && c.IdObject == buyerCustomer.Id)?.TagValue;

                        if (!(string.IsNullOrEmpty(contractNumber) || string.IsNullOrEmpty(contractDate)))
                        {
                            document.TransferInfo.TransferBases = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.DocumentRequisitesType[]
                            {
                                new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.DocumentRequisitesType
                                {
                                    DocumentName = "Договор поставки",
                                    DocumentNumber = contractNumber,
                                    DocumentDate = contractDate
                                }
                            };
                        }
                    }
                }
            }

            Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signer signer;

            if (string.IsNullOrEmpty(organization.EmchdId))
            {
                var firstMiddleName = _utils.ParseCertAttribute(organization.Certificate.Subject, "G");
                string signerFirstName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
                string signerMiddleName = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;

                signer = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signer
                {
                    Fio = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Fio
                    {
                        FirstName = signerFirstName,
                        MiddleName = signerMiddleName,
                        LastName = _utils.ParseCertAttribute(organization.Certificate.Subject, "SN")
                    },
                    Position = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerPosition
                    {
                        PositionSource = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerPositionPositionSource.Manual,
                        Value = _utils.ParseCertAttribute(organization.Certificate.Subject, "T")
                    },
                    SignatureTypeSpecified = true,
                    SignatureType = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerSignatureType.Item1,
                    SigningDate = DateTime.Now.ToString("dd.MM.yyyy"),
                    SignerPowersConfirmationMethodSpecified = true,
                    SignerPowersConfirmationMethod = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerSignerPowersConfirmationMethod.Item1
                };
            }
            else
            {
                signer = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signer
                {
                    Fio = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Fio
                    {
                        FirstName = organization.EmchdPersonName,
                        MiddleName = organization.EmchdPersonPatronymicSurname,
                        LastName = organization.EmchdPersonSurname
                    },
                    Position = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerPosition
                    {
                        PositionSource = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerPositionPositionSource.Manual,
                        Value = organization.EmchdPersonPosition
                    },
                    SignatureTypeSpecified = true,
                    SignatureType = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerSignatureType.Item1,
                    SigningDate = DateTime.Now.ToString("dd.MM.yyyy"),
                    SignerPowersConfirmationMethodSpecified = true,
                    SignerPowersConfirmationMethod = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerSignerPowersConfirmationMethod.Item3,
                    PowerOfAttorney = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.PowerOfAttorney
                    {
                        Electronic = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Electronic
                        {
                            Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Storage
                            {
                                UseDefault = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.StorageUseDefault.@false,
                                FullId = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.StorageFullId
                                {
                                    RegistrationNumber = organization.EmchdId,
                                    IssuerInn = organization.Inn
                                }
                            }
                        }
                    }
                };
            }

            document.Signers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signers
            {
                Signer = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signer[]
                {
                    signer
                }
            };

            int docLineCount = d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice ? d.DocGoodsDetailsIs.Count : d.Details.Count;
            document.DocumentShipments = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.DocumentRequisitesType[]
            {
                                new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.DocumentRequisitesType
                                {
                                    DocumentName = "Универсальный передаточный документ",
                                    DocumentNumber = d.Code,
                                    DocumentDate = d.DeliveryDate?.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy")
                                }
            };

            var details = new List<Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItem>();

            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
            {
                var additionalInfoList = new List<Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo>();

                if(edoGoodChannel != null)
                {
                    if (!string.IsNullOrEmpty(edoGoodChannel.NumberUpdId))
                        additionalInfoList.Add(new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo { Id = edoGoodChannel.NumberUpdId, Value = d.Code });

                    if (!string.IsNullOrEmpty(edoGoodChannel.OrderNumberUpdId))
                    {
                        var docJournalTag = _abt.DocJournalTags.FirstOrDefault(t => t.IdDoc == d.IdDocMaster && t.IdTad == 137);

                        if (docJournalTag == null)
                            throw new Exception("Отсутствует номер заказа покупателя.");

                        additionalInfoList.Add(new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo { Id = edoGoodChannel.OrderNumberUpdId, Value = docJournalTag?.TagValue ?? string.Empty });
                    }

                    if (!string.IsNullOrEmpty(edoGoodChannel.OrderDateUpdId))
                        additionalInfoList.Add(new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo { Id = edoGoodChannel.OrderDateUpdId, Value = d.DocMaster.DocDatetime.ToString("dd.MM.yyyy") });

                    if (!string.IsNullOrEmpty(edoGoodChannel.GlnShipToUpdId))
                    {
                        string glnShipTo = null;
                        var shipToGlnJournalTag = _abt.DocJournalTags.FirstOrDefault(t => t.IdDoc == d.IdDocMaster && t.IdTad == 222);

                        if(shipToGlnJournalTag != null)
                        {
                            glnShipTo = shipToGlnJournalTag.TagValue;
                        }
                        else if (WebService.Controllers.FinDbController.GetInstance().LoadedConfig)
                        {
                            var docOrderInfo = WebService.Controllers.FinDbController.GetInstance().GetDocOrderInfoByIdDocAndOrderStatus(d.IdDocMaster.Value);
                            glnShipTo = docOrderInfo?.GlnShipTo;
                        }

                        if(!string.IsNullOrEmpty(glnShipTo))
                            additionalInfoList.Add(new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo { Id = edoGoodChannel.GlnShipToUpdId, Value = glnShipTo });
                    }

                    foreach (var keyValuePair in edoGoodChannel.EdoValuesPairs)
                        additionalInfoList.Add(new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo { Id = keyValuePair.Key, Value = keyValuePair.Value });
                }

                if (additionalInfoList.Count > 0)
                    document.AdditionalInfoId = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfoId { AdditionalInfo = additionalInfoList.ToArray() };

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
                    else if(countryCode.Length == 2)
                        countryCode = "0" + countryCode;

                    var subtotal = Math.Round(docJournalDetail.Quantity * ((decimal)docJournalDetail.Price - (decimal)docJournalDetail.DiscountSumm), 2);
                    var vat = (decimal)Math.Round(subtotal * docJournalDetail.TaxRate / (docJournalDetail.TaxRate + 100), 2, MidpointRounding.AwayFromZero);

                    decimal price = 0;

                    if (docJournalDetail.Quantity > 0)
                        price = (decimal)Math.Round((subtotal - vat) / docJournalDetail.Quantity, 2, MidpointRounding.AwayFromZero);
                    else
                        price = (decimal)Math.Round(docJournalDetail.Price - docJournalDetail.DiscountSumm - docJournalDetail.TaxSumm, 2);

                    var detail = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItem
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
                        ItemVendorCode = barCode
                    };

                    if (!string.IsNullOrEmpty(refGood.CustomsNo))
                    {
                        detail.CustomsDeclarations = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemCustomsDeclaration[] 
                        {
                            new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemCustomsDeclaration
                            {
                                Country = countryCode,
                                DeclarationNumber = refGood.CustomsNo
                            }
                        };
                    }

                    switch (docJournalDetail.TaxRate)
                    {
                        case 0:
                            detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateUtd970.ZeroPercent;
                            break;
                        case 10:
                            detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateUtd970.TenPercent;
                            break;
                        //case 18:
                        //    detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateUtd970.;
                        //    break;
                        case 20:
                            detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateUtd970.TwentyPercent;
                            break;
                        default:
                            detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateUtd970.NoVat;
                            break;
                    }

                    decimal idGood = docJournalDetail.IdGood, idDoc = (decimal)d.IdDocMaster;

                    var docGoodDetailLabels = _abt?.Database?
                    .SqlQuery<string>($"select DM_LABEL from doc_goods_details_labels where id_doc_sale = {idDoc} and id_good = {idGood}")?
                    .ToList() ?? new List<string>();

                    if(docGoodDetailLabels.Count > 0)
                    {
                        detail.ItemMark = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemMark.Item4;
                        detail.ItemIdentificationNumbers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemIdentificationNumber[1];
                        detail.ItemIdentificationNumbers[0] = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemIdentificationNumber
                        {
                            ItemsElementName = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ItemsChoiceType[docGoodDetailLabels.Count],
                            Items = new string[docGoodDetailLabels.Count]
                        };

                        int j = 0;
                        foreach (var doc in docGoodDetailLabels)
                        {
                            detail.ItemIdentificationNumbers[0].ItemsElementName[j] = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ItemsChoiceType.Unit;
                            detail.ItemIdentificationNumbers[0].Items[j] = doc;
                            j++;
                        }
                    }


                    var detailAdditionalInfos = new List<Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo>();
                    if(edoGoodChannel != null)
                    {
                        var idChannel = edoGoodChannel.IdChannel;
                        if (!string.IsNullOrEmpty(edoGoodChannel.DetailBuyerCodeUpdId))
                        {
                            var goodMatching = _abt.RefGoodMatchings.FirstOrDefault(r => r.IdChannel == idChannel && r.IdGood == refGood.Id && r.Disabled == 0);

                            if(goodMatching == null)
                            {
                                var docDateTime = d.DocMaster.DocDatetime.Date;

                                goodMatching = _abt.RefGoodMatchings.FirstOrDefault(r => r.DisabledDatetime != null && r.IdChannel == idChannel && 
                                r.IdGood == refGood.Id && r.Disabled == 1 && r.DisabledDatetime.Value >= docDateTime);
                            }

                            if (goodMatching == null)
                                throw new Exception("Не все товары сопоставлены с кодами покупателя.");

                            if (!string.IsNullOrEmpty(goodMatching?.CustomerArticle))
                                detailAdditionalInfos.Add(new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo { Id = edoGoodChannel.DetailBuyerCodeUpdId, Value = goodMatching.CustomerArticle });
                            else
                                throw new Exception("Не для всех товаров заданы коды покупателя.");
                        }

                        if (!string.IsNullOrEmpty(edoGoodChannel.DetailBarCodeUpdId))
                            detailAdditionalInfos.Add(new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo { Id = edoGoodChannel.DetailBarCodeUpdId, Value = barCode });

                        if (!string.IsNullOrEmpty(edoGoodChannel.DetailPositionUpdId))
                            detailAdditionalInfos.Add(new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo { Id = edoGoodChannel.DetailPositionUpdId, Value = number.ToString() });
                    }

                    if(detailAdditionalInfos.Count > 0)
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
                foreach(var docJournalDetail in d.Details)
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
                    var detail = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItem
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
                        WithoutVat = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemWithoutVat.@true,
                        SubtotalWithVatExcluded = subtotal,
                        ItemVendorCode = barCode
                    };

                    if (!string.IsNullOrEmpty(refGood.CustomsNo))
                    {
                        detail.CustomsDeclarations = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemCustomsDeclaration[]
                        {
                            new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemCustomsDeclaration
                            {
                                Country = countryCode,
                                DeclarationNumber = refGood.CustomsNo
                            }
                        };
                    }

                    decimal idGood = docJournalDetail.IdGood, idDoc = (decimal)d.Id;

                    var docGoodDetailLabels = _abt?.Database?
                    .SqlQuery<string>($"select DM_LABEL from doc_goods_details_labels where id_doc_sale = {idDoc} and id_good = {idGood}")?
                    .ToList() ?? new List<string>();

                    if (docGoodDetailLabels.Count > 0)
                    {
                        detail.ItemMark = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemMark.Item4;
                        detail.ItemIdentificationNumbers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemIdentificationNumber[1];
                        detail.ItemIdentificationNumbers[0] = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemIdentificationNumber
                        {
                            ItemsElementName = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ItemsChoiceType[docGoodDetailLabels.Count],
                            Items = new string[docGoodDetailLabels.Count]
                        };

                        int j = 0;
                        foreach (var doc in docGoodDetailLabels)
                        {
                            detail.ItemIdentificationNumbers[0].ItemsElementName[j] = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ItemsChoiceType.Unit;
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

        public void ChooseFile()
        {
            _log.Log("ChooseFile: открытие окна выбора файла.");
            var xmlValidation = new XmlValidation();

            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "XML Files|*.xml";
            string FilePath;//TODO

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    if (xmlValidation.ValidationXmlByXsd(
                        openFileDialog.FileName,
                        Properties.Settings.Default.XsdSchemaUrl))
                    {
                        FilePath = openFileDialog.FileName;
                        OnPropertyChanged("FilePath");
                        _log.Log("ChooseFile: Файл успешно загружен.");
                    }
                    else
                    {
                        var errorWindow = new ErrorWindow(
                            "Произошла ошибка валидации файла.",
                            xmlValidation.Errors);

                        FilePath = string.Empty;
                        OnPropertyChanged("FilePath");

                        errorWindow.ShowDialog();
                        _log.Log($"Ошибка валидации: {string.Join("; ", xmlValidation.Errors)}");
                    }
                }
                catch(System.Net.WebException webEx)
                {
                    var errorWindow = new ErrorWindow(
                            "Произошла ошибка на удалённом сервере.",
                            new List<string>(
                                new string[]
                                {
                                    webEx.Message,
                                    webEx.StackTrace
                                }
                                ));

                    errorWindow.ShowDialog();
                    _log.Log("WebException: "+_log.GetRecursiveInnerException(webEx));
                }
                catch (Exception ex)
                {
                    var errorWindow = new ErrorWindow(
                            "Произошла ошибка.",
                            new List<string>(
                                new string[]
                                {
                                    ex.Message,
                                    ex.StackTrace
                                }
                                ));

                    errorWindow.ShowDialog();
                    _log.Log("Exception: " + _log.GetRecursiveInnerException(ex));
                }
            }
        }

        private void ShowDocumentSendHistory()
        {
            if (SelectedDocument == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбран документ.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedDocuments.Count > 1)
            {
                System.Windows.MessageBox.Show(
                    "Для просмотра истории нужно выбрать один документ.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedDocument.CurrentDocJournalId == null)
            {
                System.Windows.MessageBox.Show(
                    "Не задан документ из Трейдера.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var docs = (from doc in _abt.DocEdoProcessings
                       where doc != null && doc.IdDoc == SelectedDocument.CurrentDocJournalId
                       select doc)?.OrderBy(d => d.DocDate)?.ToList()?.Select(d => new DocEdoProcessingForLoading(d)) ?? new List<DocEdoProcessingForLoading>();

            var dataContext = new Base.ListViewModel<DocEdoProcessingForLoading>();
            dataContext.ItemsList = new System.Collections.ObjectModel.ObservableCollection<DocEdoProcessingForLoading>(docs);

            var showDocumentSendHistoryWindow = new ShowDocumentSendHistoryWindow(WorkWithDocumentsPermission);
            showDocumentSendHistoryWindow.AnnulmentDocument = (d) => 
            {
                if (d?.DocEdoProcessing == null)
                {
                    System.Windows.MessageBox.Show(
                        "Не выбрана отправка.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                if (d.DocEdoProcessing.DocStatus == (int)EdiProcessingUnit.Enums.DocEdoSendStatus.Rejected)
                {
                    System.Windows.MessageBox.Show(
                        "Документ ранее был отклонён.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                this.AnnulmentDocument(d.DocEdoProcessing);
            };
            showDocumentSendHistoryWindow.DataContext = dataContext;
            showDocumentSendHistoryWindow.ShowDialog();
        }

        private void LoadUnsentDocument()
        {
            try
            {
                var fileController = new WebService.Controllers.FileController();
                DateFrom = fileController.GetApplicationConfigParameter<DateTime>(Properties.Settings.Default.ApplicationName, "DocsDateTime");
            }
            catch(Exception ex)
            {
                var errorWindow = new ErrorWindow(
                            "Произошла ошибка.",
                            new List<string>(
                                new string[]
                                {
                                    ex.Message,
                                    ex.StackTrace
                                }
                                ));

                errorWindow.ShowDialog();
                _log.Log("Exception: " + _log.GetRecursiveInnerException(ex));
                return;
            }

            DateTo = DateTime.Now;
            OnPropertyChanged("DateFrom");
            OnPropertyChanged("DateTo");

            if (_abt != null)
            {
                _abt.Dispose();
                _abt = null;
                UniversalTransferDocument.DbContext = null;
            }

            if (SelectedFilial != null)
            {
                _abt = new AbtDbContext(_config.GetConnectionStringByUser(SelectedFilial), true);
                SetPermissions();

                if (Organizations.Count == 0)
                    SetMyOrganizations();
                else
                {
                    var dataBaseUser = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser;
                    var customers = from cust in _abt.RefCustomers
                                    join refUser in _abt.RefUsersByOrgEdo
                                    on cust.Id equals refUser.IdCustomer
                                    where refUser.UserName == dataBaseUser
                                    select cust;

                    foreach (var org in Organizations)
                        org.Kpp = customers?.FirstOrDefault(c => c.Inn == org.Inn)?.Kpp;

                    SelectedOrganization = null;
                    OnAllPropertyChanged();
                }
            }
            else
                _abt = new AbtDbContext();

            var appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            _abt.ExecuteProcedure("DBMS_APPLICATION_INFO.set_client_info", new Oracle.ManagedDataAccess.Client.OracleParameter("client_info", appVersion));

            GetDocuments(true);
            SelectedOrganization = null;
            SelectedDocuments = new List<UniversalTransferDocument>();
            Documents = new List<UniversalTransferDocument>();
            DocumentDetails = new List<UniversalTransferDocumentDetail>();
            OnPropertyChanged("Documents");
            OnPropertyChanged("DocumentDetails");
            OnPropertyChanged("SelectedOrganization");
            OnPropertyChanged("SelectedDocuments");
        }

        private async void ShowCorrectionDocuments()
        {
            if (SelectedOrganization == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбрана организация.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedOrganization.Certificate == null)
            {
                System.Windows.MessageBox.Show(
                    "Не задан сертификат отправителя.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                bool result = Edo.GetInstance().Authenticate(false, SelectedOrganization.Certificate, SelectedOrganization.Inn);

                if (!result)
                    throw new Exception("Не удалось авторизоваться в системе по сертификату.");

                _loadContext = new LoadModel();
                var loadWindow = new LoadWindow();
                loadWindow.DataContext = _loadContext;

                if (_owner != null)
                    loadWindow.Owner = _owner;

                var correctionDocumentsModel = new CorrectionDocumentsModel(_abt, SelectedOrganization);
                correctionDocumentsModel.DateTo = this.DateTo;
                correctionDocumentsModel.DateFrom = this.DateFrom;

                if(SelectedDocuments.Count == 1)
                    correctionDocumentsModel.SearchInvoiceNumber = SelectedDocument.DocJournal.Code;

                await correctionDocumentsModel.Refresh();

                var correctionDocumentsWindow = new ShowCorrectionDocumentsWindow();
                correctionDocumentsWindow.DataContext = correctionDocumentsModel;
                correctionDocumentsModel.Owner = correctionDocumentsWindow;

                correctionDocumentsWindow.ShowDialog();
                OnPropertyChanged("Documents");
                OnPropertyChanged("SelectedDocuments");
            }
            catch(Exception ex)
            {
                _log.Log($"ShowCorrectionDocuments Exception: {_log.GetRecursiveInnerException(ex)}");

                var errorWindow = new ErrorWindow(
                            "Произошла ошибка загрузки документов.",
                            new List<string>(
                                new string[]
                                {
                                    ex.Message,
                                    ex.StackTrace
                                }
                                ));

                errorWindow.ShowDialog();
            }
        }

        private async void UploadProductsAndCodesToEdoLite()
        {
            if (SelectedOrganization == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбрана организация.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedDocument == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбран документ.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedDocuments.Count > 1)
            {
                System.Windows.MessageBox.Show(
                    "Для выгрузки нужно выбрать один документ.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedDocument.CurrentDocJournalId == null)
            {
                System.Windows.MessageBox.Show(
                    "Не задан документ из Трейдера.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                var changePathDialog = new Microsoft.Win32.SaveFileDialog();
                changePathDialog.Title = "Сохранение файла";
                changePathDialog.Filter = "CSV Files|*.csv";
                changePathDialog.FileName = $"{SelectedDocument.DocJournal.Code}.csv";

                if (changePathDialog.ShowDialog() == true)
                {
                    decimal idDoc = SelectedDocument.CurrentDocJournalId.Value;

                    var markedCodes = _abt.DocGoodsDetailsLabels?
                    .Where(l => l.IdDocSale == idDoc)?.ToList();

                    using (var fileStream = new System.IO.FileStream(changePathDialog.FileName, System.IO.FileMode.Create))
                    {
                        using (var streamWriter = new System.IO.StreamWriter(fileStream))
                        {
                            int i = 0;
                            if (SelectedDocument.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
                            {
                                foreach (var detail in SelectedDocument.DocJournal.DocGoodsDetailsIs)
                                {
                                    var subtotal = Math.Round(detail.Quantity * ((decimal)detail.Price - (decimal)detail.DiscountSumm), 2);
                                    var vat = (decimal)Math.Round(subtotal * detail.TaxRate / (detail.TaxRate + 100), 2, MidpointRounding.AwayFromZero);

                                    decimal price = 0;

                                    if (detail.Quantity > 0)
                                        price = (decimal)Math.Round((subtotal - vat) / detail.Quantity, 2, MidpointRounding.AwayFromZero);
                                    else
                                        price = (decimal)Math.Round(detail.Price - detail.DiscountSumm - detail.TaxSumm, 2);

                                    string csvPositionStr = $"{++i},{EscapeCsvString(detail.Good.Name)},{price},{detail.Quantity},{Properties.Settings.Default.DefaultUnit},{detail.TaxRate}%";

                                    var markedCodesForDetail = markedCodes?.Where(m => m.IdGood == detail.IdGood)?.ToList() ?? new List<DocGoodsDetailsLabels>();

                                    if (markedCodesForDetail.Count > 0)
                                        csvPositionStr = csvPositionStr + ",КИЗ";

                                    foreach(var markedCode in markedCodesForDetail)
                                    {
                                        var dmLabel = EscapeCsvString(markedCode.DmLabel);
                                        csvPositionStr = csvPositionStr + $",{dmLabel}";
                                    }

                                    await streamWriter.WriteLineAsync(csvPositionStr);
                                }
                            }
                            else
                            {
                                foreach (var detail in SelectedDocument.DocJournal.Details)
                                {
                                    var price = (decimal)Math.Round(detail.Price - detail.DiscountSumm, 2);
                                    string csvPositionStr = $"{++i},{EscapeCsvString(detail.Good.Name)},{price},{detail.Quantity},{Properties.Settings.Default.DefaultUnit},20%";

                                    var markedCodesForDetail = markedCodes?.Where(m => m.IdGood == detail.IdGood)?.ToList() ?? new List<DocGoodsDetailsLabels>();

                                    if (markedCodesForDetail.Count > 0)
                                        csvPositionStr = csvPositionStr + ",КИЗ";

                                    foreach (var markedCode in markedCodesForDetail)
                                    {
                                        var dmLabel = EscapeCsvString(markedCode.DmLabel);
                                        csvPositionStr = csvPositionStr + $",{dmLabel}";
                                    }

                                    await streamWriter.WriteLineAsync(csvPositionStr);
                                }
                            }
                        }
                    }

                    _loadContext = new LoadModel();
                    var loadWindow = new LoadWindow();
                    loadWindow.DataContext = _loadContext;
                    loadWindow.SetSuccessFullLoad(_loadContext, $"Файл успешно сохранён.");
                    loadWindow.ShowDialog();

                    _log.Log("UploadProductsAndCodesToEdoLite: успешно завершено.");
                }
            }
            catch(Exception ex)
            {
                _log.Log($"UploadProductsAndCodesToEdoLite Exception: {_log.GetRecursiveInnerException(ex)}");

                var errorWindow = new ErrorWindow(
                            "Произошла ошибка выгрузки товаров с кодами.",
                            new List<string>(
                                new string[]
                                {
                                    ex.Message,
                                    ex.StackTrace
                                }
                                ));

                errorWindow.ShowDialog();
            }
        }

        private string EscapeCsvString(string objStr)
        {
            if (objStr == null)
                return "";

            if (objStr.Contains(',') || objStr.Contains('"') || objStr.Contains('\n') || objStr.Contains('\r'))
            {
                string result = objStr.Replace("\"", "\"\"");
                return "\"" + result + "\"";
            }

            return objStr;
        }
    }
}
