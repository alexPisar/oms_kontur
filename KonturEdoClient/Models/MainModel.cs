﻿using System;
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
        private Kontragent _consignor;
        private bool _authInHonestMark;

        public RelayCommand RefreshCommand => new RelayCommand((o) => { Refresh(); });
        public RelayCommand LoadXmlCommand => new RelayCommand((o) => { LoadXml(); });
        public RelayCommand SendCommand => new RelayCommand((o) => { Send(); });
        public RelayCommand ShowMarkedCodesCommand => new RelayCommand((o) => { ShowMarkedCodes(); });
        public RelayCommand ReprocessDocumentCommand => new RelayCommand((o) => { ReprocessDocument(); });
        public RelayCommand AnnulmentDocumentCommand => new RelayCommand((o) => { AnnulmentDocument(); });
        public List<Kontragent> Organizations { get; set; }
        public Kontragent SelectedOrganization { get; set; }

        public DateTime DateTo { get; set; } = DateTime.Now;
        public DateTime DateFrom { get; set; } = DateTime.Today;
        public bool OnlyMarkedOrders { get; set; }
        public bool IsDocumentMarked { get { return SelectedDocument?.IsMarked ?? false; } }
        public bool WorkWithDocumentsPermission { get; set; }
        public bool PermissionShowMarkedCodes { get; set; }
        public bool PermissionReturnMarkedCodes { get; set; }
        public bool DocumentWithErrorStatus => ((DocComissionEdoProcessing)SelectedDocument?.ProcessingStatus)?.DocStatus == (int?)HonestMark.DocEdoProcessingStatus.ProcessingError;

        public List<UniversalTransferDocument> Documents { get; set; }
        public UniversalTransferDocument SelectedDocument { get; set; }

        public List<KeyValuePair<DataContextManagementUnit.DataAccess.DocJournalType, string>> DocTypes { get; set; }
        public DataContextManagementUnit.DataAccess.DocJournalType? SelectedDocType => (DataContextManagementUnit.DataAccess.DocJournalType?)(_owner as MainWindow)?.DocTypesBar?.EditValue;

        public List<EdiProcessingUnit.User> Filials { get; set; }
        public EdiProcessingUnit.User SelectedFilial { get; set; }

        public List<UniversalTransferDocumentDetail> DocumentDetails { get; set; }
        public UniversalTransferDocumentDetail SelectedDetail { get; set; }

        public MainModel(AbtDbContext abt, X509Certificate2 consignorCertificate = null, Utils.XmlSignUtils utils = null, bool authInHonestMark = false)
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

            _consignor = Edo.GetInstance().GetKontragentByInnKpp(UtilitesLibrary.ConfigSet.Config.GetInstance().ConsignorInn);

            if (consignorCertificate != null)
                _consignor.Certificate = consignorCertificate;

            _authInHonestMark = authInHonestMark;
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
                && !_abt.Entry(SelectedDocument.DocJournal).Collection(d => d.DocGoodsDetailsIs).IsLoaded)
                _abt.Entry(SelectedDocument.DocJournal).Collection(d => d.DocGoodsDetailsIs).Load();
            else if(SelectedDocument.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Translocation
                && !_abt.Entry(SelectedDocument.DocJournal).Collection(d => d.Details).IsLoaded)
                _abt.Entry(SelectedDocument.DocJournal).Collection(d => d.Details).Load();

            DocumentDetails = SelectedDocument?.Details?.ToList();
            SelectedDetail = null;
            OnPropertyChanged("DocumentDetails");
            OnPropertyChanged("SelectedDetail");
            OnPropertyChanged("IsDocumentMarked");
            OnPropertyChanged("DocumentWithErrorStatus");
        }

        private void Refresh()
        {
            if (SelectedFilial != null)
            {
                _abt = new AbtDbContext(_config.GetConnectionStringByUser(SelectedFilial), true);
                SetMyOrganizations();
            }
            else
                _abt = new AbtDbContext();

            GetDocuments();
            SelectedOrganization = null;
            SelectedDocument = null;
            Documents = new List<UniversalTransferDocument>();
            DocumentDetails = new List<UniversalTransferDocumentDetail>();
            OnPropertyChanged("Documents");
            OnPropertyChanged("DocumentDetails");
            OnPropertyChanged("SelectedOrganization");
            OnPropertyChanged("SelectedDocument");
        }

        private void SetDocTypes()
        {
            DocTypes = new List<KeyValuePair<DataContextManagementUnit.DataAccess.DocJournalType, string>>();

            DocTypes.Add(new KeyValuePair<DataContextManagementUnit.DataAccess.DocJournalType, string>(DataContextManagementUnit.DataAccess.DocJournalType.Invoice, "Счёт-фактура"));
            DocTypes.Add(new KeyValuePair<DataContextManagementUnit.DataAccess.DocJournalType, string>(DataContextManagementUnit.DataAccess.DocJournalType.Translocation, "Перемещение"));
        }

        private void SetPermissions()
        {
            var param = new Oracle.ManagedDataAccess.Client.OracleParameter("UName", UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser);
            param.OracleDbType = Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2;

            var userEdoPermission = _abt.Database.SqlQuery<RefUserEdoPermissions>("select USER_NAME as UserName," +
                "WORK_WITH_DOCUMENTS_PERMISSION as WorkWithDocuments, PERMISSION_SHOW_MARKED_CODES as ShowMarkedCodes," +
                $"PERMISSION_RETURN_MARKED_CODES as ReturnMarkedCodes from EDI.REF_USER_EDO_PERMISSIONS where USER_NAME = :UName", param).FirstOrDefault();

            if(userEdoPermission != null)
            {
                WorkWithDocumentsPermission = userEdoPermission.WorkWithDocuments != 0;
                PermissionShowMarkedCodes = userEdoPermission.ShowMarkedCodes != 0;
                PermissionReturnMarkedCodes = userEdoPermission.ReturnMarkedCodes != 0;
            }
        }

        private Diadoc.Api.DataXml.Utd820.UniversalTransferDocumentBuyerTitle CreateBuyerShipmentDocument(Kontragent receiverOrganization)
        {
            var firstMiddleName = _utils.ParseCertAttribute(receiverOrganization.Certificate.Subject, "G");
            string signerFirstName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
            string signerMiddleName = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;

            var signers = new[]
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

            if (signers.First().Inn == receiverOrganization.Inn)
                signers.First().SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.LegalEntity;
            else if (signers.First().Inn?.Length == 12)
            {
                signers.First().SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.PhysicalPerson;
                signers.First().SignerPowersBase = signers.First().Position;
            }
            else
                signers.First().SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.IndividualEntity;

            var document = new Diadoc.Api.DataXml.Utd820.UniversalTransferDocumentBuyerTitle()
            {
                DocumentCreator = _utils.ParseCertAttribute(receiverOrganization.Certificate.Subject, "SN") + " " + _utils.ParseCertAttribute(receiverOrganization.Certificate.Subject, "G"),
                Signers = signers,
                OperationContent = "Товары переданы"
            };

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
                DocumentCreator = _utils.ParseCertAttribute(senderOrganization.Certificate.Subject, "SN") + " " + _utils.ParseCertAttribute(senderOrganization.Certificate.Subject, "G"),
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

            var firstMiddleName = _utils.ParseCertAttribute(senderOrganization.Certificate.Subject, "G");
            string signerFirstName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
            string signerMiddleName = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;

            var signer = new[]
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

        public async void GetDocuments()
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

                        if(SelectedDocType == DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
                            _loadedDocuments[index] = (from doc in docs
                                                   where doc != null && doc.DocGoodsI != null && doc.DocMaster != null && doc.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && doc.DocDatetime >= DateFrom
                                                   join docMaster in _abt.DocJournals on doc.IdDocMaster equals docMaster.Id
                                                   where docMaster != null && docMaster.DocGoods != null && docMaster.DocDatetime >= DateFrom && docMaster.DocDatetime < DateTo
                                                   join docGoods in _abt.DocGoods on docMaster.Id equals docGoods.IdDoc
                                                   join customer in _abt.RefCustomers on docGoods.IdSeller equals customer.IdContractor
                                                   where customer.Inn == organization.Inn && customer.Kpp == organization.Kpp
                                                   let isMarked = (from label in _abt.DocGoodsDetailsLabels where label.IdDocSale == docMaster.Id select label).Count() > 0
                                                   let honestMarkStatus = (from docComissionEdoProcessing in _abt.DocComissionEdoProcessings
                                                                           where docComissionEdoProcessing.IdDoc == docMaster.Id
                                                                           orderby docComissionEdoProcessing.DocDate descending
                                                                           select docComissionEdoProcessing)
                                                   let docEdoProcessing = (from docEdo in _abt.DocEdoProcessings
                                                                           where docEdo.IdDoc == docMaster.Id
                                                                           orderby docEdo.DocDate descending
                                                                           select docEdo)
                                                   where isMarked || !OnlyMarkedOrders
                                                   join buyerContractor in _abt.RefContractors
                                                   on docGoods.IdCustomer equals buyerContractor.Id
                                                   join buyerCustomer in _abt.RefCustomers
                                                   on buyerContractor.DefaultCustomer equals buyerCustomer.Id
                                                   select new UniversalTransferDocument
                                                   {
                                                       Total = doc.DocGoodsI.TotalSumm,
                                                       Vat = doc.DocGoodsI.TaxSumm,
                                                       TotalWithVatExcluded = (doc.DocGoodsI.TotalSumm - doc.DocGoodsI.TaxSumm),
                                                       ProcessingStatus = honestMarkStatus,
                                                       EdoProcessing = docEdoProcessing,
                                                       DocJournal = doc,
                                                       DocJournalNumber = docMaster.Code,
                                                       DocumentNumber = doc.Code,
                                                       OrgId = organization.OrgId,

                                                       SenderName = organization.Name,

                                                       SenderInnKpp = senderInnKpp,

                                                       SenderAddress = addressSender,

                                                       BuyerCustomer = buyerCustomer,

                                                       SellerContractor = docMaster.DocGoods.Seller,

                                                       BuyerContractor = buyerContractor,

                                                       IsMarked = isMarked
                                                   }).ToList();
                        else if(SelectedDocType == DataContextManagementUnit.DataAccess.DocJournalType.Translocation)
                            _loadedDocuments[index] = (from doc in docs
                                                       where doc != null && doc.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Translocation && doc.DocDatetime >= DateFrom && doc.DocDatetime < DateTo
                                                       join docGoods in _abt.DocGoods on doc.Id equals docGoods.IdDoc
                                                       where docGoods.IdCustomer != 0
                                                       join customer in _abt.RefCustomers on docGoods.IdSeller equals customer.IdContractor
                                                       where customer.Inn == organization.Inn && customer.Kpp == organization.Kpp
                                                       let isMarked = (from label in _abt.DocGoodsDetailsLabels where label.IdDocSale == doc.Id select label).Count() > 0
                                                       let honestMarkStatus = (from docComissionEdoProcessing in _abt.DocComissionEdoProcessings
                                                                               where docComissionEdoProcessing.IdDoc == doc.Id
                                                                               orderby docComissionEdoProcessing.DocDate descending
                                                                               select docComissionEdoProcessing)
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
                                                           ProcessingStatus = honestMarkStatus,
                                                           DocJournal = doc,
                                                           DocJournalNumber = doc.Code,
                                                           DocumentNumber = doc.Code,
                                                           OrgId = organization.OrgId,

                                                           SenderName = organization.Name,

                                                           SenderInnKpp = senderInnKpp,

                                                           SenderAddress = addressSender,

                                                           BuyerCustomer = buyerCustomer,

                                                           StoreSender = storeSender,

                                                           StoreRecipient = storeRecipient,

                                                           IsMarked = isMarked
                                                       }).ToList();

                        var docProcessings = from doc in _loadedDocuments[index]
                                             where doc.ProcessingStatus as DocComissionEdoProcessing != null &&
                                             ((DocComissionEdoProcessing)doc.ProcessingStatus).DocStatus == (int)HonestMark.DocEdoProcessingStatus.Sent
                                             select doc.ProcessingStatus as DocComissionEdoProcessing;

                        if(docProcessings.Count() != 0)
                        {
                            foreach(var docProcessing in docProcessings)
                            {
                                try
                                {
                                    if (_authInHonestMark)
                                    {
                                        var docProcessingInfo = HonestMark.HonestMarkClient.GetInstance().GetEdoDocumentProcessInfo(docProcessing.FileName);

                                        if (docProcessingInfo.Code == HonestMark.HonestMarkProcessResultStatus.SUCCESS)
                                        {
                                            docProcessing.DocStatus = (int)HonestMark.DocEdoProcessingStatus.Processed;
                                        }
                                        else if (docProcessingInfo.Code == HonestMark.HonestMarkProcessResultStatus.FAILED)
                                        {
                                            docProcessing.DocStatus = (int)HonestMark.DocEdoProcessingStatus.ProcessingError;

                                            var failedOperations = docProcessingInfo?.Operations?.Select(o => o.Details)?.Where(o => o.Successful == false);

                                            var errors = failedOperations.SelectMany(f => f.Errors);

                                            var errorsListStr = new List<string>();
                                            foreach (var error in errors)
                                            {
                                                if (!string.IsNullOrEmpty(error.Text))
                                                    errorsListStr.Add($"Произошла ошибка с кодом:{error.Code} \nОписание:{error.Text}\n");
                                                else if (!string.IsNullOrEmpty(error?.Error?.Detail))
                                                    errorsListStr.Add($"Произошла ошибка с кодом:{error.Code} \nДетали:{error?.Error?.Detail}\n");
                                                else
                                                    errorsListStr.Add($"Произошла ошибка с кодом:{error.Code}\n");
                                            }

                                            docProcessing.ErrorMessage = string.Join("\n\n", errorsListStr);
                                        }
                                    }
                                    else
                                    {
                                        var doc = Edo.GetInstance().GetDocument(docProcessing.MessageId, docProcessing.EntityId);

                                        if (doc == null)
                                            throw new Exception($"Не удалось найти комиссионный документ в Диадоке. ID {docProcessing.Id}");

                                        var lastDocFlow = doc.LastOuterDocflows?.FirstOrDefault(l => l?.OuterDocflow?.DocflowNamedId == "TtGis" && l.OuterDocflow?.Status?.Type != null);
                                        Diadoc.Api.Proto.OuterStatusType? statusDocFlow = lastDocFlow?.OuterDocflow?.Status?.Type;

                                        if (statusDocFlow == Diadoc.Api.Proto.OuterStatusType.Success)
                                            docProcessing.DocStatus = (int)HonestMark.DocEdoProcessingStatus.Processed;
                                        else if (statusDocFlow == Diadoc.Api.Proto.OuterStatusType.Error)
                                        {
                                            docProcessing.DocStatus = (int)HonestMark.DocEdoProcessingStatus.ProcessingError;

                                            var errors = lastDocFlow.OuterDocflow.Status?.Details ?? new List<Diadoc.Api.Proto.StatusDetail>();

                                            var errorsListStr = new List<string>();
                                            foreach (var error in errors)
                                                errorsListStr.Add($"Произошла ошибка с кодом:{error.Code} \nОписание:{error.Text}\n");

                                            docProcessing.ErrorMessage = string.Join("\n\n", errorsListStr);
                                        }
                                    }
                                }
                                catch(Exception ex)
                                {
                                    string errorMessage = $"Ошибка получения статуса в Честном знаке, документ {docProcessing.FileName} \nException: {_log.GetRecursiveInnerException(ex)}";
                                    _log.Log(errorMessage);
                                }
                            }
                            _abt.SaveChanges();
                        }

                        var sendedDocsEdoProcessing = from doc in _loadedDocuments[index]
                                                      join docProc in _abt.DocEdoProcessings
                                                      on doc.CurrentDocJournalId equals docProc.IdDoc
                                                      where docProc.DocStatus == (int)EdiProcessingUnit.Enums.DocEdoSendStatus.Sent
                                                      select docProc;

                        if(sendedDocsEdoProcessing.Count() > 0)
                        {
                            foreach(var docProcessing in sendedDocsEdoProcessing)
                            {
                                try
                                {
                                    var doc = Edo.GetInstance().GetDocument(docProcessing.MessageId, docProcessing.EntityId);

                                    _abt.Entry(docProcessing)?.Reload();
                                    if (doc.RecipientResponseStatus == Diadoc.Api.Proto.Documents.RecipientResponseStatus.WithRecipientSignature)
                                    {
                                        docProcessing.DocStatus = (int)EdiProcessingUnit.Enums.DocEdoSendStatus.Signed;
                                        _abt.SaveChanges();
                                    }
                                    else if(doc.RecipientResponseStatus == Diadoc.Api.Proto.Documents.RecipientResponseStatus.RecipientSignatureRequestRejected)
                                    {
                                        docProcessing.DocStatus = (int)EdiProcessingUnit.Enums.DocEdoSendStatus.Rejected;
                                        _abt.SaveChanges();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    string errorMessage = $"Произошла ошибка проверки статуса отправки документа с ID {docProcessing.Id}\r\nException: {_log.GetRecursiveInnerException(ex)}";
                                    _log.Log(errorMessage);
                                    errorsList.Add(ex);
                                }
                            }
                        }

                        if (!_authInHonestMark)
                        {
                            _log.Log($"Для организации {organization.Name} получено документов {_loadedDocuments[index].Count}");
                            index++;
                            continue;
                        }

                        try
                        {
                            HonestMark.HonestMarkClient.GetInstance().Authorization(organization.Certificate);

                            var docProcessingsForReprocessing = (from doc in _loadedDocuments[index]
                                                                 where doc.ProcessingStatus as DocComissionEdoProcessing != null &&
                                                                 ((DocComissionEdoProcessing)doc.ProcessingStatus).DocStatus == (int)HonestMark.DocEdoProcessingStatus.Processed
                                                                 select (DocComissionEdoProcessing)doc.ProcessingStatus).SelectMany(s => s.MainDocuments).Where(m => m.IsReprocessingStatus == 1);

                            if ((docProcessingsForReprocessing?.Count() ?? 0) != 0)
                            {
                                foreach (var docProcessing in docProcessingsForReprocessing)
                                {
                                    try
                                    {
                                        if (docProcessing.DocStatus == (int)EdiProcessingUnit.Enums.DocEdoSendStatus.Signed)
                                        {
                                            var docProcessingInfo = HonestMark.HonestMarkClient.GetInstance().GetEdoDocumentProcessInfo(docProcessing.FileName);

                                            if (docProcessingInfo.Code == HonestMark.HonestMarkProcessResultStatus.FAILED)
                                            {
                                                HonestMark.HonestMarkClient.GetInstance().ReprocessDocument(docProcessing.FileName);
                                                docProcessing.IsReprocessingStatus = 2;
                                            }
                                            else if (docProcessingInfo.Code == HonestMark.HonestMarkProcessResultStatus.SUCCESS)
                                                docProcessing.IsReprocessingStatus = 0;
                                        }
                                        else
                                        {
                                            docProcessing.IsReprocessingStatus = 0;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        string errorMessage = $"Ошибка повторной обработки документа {docProcessing.FileName} \nException: {_log.GetRecursiveInnerException(ex)}";
                                        _log.Log(errorMessage);
                                        errorsList.Add(ex);
                                    }
                                }

                                _abt.SaveChanges();
                            }

                            var docProcessingsForChecking = (from doc in _loadedDocuments[index]
                                                                 where doc.ProcessingStatus as DocComissionEdoProcessing != null &&
                                                                 ((DocComissionEdoProcessing)doc.ProcessingStatus).DocStatus == (int)HonestMark.DocEdoProcessingStatus.Processed
                                                                 select (DocComissionEdoProcessing)doc.ProcessingStatus).SelectMany(s => s.MainDocuments)
                                                                 .Where(m => m.AnnulmentStatus == (int)HonestMark.AnnulmentDocumentStatus.RevokedWaitProcessing);

                            if ((docProcessingsForChecking?.Count() ?? 0) != 0)
                            {
                                foreach (var docProcessing in docProcessingsForChecking)
                                {
                                    try
                                    {
                                        if (string.IsNullOrEmpty(docProcessing.AnnulmentFileName))
                                            throw new Exception($"Не указан файл аннулирования для документа {docProcessing.FileName}");

                                        var docProcessingInfo = HonestMark.HonestMarkClient.GetInstance().GetEdoDocumentProcessInfo(docProcessing.AnnulmentFileName);

                                        if(docProcessingInfo.Code == HonestMark.HonestMarkProcessResultStatus.FAILED)
                                        {
                                            docProcessing.AnnulmentStatus = (int)HonestMark.AnnulmentDocumentStatus.Error;
                                            errorsList.Add(new Exception($"Возникла ошибка обработки документа {docProcessing.AnnulmentFileName} в Честном знаке"));
                                        }
                                        else if(docProcessingInfo.Code == HonestMark.HonestMarkProcessResultStatus.SUCCESS)
                                        {
                                            docProcessing.AnnulmentStatus = (int)HonestMark.AnnulmentDocumentStatus.RevokedAndProcessed;
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

                            var docsForAnnulmentProcessing = from doc in _loadedDocuments[index]
                                                             join docProc in _abt.DocEdoProcessings
                                                             on doc.CurrentDocJournalId equals docProc.IdDoc
                                                             where docProc.AnnulmentStatus == (int)HonestMark.AnnulmentDocumentStatus.Requested
                                                             select docProc;

                            if ((docsForAnnulmentProcessing?.Count() ?? 0) != 0)
                            {
                                foreach (var docProcessing in docsForAnnulmentProcessing)
                                {
                                    try
                                    {
                                        var doc = Edo.GetInstance().GetDocument(docProcessing.MessageId, docProcessing.EntityId);

                                        if(doc.RevocationStatus == Diadoc.Api.Proto.Documents.RevocationStatus.RevocationAccepted)
                                        {
                                            if(doc.RecipientResponseStatus == Diadoc.Api.Proto.Documents.RecipientResponseStatus.WithRecipientSignature 
                                            && docProcessing.ComissionDocument != null)
                                                docProcessing.AnnulmentStatus = (int)HonestMark.AnnulmentDocumentStatus.RevokedWaitProcessing;
                                            else
                                                docProcessing.AnnulmentStatus = (int)HonestMark.AnnulmentDocumentStatus.Revoked;
                                        }
                                        else if(doc.RevocationStatus == Diadoc.Api.Proto.Documents.RevocationStatus.RevocationRejected)
                                        {
                                            docProcessing.AnnulmentStatus = (int)HonestMark.AnnulmentDocumentStatus.Rejected;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        string errorMessage = $"Ошибка получения статуса аннулирования документа {docProcessing.AnnulmentFileName} \nException: {_log.GetRecursiveInnerException(ex)}";
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
                        finally
                        {
                            HonestMark.HonestMarkClient.GetInstance().Authorization(_consignor.Certificate);
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
                SelectedDocument = null;
                SelectedDetail = null;
            }

            OnPropertyChanged("Documents");
            OnPropertyChanged("DocumentDetails");
            OnPropertyChanged("SelectedDocument");
            OnPropertyChanged("SelectedDetail");
            OnPropertyChanged("IsDocumentMarked");
        }

        public void SetSelectedFilial(string userId)
        {
            SelectedFilial = Filials.FirstOrDefault(f => f.UserGLN == userId);
        }

        private List<Kontragent> SetCounteragents()
        {
            _log.Log($"SetCounteragents: загрузка контрагентов для организации {SelectedOrganization.Name}, OrgId: {SelectedOrganization.OrgId}");
            var counteragents = Edo.GetInstance().GetOrganizations(SelectedOrganization.OrgId);
            OnAllPropertyChanged();
            _log.Log("SetCounteragents: выполнено");
            return counteragents;
        }

        private void SetMyOrganizations()
        {
            _log.Log($"SetMyOrganizations: загрузка организаций для пользователя с именем {UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser}");

            var dataBaseUser = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser;
            var customers = (from cust in _abt.RefCustomers
                            join refUser in _abt.RefUsersByOrgEdo
                            on cust.Id equals refUser.IdCustomer
                            where refUser.UserName == dataBaseUser
                            select cust).ToArray();

            var orgs = customers.Select(c => new Kontragent(c.Name, c.Inn, c.Kpp)).ToList();

            List<X509Certificate2> personalCertificates = GetPersonalCertificates();

            foreach (var org in orgs)
            {
                var certs = personalCertificates.Where(c => org.Inn == _utils.GetOrgInnFromCertificate(c) && _utils.IsCertificateValid(c) && c.NotAfter > DateTime.Now).OrderByDescending(c => c.NotBefore);
                org.Certificate = certs.FirstOrDefault();

                if (org.Certificate != null)
                {
                    try
                    {
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

            var certificates = crypto.GetAllGostPersonalCertificates();

            _log.Log("GetPersonalCertificates: выполнено.");
            return certificates;
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

                var signerDetails = Edo.GetInstance().GetExtendedSignerDetails(Diadoc.Api.Proto.Invoicing.Signers.DocumentTitleType.UtdSeller);
                var doc = GetUniversalDocument(SelectedDocument.DocJournal, SelectedOrganization, employee, signerDetails);
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

            if ((SelectedDocument.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && SelectedDocument.DocJournal.DocMaster.ActStatus < 5) ||
                (SelectedDocument.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Translocation && SelectedDocument.DocJournal.ActStatus < 5))
            {
                System.Windows.MessageBox.Show(
                    "Товар по данному документу не вывезен.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                bool result = Edo.GetInstance().Authenticate(false, SelectedOrganization.Certificate, SelectedOrganization.Inn);

                if (!result)
                    throw new Exception("Не удалось авторизоваться в системе по сертификату.");

                SendWindow sendWindow = new SendWindow();

                string employee = _abt.SelectSingleValue("select const_value from ref_const where id = 1200");

                var signerDetails = Edo.GetInstance().GetExtendedSignerDetails(Diadoc.Api.Proto.Invoicing.Signers.DocumentTitleType.UtdSeller);
                var doc = GetUniversalDocument(SelectedDocument.DocJournal, SelectedOrganization, employee, signerDetails);
                SendModel sendModel = new SendModel(_abt, SelectedOrganization, SelectedOrganization.Certificate, doc, SelectedDocument.CurrentDocJournalId, (DataContextManagementUnit.DataAccess.DocJournalType)SelectedDocument.DocJournal.IdDocType, _authInHonestMark);

                if (SelectedDocument.IsMarked)
                {
                    sendModel.IsReceivedMarkData = ((DocComissionEdoProcessing)SelectedDocument.ProcessingStatus)?.DocStatus == (int)HonestMark.DocEdoProcessingStatus.Processed ||
                        ((DocComissionEdoProcessing)SelectedDocument.ProcessingStatus)?.DocStatus == (int)HonestMark.DocEdoProcessingStatus.Sent;

                    if (_consignor.Certificate != null && !sendModel.IsReceivedMarkData)
                        sendModel.BeforeSendEventHandler += (object s, EventArgs e) => { SendComissionDocumentForHonestMark(s); };
                }

                sendModel.Organizations = SetCounteragents();
                sendWindow.DataContext = sendModel;
                sendModel.Owner = sendWindow;

                if (_owner != null)
                    sendWindow.Owner = _owner;
                
                sendWindow.ShowDialog();

                if (sendModel.EdoProcessing != null)
                {
                    SelectedDocument.EdoProcessing = sendModel.EdoProcessing;
                    OnPropertyChanged("Documents");
                    OnPropertyChanged("SelectedDocument");
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

            if (_consignor.Certificate == null)
                return;

            try
            {
                bool isSentData = false;

                var labels = (from label in _abt.DocGoodsDetailsLabels
                              where label.IdDocSale == SelectedDocument.CurrentDocJournalId
                              select label)?.ToList() ?? new List<DocGoodsDetailsLabels>();

                if ((SelectedDocument.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice &&
                    (SelectedDocument?.DocJournal?.DocGoodsDetailsIs?
                    .Exists(g => (labels?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) < g.Quantity) ?? false)) ||
                    (SelectedDocument.DocJournal.IdDocType != (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice &&
                    (SelectedDocument?.DocJournal?.Details?
                    .Exists(g => (labels?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) < g.Quantity) ?? false)))
                {
                    isSentData = System.Windows.MessageBox.Show(
                        "Некоторые коды маркированных товаров отсутствуют.\nВозможно, в списке присутствуют немаркированные товары.\nВы хотите отправить коды в Честный знак?",
                        "Внимание",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes;
                }
                else if ((SelectedDocument.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice &&
                    (SelectedDocument?.DocJournal?.DocGoodsDetailsIs?
                    .Exists(g => (labels?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) > g.Quantity) ?? false)) ||
                    (SelectedDocument.DocJournal.IdDocType != (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice &&
                    (SelectedDocument?.DocJournal?.Details?
                    .Exists(g => (labels?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) > g.Quantity) ?? false)))
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

                var crypt = new Cryptography.WinApi.WinApiCryptWrapper(_consignor.Certificate);

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

                DocComissionEdoProcessing docProcessing = null;
                System.ComponentModel.BackgroundWorker worker = new System.ComponentModel.BackgroundWorker();

                worker.DoWork += (object sender, System.ComponentModel.DoWorkEventArgs e) =>
                {
                    string employee = _abt.SelectSingleValue("select const_value from ref_const where id = 1200");
                    var document = CreateShipmentDocument(SelectedDocument.DocJournal, _consignor, SelectedOrganization, labels, SelectedDocument.DocJournal?.Code, employee);

                    Edo.GetInstance().Authenticate(false, _consignor.Certificate, _consignor.Inn);

                    loadContext.Text = "Формирование приходного УПД";
                    var generatedFile = Edo.GetInstance().GenerateTitleXml("UniversalTransferDocument",
                    "ДОП", "utd820_05_01_01_hyphen", 0, document);

                    loadContext.Text = "Подписание приходного УПД";
                    byte[] signature = crypt.Sign(generatedFile.Content, true);

                    loadContext.Text = "Отправка приходного УПД";
                    var message = Edo.GetInstance().SendXmlDocument(_consignor.OrgId, SelectedOrganization.OrgId, false, generatedFile.Content, "ДОП", signature);
                    var entity = message.Entities.FirstOrDefault(t => t.AttachmentType == Diadoc.Api.Proto.Events.AttachmentType.UniversalTransferDocument);

                    loadContext.Text = "Обработка приходного УПД";
                    Edo.GetInstance().Authenticate(false, SelectedOrganization.Certificate, SelectedOrganization.Inn);

                    var buyerDocument = CreateBuyerShipmentDocument(SelectedOrganization);
                    var generatedBuyerFile = Edo.GetInstance().GenerateTitleXml("UniversalTransferDocument",
                    "ДОП", "utd820_05_01_01_hyphen", 1, buyerDocument, message.MessageId, entity.EntityId);

                    crypt.InitializeCertificate(SelectedOrganization.Certificate);
                    var buyerSignature = crypt.Sign(generatedBuyerFile.Content, true);
                    Edo.GetInstance().SendPatchRecipientXmlDocument(message.MessageId, (int)Diadoc.Api.Proto.DocumentType.UniversalTransferDocument, 
                        entity.EntityId, generatedBuyerFile.Content, buyerSignature);

                    loadContext.Text = "Сохранение в базе данных.";

                    var fileNameLength = generatedFile.FileName.LastIndexOf('.');

                    if (fileNameLength < 0)
                        fileNameLength = generatedFile.FileName.Length;

                    docProcessing = new DocComissionEdoProcessing
                    {
                        Id = Guid.NewGuid().ToString(),
                        MessageId = message.MessageId,
                        EntityId = entity.EntityId,
                        IdDoc = SelectedDocument.CurrentDocJournalId,
                        SenderInn = _consignor.Inn,
                        ReceiverInn = SelectedOrganization.Inn,
                        DocStatus = (int)HonestMark.DocEdoProcessingStatus.Sent,
                        UserName = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser,
                        FileName = generatedFile.FileName.Substring(0, fileNameLength),
                        DocDate = DateTime.Now,
                        DeliveryDate = SelectedDocument?.DocJournal?.DeliveryDate
                    };

                    if (!SelectedDocument.IsMarked)
                        docProcessing.DocStatus = (int)HonestMark.DocEdoProcessingStatus.Processed;

                    SelectedDocument.ProcessingStatus = docProcessing;
                    _abt.DocComissionEdoProcessings.Add(docProcessing);
                };

                worker.RunWorkerCompleted += (object sender, System.ComponentModel.RunWorkerCompletedEventArgs e) => 
                {
                    if (e.Error != null)
                        throw e.Error;

                    if (model as SendModel != null)
                        ((SendModel)model).ComissionDocument = docProcessing;

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

            if (!_authInHonestMark)
            {
                System.Windows.MessageBox.Show(
                    "Повторная обработка невозможна, так как авторизация в Честном знаке не была успешной.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (_abt.DocComissionEdoProcessings.Any(d => d.IdDoc == SelectedDocument.CurrentDocJournalId && d.DocStatus == (int)HonestMark.DocEdoProcessingStatus.Processed))
            {
                System.Windows.MessageBox.Show(
                    "Данный документ уже был успешно обработан.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (_abt.DocComissionEdoProcessings.Any(d => d.IdDoc == SelectedDocument.CurrentDocJournalId && d.DocStatus == (int)HonestMark.DocEdoProcessingStatus.Sent))
            {
                System.Windows.MessageBox.Show(
                    "Данный документ находится ещё пока на стадии обработки.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var processingComissionStatus = (DocComissionEdoProcessing)SelectedDocument?.ProcessingStatus;

            if(processingComissionStatus == null)
            {
                System.Windows.MessageBox.Show(
                   "Не найден документ для повторной обработки.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                HonestMark.HonestMarkClient.GetInstance().ReprocessDocument(processingComissionStatus.FileName);

                processingComissionStatus.DocStatus = (int)HonestMark.DocEdoProcessingStatus.Sent;

                var processingStatus = processingComissionStatus.MainDocuments.OrderByDescending(m => m.DocDate).FirstOrDefault();

                if (processingStatus != null)
                    processingStatus.IsReprocessingStatus = 1;

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

        private void AnnulmentDocument()
        {
            if (SelectedDocument == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбран документ.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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

                DocEdoProcessing docProcessing = null;
                Diadoc.Api.Proto.Documents.Document document = null;

                if(SelectedDocument.ProcessingStatus != null)
                {
                    if (SelectedDocument.IsMarked && ((DocComissionEdoProcessing)SelectedDocument.ProcessingStatus).MainDocuments.Count > 1)
                    {
                        foreach(var docProc in ((DocComissionEdoProcessing)SelectedDocument.ProcessingStatus).MainDocuments)
                        {
                            if (docProcessing != null)
                                break;

                            var doc = Edo.GetInstance().GetDocument(docProc.MessageId, docProc.EntityId);

                            if (docProc.DocStatus == (int)EdiProcessingUnit.Enums.DocEdoSendStatus.Signed && 
                                doc.LastOuterDocflows != null && doc.LastOuterDocflows.Exists(l => l.OuterDocflow?.DocflowNamedId == "TtGis" && 
                                l.OuterDocflow?.Status?.Type == Diadoc.Api.Proto.OuterStatusType.Success))
                            {
                                docProcessing = docProc;
                                SelectedDocument.EdoProcessing = docProcessing;
                                document = doc;
                            }
                        }
                    }
                }

                docProcessing = SelectedDocument.EdoProcessing as DocEdoProcessing;

                if(document == null)
                    document = Edo.GetInstance().GetDocument(docProcessing.MessageId, docProcessing.EntityId);

                if (document == null)
                    throw new Exception("Документ не найден в системе ЭДО.");

                if(document.RevocationStatus == Diadoc.Api.Proto.Documents.RevocationStatus.RequestsMyRevocation)
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
                        Edo.GetInstance().SendPatchSignedDocument(message.MessageId, entity.EntityId, signature);

                        var fileNameLength = entity.FileName.LastIndexOf('.');

                        if (fileNameLength < 0)
                            fileNameLength = entity.FileName.Length;

                        var fileName = entity.FileName.Substring(0, fileNameLength);

                        docProcessing.AnnulmentFileName = fileName;

                        if(document.RecipientResponseStatus == Diadoc.Api.Proto.Documents.RecipientResponseStatus.WithRecipientSignature && SelectedDocument.IsMarked)
                            docProcessing.AnnulmentStatus = (int)HonestMark.AnnulmentDocumentStatus.RevokedWaitProcessing;
                        else
                            docProcessing.AnnulmentStatus = (int)HonestMark.AnnulmentDocumentStatus.Revoked;

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
                            var firstMiddleName = _utils.ParseCertAttribute(SelectedOrganization.Certificate.Subject, "G");
                            string signerFirstName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
                            string signerPatronymic = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;

                            var signer = new Diadoc.Api.Proto.Invoicing.Signer
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

                            var generatedFile = Edo.GetInstance().GenerateSignatureRejectionXml(message.MessageId, entity.EntityId, reasonText, signer);

                            loadContext.Text = "Подписание и отправка.";
                            var signature = crypt.Sign(generatedFile.Content, true);
                            Edo.GetInstance().SendRejectionDocument(message.MessageId, entity.EntityId, generatedFile.Content, signature);

                            docProcessing.AnnulmentStatus = (int)HonestMark.AnnulmentDocumentStatus.Rejected;
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
                    if (docProcessing.AnnulmentStatus != (int)HonestMark.AnnulmentDocumentStatus.Requested)
                    {
                        docProcessing.AnnulmentStatus = (int)HonestMark.AnnulmentDocumentStatus.Requested;
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
                            var firstMiddleName = _utils.ParseCertAttribute(SelectedOrganization.Certificate.Subject, "G");
                            string signerFirstName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
                            string signerPatronymic = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;

                            var signer = new Diadoc.Api.Proto.Invoicing.Signer
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

                            var generatedFile = Edo.GetInstance().GenerateRevocationRequestXml(docProcessing.MessageId, docProcessing.EntityId, text, signer);

                            if (generatedFile == null)
                                throw new Exception("Файл не был сгенерирован.");

                            loadContext.Text = "Подписание и отправка.";
                            var signature = crypt.Sign(generatedFile.Content, true);
                            Edo.GetInstance().SendRevocationDocument(docProcessing.MessageId, docProcessing.EntityId, generatedFile.Content, signature);

                            loadContext.Text = "Сохранение в базе.";
                            var fileNameLength = generatedFile.FileName.LastIndexOf('.');

                            if (fileNameLength < 0)
                                fileNameLength = generatedFile.FileName.Length;

                            var fileName = generatedFile.FileName.Substring(0, fileNameLength);

                            docProcessing.AnnulmentFileName = fileName;
                            docProcessing.AnnulmentStatus = (int)HonestMark.AnnulmentDocumentStatus.Requested;
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

        private void ReturnMarkedCodesToStore(object sender, EventArgs e)
        {
            if (!_authInHonestMark)
            {
                if(System.Windows.MessageBox.Show(
                    "Авторизация в Честном знаке не была успешной.\nПоэтому невозможно проверить коды на владельцев в Честном знаке.\nВы хотите отправить обратно коды?", "Ошибка", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                return;
            }

            if (SelectedDocument.ProcessingStatus == null)
            {
                System.Windows.MessageBox.Show(
                    "Документ с данными кодами маркировки не был ранее отправлен.\n" +
                    "Поэтому по ним невозможно осуществить возврат.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var processingStatus = (DocComissionEdoProcessing)SelectedDocument.ProcessingStatus;
            var docProcessing = processingStatus.MainDocuments.OrderByDescending(m => m.DocDate).FirstOrDefault();

            if(docProcessing != null && (docProcessing.AnnulmentStatus == (int)HonestMark.AnnulmentDocumentStatus.RevokedWaitProcessing || docProcessing.AnnulmentStatus == (int)HonestMark.AnnulmentDocumentStatus.Requested))
            {
                System.Windows.MessageBox.Show(
                    "Документ с данными кодами маркировки находится в процессе аннулирования.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (processingStatus.DocStatus == (int)HonestMark.DocEdoProcessingStatus.Sent)
            {
                System.Windows.MessageBox.Show(
                    "Документ с данными кодами маркировки обрабатывается в Честном знаке.\n" +
                    "Дождитесь конца обработки и повторите попытку.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (processingStatus.DocStatus == (int)HonestMark.DocEdoProcessingStatus.ProcessingError)
            {
                System.Windows.MessageBox.Show(
                    "Документ с данными кодами маркировки был обработан с ошибками в Честном знаке.\n" +
                    "Поэтому по ним невозможно осуществить возврат.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var labels = ((MarkedCodesWindow)sender).SelectedCodes;

            if(labels.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "Не выбраны коды маркировки.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            List<DocGoodsDetailsLabels> labelsForEdoSend = new List<DocGoodsDetailsLabels>();

            if (_authInHonestMark)
            {
                var markedCodesInfo = HonestMark.HonestMarkClient.GetInstance()
                    .GetMarkCodesInfo(HonestMark.ProductGroupsEnum.None, labels.Select(l => l.DmLabel).ToArray())
                    .Where(m => m?.CisInfo?.OwnerInn != _consignor.Inn);

                if(markedCodesInfo.Any(m => m?.CisInfo?.OwnerInn != SelectedOrganization.Inn))
                {
                    System.Windows.MessageBox.Show(
                        "В списке кодов маркировки есть те, которые не принадлежат нашей организации.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                labelsForEdoSend = labels?.Where(l => markedCodesInfo.Any(m => m?.CisInfo?.RequestedCis == l.DmLabel))?.ToList() ?? new List<DocGoodsDetailsLabels>();
            }
            else
                labelsForEdoSend = labels;

            var loadContext = new LoadModel();
            var loadWindow = new LoadWindow();
            loadWindow.DataContext = loadContext;
            loadWindow.Owner = (MarkedCodesWindow)sender;

            System.ComponentModel.BackgroundWorker worker = new System.ComponentModel.BackgroundWorker();

            worker.DoWork += (object s, System.ComponentModel.DoWorkEventArgs doWorkEventArgs) =>
            {
                if (labelsForEdoSend.Count > 0)
                {
                    var numberOfReturnDocument = processingStatus.NumberOfReturnDocuments + 1;
                    string documentNumber;

                    if (numberOfReturnDocument < 10)
                        documentNumber = $"{SelectedDocument.DocJournal.Code}-0{numberOfReturnDocument}";
                    else
                        documentNumber = $"{SelectedDocument.DocJournal.Code}-{numberOfReturnDocument}";

                    string employee = _abt.SelectSingleValue("select const_value from ref_const where id = 1200");
                    var document = CreateShipmentDocument(SelectedDocument.DocJournal, SelectedOrganization, _consignor, labelsForEdoSend, documentNumber, employee, true);

                    bool result = Edo.GetInstance().Authenticate(false, SelectedOrganization.Certificate, SelectedOrganization.Inn);

                    if (!result)
                        throw new Exception("Не удалось авторизоваться в системе по сертификату.");

                    loadContext.Text = "Формирование УПД отправителя";
                    var generatedFile = Edo.GetInstance().GenerateTitleXml("UniversalTransferDocument",
                    "ДОП", "utd820_05_01_01_hyphen", 0, document);

                    loadContext.Text = "Подписание УПД";
                    var crypt = new Cryptography.WinApi.WinApiCryptWrapper(SelectedOrganization.Certificate);
                    byte[] signature = crypt.Sign(generatedFile.Content, true);

                    loadContext.Text = "Отправка УПД";
                    var message = Edo.GetInstance().SendXmlDocument(SelectedOrganization.OrgId, _consignor.OrgId, false, generatedFile.Content, "ДОП", signature);
                    var entity = message.Entities.FirstOrDefault(t => t.AttachmentType == Diadoc.Api.Proto.Events.AttachmentType.UniversalTransferDocument);

                    loadContext.Text = "Обработка УПД";
                    Edo.GetInstance().Authenticate(false, _consignor.Certificate, _consignor.Inn);

                    var buyerDocument = CreateBuyerShipmentDocument(_consignor);
                    var generatedBuyerFile = Edo.GetInstance().GenerateTitleXml("UniversalTransferDocument",
                    "ДОП", "utd820_05_01_01_hyphen", 1, buyerDocument, message.MessageId, entity.EntityId);

                    crypt.InitializeCertificate(_consignor.Certificate);
                    var buyerSignature = crypt.Sign(generatedBuyerFile.Content, true);

                    Edo.GetInstance().SendPatchRecipientXmlDocument(message.MessageId, (int)Diadoc.Api.Proto.DocumentType.UniversalTransferDocument,
                        entity.EntityId, generatedBuyerFile.Content, buyerSignature);
                    Edo.GetInstance().Authenticate(false, SelectedOrganization.Certificate, SelectedOrganization.Inn);

                    processingStatus.NumberOfReturnDocuments++;
                }

                loadContext.Text = "Сохранение в базе";
                foreach (var label in labels)
                {
                    label.IdDocSale = null;
                    label.SaleDmLabel = null;
                    label.SaleDateTime = null;
                }

                _abt.SaveChanges();
            };

            worker.RunWorkerCompleted += (object s, System.ComponentModel.RunWorkerCompletedEventArgs runWorkerCompletedEventArgs) =>
            {
                foreach (var label in labels)
                    ((MarkedCodesWindow)sender).MarkedCodes.Remove(label);

                if(SelectedDocument.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
                    ((MarkedCodesWindow)sender).SetMarkedItems(_abt, SelectedDocument.DocJournal.DocGoodsDetailsIs, ((MarkedCodesWindow)sender).MarkedCodes);
                else
                    ((MarkedCodesWindow)sender).SetMarkedItems(_abt, SelectedDocument.DocJournal.Details, ((MarkedCodesWindow)sender).MarkedCodes);

                if (runWorkerCompletedEventArgs.Error != null)
                {
                    loadWindow.Close();

                    var errorWindow = new ErrorWindow(
                        "Произошла ошибка.",
                        new List<string>(
                            new string[]
                            {
                                    runWorkerCompletedEventArgs.Error.Message,
                                    runWorkerCompletedEventArgs.Error.StackTrace
                            }
                            ));

                    errorWindow.ShowDialog();
                    _log.Log("Exception: " + _log.GetRecursiveInnerException(runWorkerCompletedEventArgs.Error));
                }
                else
                    loadWindow.SetSuccessFullLoad(loadContext, "Возврат завершён успешно.");
            };

            worker.RunWorkerAsync();

            loadWindow.ShowDialog();
        }

        private Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphens GetUniversalDocument(
            DocJournal d, Kontragent organization, string employee = null, Diadoc.Api.Proto.Invoicing.Signers.ExtendedSignerDetails signerDetails = null)
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
                DocumentCreator = _utils.ParseCertAttribute(SelectedOrganization.Certificate.Subject, "SN") + " " + _utils.ParseCertAttribute(SelectedOrganization.Certificate.Subject, "G"),
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

            if(d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
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

            var firstMiddleName = _utils.ParseCertAttribute(SelectedOrganization.Certificate.Subject, "G");
            string signerFirstName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
            string signerMiddleName = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;

            var signer = new[]
            {
                                new Diadoc.Api.DataXml.ExtendedSignerDetails_SellerTitle
                                {
                                    FirstName = signerFirstName,
                                    MiddleName = signerMiddleName,
                                    LastName = _utils.ParseCertAttribute(SelectedOrganization.Certificate.Subject, "SN"),
                                    SignerOrganizationName = _utils.ParseCertAttribute(SelectedOrganization.Certificate.Subject, "CN"),
                                    Inn = _utils.ParseCertAttribute(SelectedOrganization.Certificate.Subject, "ИНН").TrimStart('0'),
                                    Position = _utils.ParseCertAttribute(SelectedOrganization.Certificate.Subject, "T")
                                }
                            };

            if (signer.First().Inn == organization.Inn)
                signer.First().SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.LegalEntity;
            else if (signer.First().Inn?.Length == 12)
            {
                signer.First().SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.PhysicalPerson;
                signer.First().SignerPowersBase = signer.First().Position;
            }
            else
                signer.First().SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.IndividualEntity;

            if(signerDetails != null)
            {
                signer.First().Inn = organization.Inn;
                signer.First().SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.LegalEntity;
                signer.First().SignerPowers = (Diadoc.Api.DataXml.ExtendedSignerDetails_SellerTitleSignerPowers)Convert.ToInt32(signerDetails.SignerPowers);

                if(signerDetails.SignerStatus == Diadoc.Api.Proto.Invoicing.Signers.SignerStatus.SellerEmployee)
                    signer.First().SignerStatus = Diadoc.Api.DataXml.ExtendedSignerDetailsSignerStatus.SellerEmployee;
                else if(signerDetails.SignerStatus == Diadoc.Api.Proto.Invoicing.Signers.SignerStatus.InformationCreatorEmployee)
                    signer.First().SignerStatus = Diadoc.Api.DataXml.ExtendedSignerDetailsSignerStatus.InformationCreatorEmployee;
                else if(signerDetails.SignerStatus == Diadoc.Api.Proto.Invoicing.Signers.SignerStatus.OtherOrganizationEmployee)
                    signer.First().SignerStatus = Diadoc.Api.DataXml.ExtendedSignerDetailsSignerStatus.OtherOrganizationEmployee;
                else if(signerDetails.SignerStatus == Diadoc.Api.Proto.Invoicing.Signers.SignerStatus.AuthorizedPerson)
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

                    var vat = (decimal)Math.Round(docJournalDetail.TaxSumm * docJournalDetail.Quantity, 2);
                    var subtotal = Math.Round(docJournalDetail.Quantity * ((decimal)docJournalDetail.Price - (decimal)docJournalDetail.DiscountSumm), 2);

                    var detail = new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItem
                    {
                        Product = refGood.Name,
                        Unit = Properties.Settings.Default.DefaultUnit,
                        Quantity = docJournalDetail.Quantity,
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

                    decimal idGood = docJournalDetail.IdGood, idDoc = (decimal)d.IdDocMaster;

                    var docGoodDetailLabels = _abt?.Database?
                    .SqlQuery<string>($"select DM_LABEL from doc_goods_details_labels where id_doc_sale = {idDoc} and id_good = {idGood}")?
                    .ToList() ?? new List<string>();

                    if(docGoodDetailLabels.Count > 0)
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
    }
}
