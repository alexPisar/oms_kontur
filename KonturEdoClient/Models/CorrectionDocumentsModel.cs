using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EdiProcessingUnit.Edo.Models;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using DataContextManagementUnit.DataAccess.Contexts.Abt;

namespace KonturEdoClient.Models
{
    public class CorrectionDocumentsModel : Base.ModelBase
    {
        private AbtDbContext _abt;
        private UtilitesLibrary.Logger.UtilityLog _log = UtilitesLibrary.Logger.UtilityLog.GetInstance();
        private Kontragent _currentOrganization;

        public System.Windows.Window Owner { get; set; }

        public ObservableCollection<UniversalCorrectionDocument> Documents { get; set; }
        public UniversalCorrectionDocument SelectedDocument { get; set; }

        public List<DocGoodsDetail> Details => SelectedDocument?.CorrectionDocJournal?.Details;
        public List<DocGoodsDetailsI> DocGoodsDetailsIs => SelectedDocument?.CorrectionDocJournal?.DocGoodsDetailsIs;
        public IEnumerable<object> CorrectionDocumentDetails
        {
            get {
                if (SelectedDocument?.CorrectionDocJournal?.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.ReturnFromBuyer)
                {
                    return Details;
                }
                else if (SelectedDocument?.CorrectionDocJournal?.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Correction)
                {
                    return DocGoodsDetailsIs;
                }
                else
                    return new List<object>();
            }
        }

        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }

        public string SearchDocumentNumber { get; set; }
        public string SearchInvoiceNumber { get; set; }

        public List<KeyValuePair<DataContextManagementUnit.DataAccess.DocJournalType, string>> DocTypes { get; set; }
        public DataContextManagementUnit.DataAccess.DocJournalType SelectedDocType { get; set; }

        public string SellerGridColumnName { get; set; }

        public string BuyerGridColumnName { get; set; }

        public CorrectionDocumentsModel(AbtDbContext abt, Kontragent currentOrganization)
        {
            _abt = abt;

            if (currentOrganization?.Certificate == null)
                throw new Exception("Не задан сертификат для организации.");

            _currentOrganization = currentOrganization;
            DocTypes = SetDocTypes();
            SelectedDocType = DataContextManagementUnit.DataAccess.DocJournalType.ReturnFromBuyer;
        }

        public List<KeyValuePair<DataContextManagementUnit.DataAccess.DocJournalType, string>> SetDocTypes()
        {
            var docTypes = new List<KeyValuePair<DataContextManagementUnit.DataAccess.DocJournalType, string>>();
            docTypes.Add(new KeyValuePair<DataContextManagementUnit.DataAccess.DocJournalType, string>(DataContextManagementUnit.DataAccess.DocJournalType.ReturnFromBuyer, "Возврат от покупателя"));
            docTypes.Add(new KeyValuePair<DataContextManagementUnit.DataAccess.DocJournalType, string>(DataContextManagementUnit.DataAccess.DocJournalType.Correction, "Корректировочная с/ф"));

            return docTypes;
        }

