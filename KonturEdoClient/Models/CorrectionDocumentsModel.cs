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
        private X509Certificate2 _orgCert;
        private UniversalTransferDocument _baseDocument;
        private AbtDbContext _abt;
        private UtilitesLibrary.Logger.UtilityLog _log = UtilitesLibrary.Logger.UtilityLog.GetInstance();

        public System.Windows.Window Owner { get; set; }

        public ObservableCollection<UniversalCorrectionDocument> Documents { get; set; }
        public UniversalCorrectionDocument SelectedDocument { get; set; }

        public List<DocGoodsDetail> Details => SelectedDocument?.CorrectionDocJournal?.Details;

        public CorrectionDocumentsModel(AbtDbContext abt, X509Certificate2 orgCert, UniversalTransferDocument baseDocument)
        {
            _baseDocument = baseDocument;
            _abt = abt;
            _orgCert = orgCert;
        }

        public async void SendDocument()
        {
            if (SelectedDocument == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбран корректировочный документ.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var baseProcessing = _baseDocument.EdoProcessing as DocEdoProcessing;
            var buyerContractor = SelectedDocument.CorrectionDocJournal?.DocGoods?.Customer;

            if (buyerContractor?.DefaultCustomer == null)
            {
                System.Windows.MessageBox.Show(
                    "Не задана принимающая организация в базе.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

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
                    var buyerCustomer = _abt.RefCustomers.FirstOrDefault(r => r.Id == buyerContractor.DefaultCustomer);

                    var typeNamedId = "UniversalCorrectionDocument";
                    var function = "КСЧФ";
                    var version = "ucd736_05_01_02";

                    var correctionDocument = new Diadoc.Api.DataXml.Ucd736.UniversalCorrectionDocument
                    {
                        Currency = Properties.Settings.Default.DefaultCurrency,
                        Function = Diadoc.Api.DataXml.Ucd736.UniversalCorrectionDocumentFunction.КСЧФ,
                        DocumentNumber = SelectedDocument.DocumentNumber,
                        DocumentDate = SelectedDocument?.CorrectionDocJournal?.DeliveryDate?.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy")
                    };

                    correctionDocument.Seller = new Diadoc.Api.DataXml.Ucd736.ExtendedOrganizationInfo_ForeignAddress1000
                    {
                        Item = new Diadoc.Api.DataXml.Ucd736.ExtendedOrganizationDetails_ForeignAddress1000
                        {
                            Inn = buyerCustomer.Inn,
                            Kpp = buyerCustomer.Kpp,
                            OrgName = buyerCustomer.Name,
                            OrgType = buyerCustomer.Inn.Length == 12 ? Diadoc.Api.DataXml.OrganizationType.IndividualEntity : Diadoc.Api.DataXml.OrganizationType.LegalEntity,
                            Address = new Diadoc.Api.DataXml.Ucd736.Address_ForeignAddress1000
                            {
                                Item = new Diadoc.Api.DataXml.Ucd736.ForeignAddress1000
                                {
                                    Country = Properties.Settings.Default.DefaultOrgCountryCode,
                                    Address = buyerContractor.Address
                                }
                            }
                        }
                    };

                    var orgInn = utils.GetOrgInnFromCertificate(_orgCert);
                    bool result = EdiProcessingUnit.Edo.Edo.GetInstance().Authenticate(false, _orgCert, orgInn);

                    if (!result)
                        throw new Exception("Не удалось авторизоваться в системе по сертификату.");

                    var baseDocument = EdiProcessingUnit.Edo.Edo.GetInstance().GetDocument(baseProcessing.MessageId, baseProcessing.EntityId);

                    var counteragentBox = baseDocument.CounteragentBoxId;

                    correctionDocument.Buyer = new Diadoc.Api.DataXml.Ucd736.ExtendedOrganizationInfo_ForeignAddress1000
                    {
                        Item = new Diadoc.Api.DataXml.Ucd736.ExtendedOrganizationReference
                        {
                            BoxId = counteragentBox,
                            ShortOrgName = baseProcessing.ReceiverName,
                            OrgType = baseProcessing.ReceiverInn.Length == 12 ? Diadoc.Api.DataXml.OrganizationType.IndividualEntity : Diadoc.Api.DataXml.OrganizationType.LegalEntity
                        }
                    };

                    var firstMiddleName = utils.ParseCertAttribute(_orgCert.Subject, "G");
                    string signerFirstName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
                    string signerMiddleName = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;
                    string signerLastName = utils.ParseCertAttribute(_orgCert.Subject, "SN");

                    var signer = new[]
                    {
                    new Diadoc.Api.DataXml.ExtendedSignerDetails_CorrectionSellerTitle
                    {
                        SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.LegalEntity,
                        FirstName = signerFirstName,
                        MiddleName = signerMiddleName,
                        LastName = signerLastName,
                        SignerOrganizationName = utils.ParseCertAttribute(_orgCert.Subject, "CN"),
                        Inn = orgInn,
                        Position = utils.ParseCertAttribute(_orgCert.Subject, "T")
                    }
                };
                    correctionDocument.UseSignerDetails(signer);

                    correctionDocument.DocumentCreator = signerLastName + " " + firstMiddleName;

                    var itemDetails = new List<Diadoc.Api.DataXml.Ucd736.ExtendedInvoiceCorrectionItem>();
                    foreach (var detail in Details)
                    {
                        var baseDetail = _baseDocument.DocJournal.DocGoodsDetailsIs.FirstOrDefault(d => d.IdGood == detail.IdGood);

                        if (baseDetail == null)
                            throw new Exception($"Не найден товар в исходном документе {detail.Good.Name}, ID {detail.IdGood}");

                        var additionalInfos = new List<Diadoc.Api.DataXml.AdditionalInfo>();

                        int baseIndex = _baseDocument.DocJournal.DocGoodsDetailsIs.IndexOf(baseDetail) + 1;

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
                                CorrectedValue = detail.Quantity - baseDetail.Quantity,
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

                        if(_baseDocument.RefEdoGoodChannel as RefEdoGoodChannel != null)
                        {
                            var refEdoGoodChannel = _baseDocument.RefEdoGoodChannel as RefEdoGoodChannel;
                            var idChannel = refEdoGoodChannel.IdChannel;
                            if (!string.IsNullOrEmpty(refEdoGoodChannel.DetailBuyerCodeUpdId))
                            {
                                var goodMatching = _abt.RefGoodMatchings.FirstOrDefault(r => r.IdChannel == idChannel && r.IdGood == detail.IdGood && r.Disabled == 0);

                                if (goodMatching == null)
                                    throw new Exception("Не все товары сопоставлены с кодами покупателя.");

                                if (!string.IsNullOrEmpty(goodMatching?.CustomerArticle))
                                    additionalInfos.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = refEdoGoodChannel.DetailBuyerCodeUpdId, Value = goodMatching.CustomerArticle });
                                else
                                    throw new Exception("Не для всех товаров заданы коды покупателя.");
                            }

                            if (!string.IsNullOrEmpty(refEdoGoodChannel.DetailBarCodeUpdId))
                                additionalInfos.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = refEdoGoodChannel.DetailBarCodeUpdId, Value = barCode });
                        }

                        item.AdditionalInfos = additionalInfos.ToArray();
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

                    correctionDocument.Invoices = new[]
                    {
                        new Diadoc.Api.DataXml.Ucd736.InvoiceForCorrectionInfo
                        {
                            Date = _baseDocument?.DocJournal?.DeliveryDate?.Date.ToString("dd.MM.yyyy"),
                            Number = _baseDocument?.DocJournal?.Code
                        }
                    };

                    var additionalInfoList = new List<Diadoc.Api.DataXml.AdditionalInfo>();
                    if (_baseDocument.RefEdoGoodChannel as RefEdoGoodChannel != null)
                    {
                        var refEdoGoodChannel = _baseDocument.RefEdoGoodChannel as RefEdoGoodChannel;
                        if (!string.IsNullOrEmpty(refEdoGoodChannel.NumberUpdId))
                            additionalInfoList.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = refEdoGoodChannel.NumberUpdId, Value = _baseDocument?.DocJournal?.Code });

                        if (_baseDocument?.DocJournal?.IdDocMaster != null && !string.IsNullOrEmpty(refEdoGoodChannel.OrderNumberUpdId))
                        {
                            var docJournalTag = _abt.DocJournalTags.FirstOrDefault(t => t.IdDoc == _baseDocument.DocJournal.IdDocMaster && t.IdTad == 137);
                            additionalInfoList.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = refEdoGoodChannel.OrderNumberUpdId, Value = docJournalTag?.TagValue ?? string.Empty });
                        }

                        if (!string.IsNullOrEmpty(refEdoGoodChannel.OrderDateUpdId))
                            additionalInfoList.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = refEdoGoodChannel.OrderDateUpdId, Value = _baseDocument?.DocJournal?.DocMaster?.DocDatetime.ToString("dd.MM.yyyy") });

                        foreach (var keyValuePair in refEdoGoodChannel.EdoUcdValuesPairs)
                            additionalInfoList.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = keyValuePair.Key, Value = keyValuePair.Value });

                        if (SelectedDocument?.CorrectionDocJournal?.Id != null && !string.IsNullOrEmpty(refEdoGoodChannel.DocReturnNumberUcdId))
                        {
                            var docJournalTag = _abt.DocJournalTags.FirstOrDefault(d => d.IdDoc == SelectedDocument.CorrectionDocJournal.Id && d.IdTad == 101);

                            if (docJournalTag != null)
                                additionalInfoList.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = refEdoGoodChannel.DocReturnNumberUcdId, Value = docJournalTag.TagValue });
                            else
                                throw new Exception("Не найден номер возвратного документа.");
                        }

                        if (SelectedDocument?.CorrectionDocJournal?.Id != null && !string.IsNullOrEmpty(refEdoGoodChannel.DocReturnDateUcdId))
                        {
                            var docJournalTag = _abt.DocJournalTags.FirstOrDefault(d => d.IdDoc == SelectedDocument.CorrectionDocJournal.Id && d.IdTad == 102);

                            if (docJournalTag != null)
                                additionalInfoList.Add(new Diadoc.Api.DataXml.AdditionalInfo { Id = refEdoGoodChannel.DocReturnDateUcdId, Value = docJournalTag.TagValue });
                            else
                                throw new Exception("Не найдена дата возвратного документа.");
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
                    var crypt = new Cryptography.WinApi.WinApiCryptWrapper(_orgCert);
                    var signature = crypt.Sign(generatedFile.Content, true);

                    loadContext.Text = "Отправка документа.";
                    var message = EdiProcessingUnit.Edo.Edo.GetInstance().SendDocumentAttachment(null, counteragentBox, typeNamedId, function, version, generatedFile.Content, signature,
                        null, null, baseProcessing.MessageId, baseProcessing.EntityId);

                    if(message != null)
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
                            Parent = baseProcessing
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