        public async Task Refresh()
        {
            var loadContext = new LoadModel();
            var loadWindow = new LoadWindow();
            loadWindow.DataContext = loadContext;

            if (Owner != null)
                loadWindow.Owner = Owner;

            loadWindow.Show();
            //loadWindow.Activate();

            Exception exception = null;

            await Task.Run(() =>
            {
                try
                {
                    var updDocType = (int)EdiProcessingUnit.Enums.DocEdoType.Upd;
                    IEnumerable<UniversalCorrectionDocument> docsCollection = null;

                    if (SelectedDocType == DataContextManagementUnit.DataAccess.DocJournalType.ReturnFromBuyer)
                    {
                        docsCollection = (from correctionDocJournal in _abt.DocJournals
                                          where correctionDocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.ReturnFromBuyer && correctionDocJournal.CreateInvoice == 1
                                          && correctionDocJournal.DocDatetime >= DateFrom && correctionDocJournal.DocDatetime < DateTo
                                          && _abt.DocEdoProcessings.Any(d => d.IdDoc == correctionDocJournal.IdDocMaster && d.DocType == updDocType &&
                                          d.DocStatus != (int)EdiProcessingUnit.Enums.DocEdoSendStatus.Rejected)
                                          join invoice in _abt.DocJournals on correctionDocJournal.IdDocMaster equals invoice.IdDocMaster
                                          where invoice.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice
                                          join docGoods in _abt.DocGoods on invoice.IdDocMaster equals docGoods.IdDoc
                                          join customer in _abt.RefCustomers on docGoods.IdSeller equals customer.IdContractor
                                          where customer.Inn == _currentOrganization.Inn && customer.Kpp == _currentOrganization.Kpp
                                          let docJournalTags = (from docJournalTag in _abt.DocJournalTags
                                                                where docJournalTag.IdTad == 109 && docJournalTag.IdDoc == correctionDocJournal.Id
                                                                select docJournalTag)
                                          let updNumber = invoice.Code
                                          select new UniversalCorrectionDocument
                                          {
                                              CorrectionDocJournal = correctionDocJournal,
                                              DocumentNumber = updNumber + "-КОР",
                                              InvoiceDocJournal = invoice,
                                              DocJournalTag = docJournalTags
                                          });
                        SellerGridColumnName = "Принимает";
                        BuyerGridColumnName = "Возвращает";
                    }
                    else if (SelectedDocType == DataContextManagementUnit.DataAccess.DocJournalType.Correction)
                    {
                        docsCollection = (from correctionDocJournal in _abt.DocJournals
                                          where correctionDocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Correction
                                          && correctionDocJournal.DocDatetime >= DateFrom && correctionDocJournal.DocDatetime < DateTo
                                          join invoice in _abt.DocJournals on correctionDocJournal.IdDocMaster equals invoice.Id
                                          where invoice.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice
                                          && _abt.DocEdoProcessings.Any(d => d.IdDoc == invoice.IdDocMaster && d.DocType == updDocType &&
                                          d.DocStatus != (int)EdiProcessingUnit.Enums.DocEdoSendStatus.Rejected)
                                          join docGoods in _abt.DocGoods on invoice.IdDocMaster equals docGoods.IdDoc
                                          join customer in _abt.RefCustomers on docGoods.IdSeller equals customer.IdContractor
                                          where customer.Inn == _currentOrganization.Inn && customer.Kpp == _currentOrganization.Kpp
                                          let docJournalTags = (from docJournalTag in _abt.DocJournalTags
                                                                where docJournalTag.IdTad == 109 && docJournalTag.IdDoc == correctionDocJournal.Id
                                                                select docJournalTag)
                                          let updNumber = invoice.Code
                                          select new UniversalCorrectionDocument
                                          {
                                              CorrectionDocJournal = correctionDocJournal,
                                              DocumentNumber = updNumber + "-КОР",
                                              InvoiceDocJournal = invoice,
                                              DocJournalTag = docJournalTags
                                          });
                        SellerGridColumnName = "Продавец";
                        BuyerGridColumnName = "Покупатель";
                    }

                    if (docsCollection == null)
                        docsCollection = new List<UniversalCorrectionDocument>();

                    if (!string.IsNullOrEmpty(SearchDocumentNumber))
                        docsCollection = docsCollection.Where(d => d.CorrectionDocJournal.Code.Contains(SearchDocumentNumber));

                    if (!string.IsNullOrEmpty(SearchInvoiceNumber))
                        docsCollection = docsCollection.Where(d => d.InvoiceDocJournal.Code.Contains(SearchInvoiceNumber));

                    docsCollection = from d in docsCollection.AsParallel() select d.Init(_abt);//.ToList();
                    Documents = new ObservableCollection<UniversalCorrectionDocument>(docsCollection);
                    OnPropertyChanged("Documents");
                    OnPropertyChanged("SellerGridColumnName");
                    OnPropertyChanged("BuyerGridColumnName");
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            loadWindow.Close();

            if (exception != null)
                throw exception;
        }

        public async void SendDocument()
        {
            if (SelectedDocument == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбран корректировочный документ.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if(SelectedDocument?.CorrectionDocJournal?.IdDocMaster == null)
            {
                System.Windows.MessageBox.Show(
                    "Не найден корректировочный документ.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            DocEdoProcessing baseProcessing = null;
            RefContractor sellerContractor = null;

            if (SelectedDocument.CorrectionDocJournal?.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.ReturnFromBuyer)
            {
                sellerContractor = SelectedDocument.CorrectionDocJournal?.DocGoods?.Customer;

                var baseProcessings = from pr in _abt.DocEdoProcessings
                                      where pr.IdDoc == SelectedDocument.CorrectionDocJournal.IdDocMaster && pr.DocType == (int)EdiProcessingUnit.Enums.DocEdoType.Upd
                                      && pr.AnnulmentStatus != (int)EdiProcessingUnit.HonestMark.AnnulmentDocumentStatus.RevokedWaitProcessing
                                      && pr.AnnulmentStatus != (int)EdiProcessingUnit.HonestMark.AnnulmentDocumentStatus.RevokedAndProcessed
                                      && pr.AnnulmentStatus != (int)EdiProcessingUnit.HonestMark.AnnulmentDocumentStatus.Revoked
                                      select pr;
                baseProcessing = baseProcessings.FirstOrDefault(d => 
                d.DocStatus == (int)EdiProcessingUnit.Enums.DocEdoSendStatus.Signed || d.DocStatus == (int)EdiProcessingUnit.Enums.DocEdoSendStatus.PartialSigned);

                if(baseProcessing == null)
                    baseProcessing = baseProcessings.FirstOrDefault();
            }
            else if (SelectedDocument.CorrectionDocJournal?.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Correction)
            {
                sellerContractor = SelectedDocument.InvoiceDocJournal?.DocMaster?.DocGoods?.Seller;

                var baseProcessings = from pr in _abt.DocEdoProcessings
                                      where pr.IdDoc == SelectedDocument.InvoiceDocJournal.IdDocMaster && pr.DocType == (int)EdiProcessingUnit.Enums.DocEdoType.Upd
                                      && pr.AnnulmentStatus != (int)EdiProcessingUnit.HonestMark.AnnulmentDocumentStatus.RevokedWaitProcessing
                                      && pr.AnnulmentStatus != (int)EdiProcessingUnit.HonestMark.AnnulmentDocumentStatus.RevokedAndProcessed
                                      && pr.AnnulmentStatus != (int)EdiProcessingUnit.HonestMark.AnnulmentDocumentStatus.Revoked
                                      select pr;
                baseProcessing = baseProcessings.FirstOrDefault(d =>
                d.DocStatus == (int)EdiProcessingUnit.Enums.DocEdoSendStatus.Signed || d.DocStatus == (int)EdiProcessingUnit.Enums.DocEdoSendStatus.PartialSigned);

                if(baseProcessing == null)
                    baseProcessing = baseProcessings.FirstOrDefault();
            }

            if (sellerContractor?.DefaultCustomer == null)
            {
                System.Windows.MessageBox.Show(
                    "Не задана продающая организация в базе.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (baseProcessing == null)
                throw new Exception("Не найден базовый документ в системе.");

            RefContractor buyerContractor = _abt.DocGoods.FirstOrDefault(g => g.IdDoc == baseProcessing.IdDoc)?.Customer;

            RefCustomer buyerCustomer = null;
            if(buyerContractor?.DefaultCustomer != null)
                buyerCustomer = _abt.RefCustomers.FirstOrDefault(r => r.Id == buyerContractor.DefaultCustomer);

            _log.Log($"CorrectionDocumentsModel : начало отправки в методе SendDocument");

            var loadContext = new LoadModel();
            var loadWindow = new LoadWindow();
            loadWindow.DataContext = loadContext;

            if(Owner != null)
                loadWindow.Owner = Owner;

            loadWindow.Show();
            Exception exception = null;

            await Task.Run(() =>
            {
                try
                {
                    loadContext.Text = "Формирование документа.";
                    var utils = new Utils.XmlSignUtils();
                    var sellerCustomer = _abt.RefCustomers.FirstOrDefault(r => r.Id == sellerContractor.DefaultCustomer);

                    var typeNamedId = "UniversalCorrectionDocument";
                    var function = "КСЧФДИС";
                    var version = "ucd736_05_01_02";

                    var correctionDocument = new Diadoc.Api.DataXml.Ucd736.UniversalCorrectionDocument
                    {
                        Currency = Properties.Settings.Default.DefaultCurrency,
                        Function = Diadoc.Api.DataXml.Ucd736.UniversalCorrectionDocumentFunction.КСЧФДИС,
                        DocumentNumber = SelectedDocument.DocumentNumber,
                        DocumentDate = SelectedDocument?.CorrectionDocJournal?.DocDatetime.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy")
                    };

                    correctionDocument.Seller = new Diadoc.Api.DataXml.Ucd736.ExtendedOrganizationInfo_ForeignAddress1000();

                    if (!string.IsNullOrEmpty(EdiProcessingUnit.Edo.Edo.GetInstance().ActualBoxId))
                    {
                        (correctionDocument.Seller as Diadoc.Api.DataXml.Ucd736.ExtendedOrganizationInfo_ForeignAddress1000).Item =
                        new Diadoc.Api.DataXml.Ucd736.ExtendedOrganizationReference
                        {
                            BoxId = EdiProcessingUnit.Edo.Edo.GetInstance().ActualBoxId,
                            ShortOrgName = sellerCustomer.Name,
                            OrgType = sellerCustomer.Inn.Length == 12 ? Diadoc.Api.DataXml.OrganizationType.IndividualEntity : Diadoc.Api.DataXml.OrganizationType.LegalEntity
                        };
                    }
                    else
                    {
                        (correctionDocument.Seller as Diadoc.Api.DataXml.Ucd736.ExtendedOrganizationInfo_ForeignAddress1000).Item = 
                        new Diadoc.Api.DataXml.Ucd736.ExtendedOrganizationDetails_ForeignAddress1000
                        {
                            Inn = sellerCustomer.Inn,
                            Kpp = sellerCustomer.Kpp,
                            OrgName = sellerCustomer.Name,
                            OrgType = sellerCustomer.Inn.Length == 12 ? Diadoc.Api.DataXml.OrganizationType.IndividualEntity : Diadoc.Api.DataXml.OrganizationType.LegalEntity,
                            Address = new Diadoc.Api.DataXml.Ucd736.Address_ForeignAddress1000
                            {
                                Item = new Diadoc.Api.DataXml.Ucd736.ForeignAddress1000
                                {
                                    Country = Properties.Settings.Default.DefaultOrgCountryCode,
                                    Address = sellerContractor.Address
                                }
                            }
                        };
                    }

                    var orgInn = _currentOrganization.Inn;

                    var baseDocument = EdiProcessingUnit.Edo.Edo.GetInstance().GetDocument(baseProcessing.MessageId, baseProcessing.EntityId);

                    var counteragentBox = baseDocument.CounteragentBoxId;

                    if(buyerContractor?.DefaultCustomer != null)
                    {
                        var counteragent = EdiProcessingUnit.Edo.Edo.GetInstance().GetKontragents(_currentOrganization.OrgId)?
                        .FirstOrDefault(c => c?.Organization?.Inn == buyerCustomer.Inn);

                        correctionDocument.Buyer = new Diadoc.Api.DataXml.Ucd736.ExtendedOrganizationInfo_ForeignAddress1000
                        {
                            Item = new Diadoc.Api.DataXml.Ucd736.ExtendedOrganizationDetails_ForeignAddress1000
                            {
                                OrgName = buyerCustomer.Name,
                                OrgType = buyerCustomer.Inn.Length == 12 ? Diadoc.Api.DataXml.OrganizationType.IndividualEntity : Diadoc.Api.DataXml.OrganizationType.LegalEntity,
                                Inn = buyerCustomer.Inn,
                                Kpp = buyerCustomer.Kpp,
                                Address = new Diadoc.Api.DataXml.Ucd736.Address_ForeignAddress1000
                                {
                                    Item = new Diadoc.Api.DataXml.Ucd736.ForeignAddress1000
                                    {
                                        Country = Properties.Settings.Default.DefaultOrgCountryCode,
                                        Address = buyerCustomer.Address
                                    }
                                }
                            }
                        };

                        if (!string.IsNullOrEmpty(counteragent?.Organization?.FnsParticipantId))
                            (correctionDocument.Buyer.Item as Diadoc.Api.DataXml.Ucd736.ExtendedOrganizationDetails_ForeignAddress1000).FnsParticipantId = counteragent.Organization.FnsParticipantId;

                        var contractNumber = _abt.RefRefTags.FirstOrDefault(c => c.IdTag == 200 && c.IdObject == buyerCustomer.Id)?.TagValue;
                        var contractDate = _abt.RefRefTags.FirstOrDefault(c => c.IdTag == 199 && c.IdObject == buyerCustomer.Id)?.TagValue;

                        if (!(string.IsNullOrEmpty(contractNumber) || string.IsNullOrEmpty(contractDate)))
                        {
                            correctionDocument.EventContent = new Diadoc.Api.DataXml.Ucd736.EventContent()
                            {
                                OperationContent = "Изменение стоимости товаров и услуг",
                                TransferDocDetails = new Diadoc.Api.DataXml.Ucd736.DocType[]
                                {
                                    new Diadoc.Api.DataXml.Ucd736.DocType
                                    {
                                        BaseDocumentName = "УПД",
                                        BaseDocumentNumber = SelectedDocument?.InvoiceDocJournal?.Code,
                                        BaseDocumentDate = SelectedDocument?.InvoiceDocJournal?.DeliveryDate?.Date.ToString("dd.MM.yyyy")
                                    }
                                },
                                CorrectionBase = new Diadoc.Api.DataXml.Ucd736.DocType[]
                                {
                                    new Diadoc.Api.DataXml.Ucd736.DocType
                                    {
                                        BaseDocumentName = "Договор поставки",
                                        BaseDocumentNumber = contractNumber,
                                        BaseDocumentDate = contractDate
                                    }
                                }
                            };
                        }
                    }
                    else
                        correctionDocument.Buyer = new Diadoc.Api.DataXml.Ucd736.ExtendedOrganizationInfo_ForeignAddress1000
                        {
                            Item = new Diadoc.Api.DataXml.Ucd736.ExtendedOrganizationReference
                            {
                                BoxId = counteragentBox,
                                ShortOrgName = baseProcessing.ReceiverName,
                                OrgType = baseProcessing.ReceiverInn.Length == 12 ? Diadoc.Api.DataXml.OrganizationType.IndividualEntity : Diadoc.Api.DataXml.OrganizationType.LegalEntity
                            }
                        };

                    Diadoc.Api.DataXml.ExtendedSignerDetails_CorrectionSellerTitle[] signer;
                    if (string.IsNullOrEmpty(_currentOrganization.EmchdId))
                    {
                        var firstMiddleName = utils.ParseCertAttribute(_currentOrganization.Certificate.Subject, "G");
                        string signerFirstName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
                        string signerMiddleName = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;
                        string signerLastName = utils.ParseCertAttribute(_currentOrganization.Certificate.Subject, "SN");

                        signer = new[]
                        {
                    new Diadoc.Api.DataXml.ExtendedSignerDetails_CorrectionSellerTitle
                    {
                        SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.LegalEntity,
                        FirstName = signerFirstName,
                        MiddleName = signerMiddleName,
                        LastName = signerLastName,
                        SignerOrganizationName = utils.ParseCertAttribute(_currentOrganization.Certificate.Subject, "CN"),
                        Inn = orgInn,
                        Position = utils.ParseCertAttribute(_currentOrganization.Certificate.Subject, "T")
                    }
                };
                    }
                    else
                    {
                        signer = new[]
                        {
                            new Diadoc.Api.DataXml.ExtendedSignerDetails_CorrectionSellerTitle
                            {
                                SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.LegalEntity,
                                FirstName = _currentOrganization.EmchdPersonName,
                                MiddleName = _currentOrganization.EmchdPersonPatronymicSurname,
                                LastName = _currentOrganization.EmchdPersonSurname,
                                SignerOrganizationName = _currentOrganization.Name,
                                Inn = orgInn,
                                Position = _currentOrganization.EmchdPersonPosition
                            }
                        };
                    }

                    correctionDocument.UseSignerDetails(signer);

                    correctionDocument.DocumentCreator = $"{signer.First().LastName} {signer.First().FirstName} {signer.First().MiddleName}";

                    var idChannel = SelectedDocument?.InvoiceDocJournal?.DocMaster?.DocGoods?.Customer?.IdChannel;

                    RefEdoGoodChannel refEdoGoodChannel = null;
                    if (idChannel != null && idChannel != 99001)
                        refEdoGoodChannel = (from r in _abt.RefContractors
                                             where r.IdChannel == idChannel
                                             join s in (from c in _abt.RefContractors
                                                        where c.DefaultCustomer != null
                                                        join refEdo in _abt.RefEdoGoodChannels on c.IdChannel equals (refEdo.IdChannel)
                                                        select new { RefEdoGoodChannel = refEdo, RefContractor = c })
                                                        on r.DefaultCustomer equals (s.RefContractor.DefaultCustomer)
                                             where s != null
                                             select s.RefEdoGoodChannel).FirstOrDefault();

                    var itemDetails = new List<Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItem>();

                    if (SelectedDocument.CorrectionDocJournal?.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.ReturnFromBuyer)
                    {
                        foreach (var detail in Details)
                        {
                            var baseDetail = SelectedDocument.InvoiceDocJournal.DocGoodsDetailsIs.FirstOrDefault(d => d.IdGood == detail.IdGood);

                            if (baseDetail == null)
                                throw new Exception($"Не найден товар в исходном документе {detail.Good.Name}, ID {detail.IdGood}");

                            var additionalInfos = new List<Diadoc.Api.DataXml.AdditionalInfo>();

                            int baseIndex = SelectedDocument.InvoiceDocJournal.DocGoodsDetailsIs.IndexOf(baseDetail) + 1;

                            var barCode = _abt.RefBarCodes?
                            .FirstOrDefault(b => b.IdGood == detail.IdGood && b.IsPrimary == false)?
                            .BarCode;

                            var oldSubtotal = Math.Round(baseDetail.Quantity * ((decimal)baseDetail.Price - (decimal)baseDetail.DiscountSumm), 2);
                            var oldVat = (decimal)Math.Round(oldSubtotal * baseDetail.TaxRate / (baseDetail.TaxRate + 100), 2);
                            var oldPrice = (decimal)Math.Round((oldSubtotal - oldVat) / baseDetail.Quantity, 2, MidpointRounding.AwayFromZero);

                            var newSubtotal = Math.Round(detail.Quantity * ((decimal)detail.Price - (decimal)detail.DiscountSumm), 2);
                            var newVat = (decimal)Math.Round(newSubtotal * baseDetail.TaxRate / (baseDetail.TaxRate + 100), 2);
                            var newPrice = (decimal)Math.Round((newSubtotal - newVat) / detail.Quantity, 2, MidpointRounding.AwayFromZero);

                            var item = new Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItem
                            {
                                Product = detail.Good.Name,
                                ItemVendorCode = barCode,
                                OriginalNumber = baseIndex.ToString(),
                                Unit = new Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemUnit
                                {
                                    OriginalValue = Properties.Settings.Default.DefaultUnit,
                                    CorrectedValue = Properties.Settings.Default.DefaultUnit
                                },
                                UnitName = new Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemUnitName
                                {
                                    OriginalValue = "шт.",
                                    CorrectedValue = "шт."
                                },
                                Quantity = new Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemQuantity
                                {
                                    OriginalValue = baseDetail.Quantity,
                                    OriginalValueSpecified = true,
                                    CorrectedValue = baseDetail.Quantity - detail.Quantity,
                                    CorrectedValueSpecified = true
                                },
                                TaxRate = new Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemTaxRate
                                {
                                    OriginalValue = Diadoc.Api.DataXml.TaxRateWithTwentyPercentAndTaxedByAgent.TwentyPercent,
                                    CorrectedValue = Diadoc.Api.DataXml.TaxRateWithTwentyPercentAndTaxedByAgent.TwentyPercent
                                },
                                Price = new Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemPrice
                                {
                                    OriginalValue = oldPrice,
                                    OriginalValueSpecified = true,
                                    CorrectedValue = newPrice,
                                    CorrectedValueSpecified = true
                                },
                                Vat = new Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemVat
                                {
                                    OriginalValue = oldVat,
                                    OriginalValueSpecified = true,
                                    CorrectedValue = oldVat - newVat,
                                    CorrectedValueSpecified = true,
                                    ItemElementName = Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemVatDiffType.AmountsDec,
                                    Item = newVat
                                },
                                Subtotal = new Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemSubtotal
                                {
                                    OriginalValue = oldSubtotal,
                                    OriginalValueSpecified = true,
                                    CorrectedValue = oldSubtotal - newSubtotal,
                                    CorrectedValueSpecified = true,
                                    ItemElementName = Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemSubtotalDiffType.AmountsDec,
                                    Item = newSubtotal
                                },
                                SubtotalWithVatExcluded = new Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemSubtotalWithVatExcluded
                                {
                                    OriginalValue = oldSubtotal - oldVat,
                                    OriginalValueSpecified = true,
                                    CorrectedValue = oldSubtotal - oldVat - newSubtotal + newVat,
                                    CorrectedValueSpecified = true,
                                    Item = newSubtotal - newVat,
                                    ItemElementName = Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemSubtotalWithVatExcludedDiffType.AmountsDec
                                }
                            };

                            var country = _abt.RefCountries.FirstOrDefault(c => c.Id == detail.Good.IdCountry);

                            if (country != null)
                            {
                                string countryCode = country.NumCode?.ToString();

                                if (countryCode.Length == 1)
                                    countryCode = "00" + countryCode;
                                else if (countryCode.Length == 2)
                                    countryCode = "0" + countryCode;

                                additionalInfos.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = "цифровой код страны происхождения", Value = countryCode });
                                additionalInfos.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = "краткое наименование страны происхождения", Value = country.Name });

                                if(!string.IsNullOrEmpty(detail?.Good?.CustomsNo))
                                    additionalInfos.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = "регистрационный номер декларации на товары", Value = detail.Good.CustomsNo });
                            }

                            if (refEdoGoodChannel != null)
                            {
                                if (!string.IsNullOrEmpty(refEdoGoodChannel.DetailBuyerCodeUpdId))
                                {
                                    var edoGoodChannelId = refEdoGoodChannel.IdChannel;
                                    var goodMatching = _abt.RefGoodMatchings.FirstOrDefault(r => r.IdChannel == edoGoodChannelId && r.IdGood == detail.IdGood && r.Disabled == 0);

                                    if(goodMatching == null && SelectedDocument?.InvoiceDocJournal?.DocMaster != null)
                                    {
                                        var docDateTime = SelectedDocument.InvoiceDocJournal.DocMaster.DocDatetime.Date;

                                        goodMatching = _abt.RefGoodMatchings.FirstOrDefault(r => r.DisabledDatetime != null && r.IdChannel == edoGoodChannelId &&
                                        r.IdGood == detail.IdGood && r.Disabled == 1 && r.DisabledDatetime.Value >= docDateTime);
                                    }

                                    if (goodMatching == null)
                                        throw new Exception("Не все товары сопоставлены с кодами покупателя.");

                                    if (!string.IsNullOrEmpty(goodMatching?.CustomerArticle))
                                        additionalInfos.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = refEdoGoodChannel.DetailBuyerCodeUpdId, Value = goodMatching.CustomerArticle });
                                    else
                                        throw new Exception("Не для всех товаров заданы коды покупателя.");
                                }

                                if (!string.IsNullOrEmpty(refEdoGoodChannel.DetailBarCodeUpdId))
                                    additionalInfos.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = refEdoGoodChannel.DetailBarCodeUpdId, Value = barCode });

                                if (!string.IsNullOrEmpty(refEdoGoodChannel.DetailPositionUpdId))
                                    additionalInfos.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = refEdoGoodChannel.DetailPositionUpdId, Value = item.OriginalNumber });
                            }

                            item.AdditionalInfos = additionalInfos.ToArray();

                            if (SelectedDocument.IsMarked)
                            {
                                var originalMarkedCodes = (from label in _abt.DocGoodsDetailsLabels
                                                           where label.IdDocSale == SelectedDocument.InvoiceDocJournal.IdDocMaster && label.IdGood == detail.IdGood
                                                           select label).ToArray();
                                var correctedMarkedCodes = originalMarkedCodes.Where(label => label.IdDocReturn == null).ToArray();

                                if (originalMarkedCodes.Length > 0)
                                {
                                    item.OriginalItemIdentificationNumbers = new Diadoc.Api.DataXml.Ucd736.ItemIdentificationNumbersItemIdentificationNumber[1];
                                    item.OriginalItemIdentificationNumbers[0] = new Diadoc.Api.DataXml.Ucd736.ItemIdentificationNumbersItemIdentificationNumber
                                    {
                                        ItemsElementName = new Diadoc.Api.DataXml.ItemsChoiceType[originalMarkedCodes.Length],
                                        Items = new string[originalMarkedCodes.Length]
                                    };

                                    int j = 0;
                                    foreach(var docGoodsDetailLabel in originalMarkedCodes)
                                    {
                                        item.OriginalItemIdentificationNumbers[0].ItemsElementName[j] = Diadoc.Api.DataXml.ItemsChoiceType.Unit;
                                        item.OriginalItemIdentificationNumbers[0].Items[j] = docGoodsDetailLabel.DmLabel;
                                        j++;
                                    }
                                }

                                if (correctedMarkedCodes.Length > 0)
                                {
                                    if (correctedMarkedCodes.Length != (int)item.Quantity.CorrectedValue)
                                        throw new Exception($"Количество кодов маркировки не совпадает с количеством товара в документе. ID товара {detail.IdGood}");

                                    item.CorrectedItemIdentificationNumbers = new Diadoc.Api.DataXml.Ucd736.ItemIdentificationNumbersItemIdentificationNumber[1];
                                    item.CorrectedItemIdentificationNumbers[0] = new Diadoc.Api.DataXml.Ucd736.ItemIdentificationNumbersItemIdentificationNumber
                                    {
                                        ItemsElementName = new Diadoc.Api.DataXml.ItemsChoiceType[correctedMarkedCodes.Length],
                                        Items = new string[correctedMarkedCodes.Length]
                                    };

                                    int j = 0;
                                    foreach (var docGoodsDetailLabel in correctedMarkedCodes)
                                    {
                                        item.CorrectedItemIdentificationNumbers[0].ItemsElementName[j] = Diadoc.Api.DataXml.ItemsChoiceType.Unit;
                                        item.CorrectedItemIdentificationNumbers[0].Items[j] = docGoodsDetailLabel.DmLabel;
                                        j++;
                                    }
                                }
                                else
                                {
                                    if (item.Quantity.CorrectedValue > 0)
                                        if (_abt.RefItems.Any(r => r.IdName == 30071 && r.IdGood == detail.IdGood && r.Quantity == 1))
                                            throw new Exception($"Товар из ЧЗ {detail.IdGood} забыли пропикать!!");
                                }
                            }
                            else
                            {
                                if (item.Quantity.CorrectedValue > 0)
                                    if (_abt.RefItems.Any(r => r.IdName == 30071 && r.IdGood == detail.IdGood && r.Quantity == 1))
                                        throw new Exception($"Товар из ЧЗ {detail.IdGood} забыли пропикать!!");
                            }

                            itemDetails.Add(item);
                        }

                        correctionDocument.Table = new Diadoc.Api.DataXml.Ucd736.InvoiceCorrectionTable
                        {
                            TotalsDec = new Diadoc.Api.DataXml.Ucd736.InvoiceTotalsDiff736
                            {
                                Total = itemDetails.Sum(d => d.Subtotal.OriginalValue) - itemDetails.Sum(d => d.Subtotal.CorrectedValue),
                                TotalSpecified = true,
                                Vat = itemDetails.Sum(d => d.Vat.OriginalValue) - itemDetails.Sum(d => d.Vat.CorrectedValue),
                                VatSpecified = true,
                                TotalWithVatExcluded = itemDetails.Sum(d => d.SubtotalWithVatExcluded.OriginalValue) - itemDetails.Sum(d => d.SubtotalWithVatExcluded.CorrectedValue)
                            },

                            TotalsInc = new Diadoc.Api.DataXml.Ucd736.InvoiceTotalsDiff736
                            {
                                Total = 0,
                                TotalSpecified = true,
                                Vat = 0,
                                VatSpecified = true,
                                TotalWithVatExcluded = 0
                            },
                            Items = itemDetails.ToArray()
                        };
                    }
                    else if(SelectedDocument.CorrectionDocJournal?.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Correction)
                    {
                        foreach(var detail in DocGoodsDetailsIs)
                        {
                            var baseDetail = SelectedDocument.InvoiceDocJournal.DocGoodsDetailsIs.FirstOrDefault(d => d.IdGood == detail.IdGood);

                            if (baseDetail == null)
                                throw new Exception($"Не найден товар в исходном документе {detail.Good.Name}, ID {detail.IdGood}");

                            var additionalInfos = new List<Diadoc.Api.DataXml.AdditionalInfo>();

                            int baseIndex = SelectedDocument.InvoiceDocJournal.DocGoodsDetailsIs.IndexOf(baseDetail) + 1;

                            var barCode = _abt.RefBarCodes?
                            .FirstOrDefault(b => b.IdGood == detail.IdGood && b.IsPrimary == false)?
                            .BarCode;

                            var oldSubtotal = Math.Round(baseDetail.Quantity * ((decimal)baseDetail.Price - (decimal)baseDetail.DiscountSumm), 2);
                            var oldVat = (decimal)Math.Round(oldSubtotal * baseDetail.TaxRate / (baseDetail.TaxRate + 100), 2);
                            var oldPrice = (decimal)Math.Round((oldSubtotal - oldVat) / baseDetail.Quantity, 2, MidpointRounding.AwayFromZero);

                            var newSubtotal = Math.Round(detail.Quantity * ((decimal)detail.Price - (decimal)detail.DiscountSumm), 2);
                            var newVat = (decimal)Math.Round(newSubtotal * baseDetail.TaxRate / (baseDetail.TaxRate + 100), 2);
                            var newPrice = (decimal)Math.Round((newSubtotal - newVat) / detail.Quantity, 2, MidpointRounding.AwayFromZero);

                            var item = new Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItem
                            {
                                Product = detail.Good.Name,
                                ItemVendorCode = barCode,
                                OriginalNumber = baseIndex.ToString(),
                                Unit = new Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemUnit
                                {
                                    OriginalValue = Properties.Settings.Default.DefaultUnit,
                                    CorrectedValue = Properties.Settings.Default.DefaultUnit
                                },
                                UnitName = new Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemUnitName
                                {
                                    OriginalValue = "шт.",
                                    CorrectedValue = "шт."
                                },
                                Quantity = new Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemQuantity
                                {
                                    OriginalValue = baseDetail.Quantity,
                                    OriginalValueSpecified = true,
                                    CorrectedValue = detail.Quantity,
                                    CorrectedValueSpecified = true
                                },
                                TaxRate = new Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemTaxRate
                                {
                                    OriginalValue = Diadoc.Api.DataXml.TaxRateWithTwentyPercentAndTaxedByAgent.TwentyPercent,
                                    CorrectedValue = Diadoc.Api.DataXml.TaxRateWithTwentyPercentAndTaxedByAgent.TwentyPercent
                                },
                                Price = new Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemPrice
                                {
                                    OriginalValue = oldPrice,
                                    OriginalValueSpecified = true,
                                    CorrectedValue = newPrice,
                                    CorrectedValueSpecified = true
                                },
                                Vat = new Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemVat
                                {
                                    OriginalValue = oldVat,
                                    OriginalValueSpecified = true,
                                    CorrectedValue = newVat,
                                    CorrectedValueSpecified = true,
                                    ItemElementName = oldVat > newVat ? Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemVatDiffType.AmountsDec : Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemVatDiffType.AmountsInc,
                                    Item = Math.Abs(newVat - oldVat)
                                },
                                Subtotal = new Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemSubtotal
                                {
                                    OriginalValue = oldSubtotal,
                                    OriginalValueSpecified = true,
                                    CorrectedValue = newSubtotal,
                                    CorrectedValueSpecified = true,
                                    ItemElementName = oldSubtotal > newSubtotal ? Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemSubtotalDiffType.AmountsDec : Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemSubtotalDiffType.AmountsInc,
                                    Item = Math.Abs(newSubtotal - oldSubtotal)
                                },
                                SubtotalWithVatExcluded = new Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemSubtotalWithVatExcluded
                                {
                                    OriginalValue = oldSubtotal - oldVat,
                                    OriginalValueSpecified = true,
                                    CorrectedValue = newSubtotal - newVat,
                                    CorrectedValueSpecified = true,
                                    Item = Math.Abs(oldSubtotal - oldVat - newSubtotal + newVat),
                                    ItemElementName = oldSubtotal - oldVat - newSubtotal + newVat > 0 ? Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemSubtotalWithVatExcludedDiffType.AmountsDec : Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemSubtotalWithVatExcludedDiffType.AmountsInc
                                }
                            };

                            if (oldVat == newVat)
                                throw new Exception($"Сумма НДС до и после изменения не должны совпадать. ID товара {detail.IdGood}");

                            var country = _abt.RefCountries.FirstOrDefault(c => c.Id == detail.Good.IdCountry);

                            if (country != null)
                            {
                                string countryCode = country.NumCode?.ToString();

                                if (countryCode.Length == 1)
                                    countryCode = "00" + countryCode;
                                else if (countryCode.Length == 2)
                                    countryCode = "0" + countryCode;

                                additionalInfos.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = "цифровой код страны происхождения", Value = countryCode });
                                additionalInfos.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = "краткое наименование страны происхождения", Value = country.Name });

                                if (!string.IsNullOrEmpty(detail?.Good?.CustomsNo))
                                    additionalInfos.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = "регистрационный номер декларации на товары", Value = detail.Good.CustomsNo });
                            }

                            if (refEdoGoodChannel != null)
                            {
                                if (!string.IsNullOrEmpty(refEdoGoodChannel.DetailBuyerCodeUpdId))
                                {
                                    var edoGoodChannelId = refEdoGoodChannel.IdChannel;
                                    var goodMatching = _abt.RefGoodMatchings.FirstOrDefault(r => r.IdChannel == edoGoodChannelId && r.IdGood == detail.IdGood && r.Disabled == 0);

                                    if (goodMatching == null && SelectedDocument?.InvoiceDocJournal?.DocMaster != null)
                                    {
                                        var docDateTime = SelectedDocument.InvoiceDocJournal.DocMaster.DocDatetime.Date;

                                        goodMatching = _abt.RefGoodMatchings.FirstOrDefault(r => r.DisabledDatetime != null && r.IdChannel == edoGoodChannelId &&
                                        r.IdGood == detail.IdGood && r.Disabled == 1 && r.DisabledDatetime.Value >= docDateTime);
                                    }

                                    if (goodMatching == null)
                                        throw new Exception("Не все товары сопоставлены с кодами покупателя.");

                                    if (!string.IsNullOrEmpty(goodMatching?.CustomerArticle))
                                        additionalInfos.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = refEdoGoodChannel.DetailBuyerCodeUpdId, Value = goodMatching.CustomerArticle });
                                    else
                                        throw new Exception("Не для всех товаров заданы коды покупателя.");
                                }

                                if (!string.IsNullOrEmpty(refEdoGoodChannel.DetailBarCodeUpdId))
                                    additionalInfos.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = refEdoGoodChannel.DetailBarCodeUpdId, Value = barCode });

                                if (!string.IsNullOrEmpty(refEdoGoodChannel.DetailPositionUpdId))
                                    additionalInfos.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = refEdoGoodChannel.DetailPositionUpdId, Value = item.OriginalNumber });
                            }

                            item.AdditionalInfos = additionalInfos.ToArray();
                            itemDetails.Add(item);
                        }

                        correctionDocument.Table = new Diadoc.Api.DataXml.Ucd736.InvoiceCorrectionTable
                        {
                            TotalsDec = new Diadoc.Api.DataXml.Ucd736.InvoiceTotalsDiff736
                            {
                                Total = itemDetails.Where(i => i.Subtotal.ItemElementName == Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemSubtotalDiffType.AmountsDec).Sum(d => d.Subtotal.Item),
                                TotalSpecified = true,
                                Vat = itemDetails.Where(i => i.Vat.ItemElementName == Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemVatDiffType.AmountsDec).Sum(d => d.Vat.Item),
                                VatSpecified = true,
                                TotalWithVatExcluded = itemDetails.Where(i => i.SubtotalWithVatExcluded.ItemElementName == Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemSubtotalWithVatExcludedDiffType.AmountsDec).Sum(d => d.SubtotalWithVatExcluded.Item)
                            },

                            TotalsInc = new Diadoc.Api.DataXml.Ucd736.InvoiceTotalsDiff736
                            {
                                Total = itemDetails.Where(i => i.Subtotal.ItemElementName == Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemSubtotalDiffType.AmountsInc).Sum(d => d.Subtotal.Item),
                                TotalSpecified = true,
                                Vat = itemDetails.Where(i => i.Vat.ItemElementName == Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemVatDiffType.AmountsInc).Sum(d => d.Vat.Item),
                                VatSpecified = true,
                                TotalWithVatExcluded = itemDetails.Where(i => i.SubtotalWithVatExcluded.ItemElementName == Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItemSubtotalWithVatExcludedDiffType.AmountsInc).Sum(d => d.SubtotalWithVatExcluded.Item)
                            },
                            Items = itemDetails.ToArray()
                        };
                    }

                    correctionDocument.Invoices = new[]
                    {
                        new Diadoc.Api.DataXml.Ucd736.InvoiceForCorrectionInfo
                        {
                            Date = SelectedDocument?.InvoiceDocJournal?.DeliveryDate?.Date.ToString("dd.MM.yyyy"),
                            Number = SelectedDocument?.InvoiceDocJournal?.Code
                        }
                    };

                    var additionalInfoList = new List<Diadoc.Api.DataXml.AdditionalInfo>();
                    if (refEdoGoodChannel != null)
                    {
                        if (!string.IsNullOrEmpty(refEdoGoodChannel.NumberUpdId))
                            additionalInfoList.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = refEdoGoodChannel.NumberUpdId, Value = SelectedDocument?.InvoiceDocJournal?.Code });

                        if (SelectedDocument?.InvoiceDocJournal?.IdDocMaster != null && !string.IsNullOrEmpty(refEdoGoodChannel.OrderNumberUpdId))
                        {
                            var docJournalTag = _abt.DocJournalTags.FirstOrDefault(t => t.IdDoc == SelectedDocument.InvoiceDocJournal.IdDocMaster && t.IdTad == 137);
                            additionalInfoList.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = refEdoGoodChannel.OrderNumberUpdId, Value = docJournalTag?.TagValue ?? string.Empty });
                        }

                        if (!string.IsNullOrEmpty(refEdoGoodChannel.OrderDateUpdId))
                            additionalInfoList.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = refEdoGoodChannel.OrderDateUpdId, Value = SelectedDocument?.InvoiceDocJournal?.DocMaster?.DocDatetime.ToString("dd.MM.yyyy") });

                        if (SelectedDocument?.InvoiceDocJournal?.IdDocMaster != null && !string.IsNullOrEmpty(refEdoGoodChannel.GlnShipToUpdId))
                        {
                            if (WebService.Controllers.FinDbController.GetInstance().LoadedConfig)
                            {
                                var docOrderInfo = WebService.Controllers.FinDbController.GetInstance().GetDocOrderInfoByIdDocAndOrderStatus(SelectedDocument.InvoiceDocJournal.IdDocMaster.Value);

                                additionalInfoList.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = refEdoGoodChannel.GlnShipToUpdId, Value = docOrderInfo.GlnShipTo });
                            }
                        }

                        foreach (var keyValuePair in refEdoGoodChannel.EdoUcdValuesPairs.Where(
                            u => (SelectedDocument.CorrectionDocJournal?.IdDocType != null && u.IdDocType == SelectedDocument.CorrectionDocJournal.IdDocType) || u.IdDocType == 0))
                            additionalInfoList.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = keyValuePair.Key, Value = keyValuePair.Value });

                        if (SelectedDocument?.CorrectionDocJournal?.Id != null && !string.IsNullOrEmpty(refEdoGoodChannel.DocReturnNumberUcdId))
                        {
                            var docJournalTag = _abt.DocJournalTags.FirstOrDefault(d => d.IdDoc == SelectedDocument.CorrectionDocJournal.Id && d.IdTad == 101);

                            if (docJournalTag != null)
                                additionalInfoList.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = refEdoGoodChannel.DocReturnNumberUcdId, Value = docJournalTag.TagValue });
                        }

                        if (SelectedDocument?.CorrectionDocJournal?.Id != null && !string.IsNullOrEmpty(refEdoGoodChannel.DocReturnDateUcdId))
                        {
                            var docJournalTag = _abt.DocJournalTags.FirstOrDefault(d => d.IdDoc == SelectedDocument.CorrectionDocJournal.Id && d.IdTad == 102);

                            if (docJournalTag != null)
                                additionalInfoList.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = refEdoGoodChannel.DocReturnDateUcdId, Value = docJournalTag.TagValue });
                        }
                    }

                    if(additionalInfoList.Count > 0)
                        correctionDocument.AdditionalInfoId = new Diadoc.Api.DataXml.AdditionalInfoId736
                        {
                            AdditionalInfo = additionalInfoList.ToArray()
                        };

                    var generatedFile = EdiProcessingUnit.Edo.Edo.GetInstance().GenerateTitleXml(typeNamedId,
                            function, version, 0, correctionDocument);

                    loadContext.Text = "Подписание документа.";
                    var crypt = new Cryptography.WinApi.WinApiCryptWrapper(_currentOrganization.Certificate);
                    var signature = crypt.Sign(generatedFile.Content, true);

                    loadContext.Text = "Отправка документа.";

                    Diadoc.Api.Proto.Events.Message message;

                    var signedContent = new Diadoc.Api.Proto.Events.SignedContent
                    {
                        Content = generatedFile.Content,
                        Signature = signature
                    };

                    if (!string.IsNullOrEmpty(_currentOrganization.EmchdId))
                    {
                        var powerOfAttorneyToPost = new Diadoc.Api.Proto.Events.PowerOfAttorneyToPost
                        {
                            UseDefault = false,
                            FullId = new Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyFullId
                            {
                                RegistrationNumber = _currentOrganization.EmchdId,
                                IssuerInn = _currentOrganization.Inn
                            }
                        };

                        message = EdiProcessingUnit.Edo.Edo.GetInstance().SendDocumentAttachment(null, counteragentBox, typeNamedId, function, version, 
                            new List<Diadoc.Api.Proto.Events.SignedContent>(new[] { signedContent }),
                            null, null, powerOfAttorneyToPost, 
                            new Diadoc.Api.Proto.DocumentId
                            {
                                MessageId = baseProcessing.MessageId,
                                EntityId = baseProcessing.EntityId
                            });
                    }
                    else
                    {
                        message = EdiProcessingUnit.Edo.Edo.GetInstance().SendDocumentAttachment(null, counteragentBox, typeNamedId, function, version,
                            new List<Diadoc.Api.Proto.Events.SignedContent>(new[] { signedContent }),
                                null, null, null,
                                new Diadoc.Api.Proto.DocumentId
                                {
                                    MessageId = baseProcessing.MessageId,
                                    EntityId = baseProcessing.EntityId
                                });
                    }

                    if (message != null)
                    {
                        loadContext.Text = "Сохранение в базе данных.";

                        var entity = message.Entities.FirstOrDefault(t => t.AttachmentType == Diadoc.Api.Proto.Events.AttachmentType.UniversalCorrectionDocument);

                        var fileNameLength = entity.FileName.LastIndexOf('.');

                        if (fileNameLength < 0)
                            fileNameLength = entity.FileName.Length;

                        var fileName = entity.FileName.Substring(0, fileNameLength);

                        var newDocEdoProcessing = new DocEdoProcessing
                        {
                            Id = Guid.NewGuid().ToString(),
                            MessageId = message.MessageId,
                            EntityId = entity.EntityId,
                            FileName = fileName,
                            IsReprocessingStatus = 0,
                            IdDoc = SelectedDocument?.CorrectionDocJournal?.Id,
                            DocDate = DateTime.Now,
                            UserName = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser,
                            ReceiverName = baseProcessing.ReceiverName,
                            ReceiverInn = baseProcessing.ReceiverInn,
                            DocType = (int)EdiProcessingUnit.Enums.DocEdoType.Ucd,
                            IdParent = baseProcessing.Id,
                            Parent = baseProcessing,
                            HonestMarkStatus = SelectedDocument.IsMarked ? (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Sent : (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.None
                        };
                        baseProcessing.Children.Add(newDocEdoProcessing);
                        //_abt.DocEdoProcessings.Add(newDocEdoProcessing);

                        _abt.SaveChanges();
                        SelectedDocument.EdoProcessing = newDocEdoProcessing;
                        OnPropertyChanged("SelectedDocument");
                    }

                    loadWindow.SetSuccessFullLoad(loadContext, "Отправка завершена успешно.");
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            if (exception != null)
            {
                loadWindow.Close();

                var errorWindow = new ErrorWindow(
                            "Произошла ошибка отправки корректировки.",
                            new List<string>(
                                new string[]
                                {
                                    exception.Message,
                                    exception.StackTrace
                                }
                                ));

                errorWindow.ShowDialog();
                _log.Log($"CorrectionDocumentsModel.SendDocument Exception: {_log.GetRecursiveInnerException(exception)}");
            }
            else
                _log.Log($"CorrectionDocumentsModel : отправка корректировки завершена успешно.");
        }
    }
}
