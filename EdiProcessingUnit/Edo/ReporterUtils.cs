using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using System.Security.Cryptography.X509Certificates;
using Reporter;
using Reporter.Reports;

namespace EdiProcessingUnit.Edo
{
    public class ReporterUtils
    {
        private AbtDbContext _abt;
        private string _edoProgramName;
        private string _edoProgramVersion;
        private string _currentFileName;
        private const string _prefixSellerFileName = "ON_NSCHFDOPPR";
        private XmlUtils _xmlUtils;

        public ReporterUtils(AbtDbContext abt, string edoProgramName, string edoProgramVersion)
        {
            _edoProgramName = edoProgramName;
            _edoProgramVersion = edoProgramVersion;
            _abt = abt;
            _xmlUtils = new XmlUtils();
        }

        public string EdoProgram => $"{_edoProgramName} {_edoProgramVersion}";

        public string EmployeePosition { get; set; }
        public string Employee { get; set; }

        public string CurrentFileName => _currentFileName;

        public TReport GetReport<TReport>(Models.Kontragent sender, RefCustomer receiver, DocJournal docJournal, X509Certificate2 senderCertificate, bool isMarked = false,
            List<DocGoodsDetailsLabels> markedCodes = null, RefEdoGoodChannel edoGoodChannel = null, List<DocJournalTag> docJournalTags = null, KeyValuePair<string, string>? contract = null) where TReport:class,IReport,new()
        {
            TReport resultObj=new TReport();

            var type = typeof(TReport);
            if (resultObj is UniversalTransferSellerDocumentUtd970)
            {
                resultObj = GetUniversalTransferDocumentUtd970(sender, receiver, docJournal, senderCertificate, isMarked,
                    markedCodes, edoGoodChannel, docJournalTags, contract, resultObj as UniversalTransferSellerDocumentUtd970) as TReport;
            }
            else return null;

            return resultObj;
        }

        public byte[] GetReportXmlContent(Models.Kontragent sender, RefCustomer receiver, DocJournal docJournal, X509Certificate2 senderCertificate, bool isMarked = false,
            List<DocGoodsDetailsLabels> markedCodes = null, RefEdoGoodChannel edoGoodChannel = null, List<DocJournalTag> docJournalTags = null, KeyValuePair<string, string>? contract = null)
        {
            IReport report;

            if (docJournal?.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
            {
                report = GetReport<UniversalTransferSellerDocumentUtd970>(sender, receiver, docJournal, senderCertificate, isMarked, markedCodes, edoGoodChannel, docJournalTags, contract);
                _currentFileName = (report as UniversalTransferSellerDocumentUtd970).FileName;
            }
            else
                throw new Exception("Не определён тип документа.");

            var xml = report.GetXmlContent();
            var content = Encoding.GetEncoding(1251).GetBytes(xml);
            return content;
        }

        public UniversalTransferSellerDocumentUtd970 GetUniversalTransferDocumentUtd970(Models.Kontragent sender, RefCustomer receiver, DocJournal docJournal, X509Certificate2 senderCertificate, 
            bool isMarked = false, List<DocGoodsDetailsLabels> markedCodes = null, RefEdoGoodChannel edoGoodChannel = null, List<DocJournalTag> docJournalTags = null, KeyValuePair<string, string>? contract = null,
            UniversalTransferSellerDocumentUtd970 reportObj = null)
        {
            if (docJournal?.IdDocType != (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
                throw new Exception("Данный документ не является счёт-фактурой!");

            if (string.IsNullOrEmpty(sender?.Inn))
                throw new Exception("Не задан ИНН отправителя!");

            if (string.IsNullOrEmpty(receiver?.Inn))
                throw new Exception("Не задан ИНН получателя!");

            if (reportObj == null)
                reportObj = new UniversalTransferSellerDocumentUtd970();

            var receiverKpp = receiver.Inn.Length == 10 && !string.IsNullOrEmpty(receiver.Kpp) ? receiver.Kpp + "_" : string.Empty;
            var senderKpp = sender.Inn.Length == 10 && !string.IsNullOrEmpty(sender.Kpp) ? sender.Kpp + "_" : string.Empty;

            if (isMarked)
                reportObj.FileName = $"{_prefixSellerFileName}_{receiver.Inn}_{receiverKpp}{sender.Inn}_{senderKpp}{DateTime.Now.ToString("yyyyMMdd")}_{Guid.NewGuid().ToString()}_0_1_0_0_0_00";
            else
                reportObj.FileName = $"{_prefixSellerFileName}_{receiver.Inn}_{receiverKpp}{sender.Inn}_{senderKpp}{DateTime.Now.ToString("yyyyMMdd")}_{Guid.NewGuid().ToString()}_0_0_0_0_0_00";

            reportObj.EdoProgramVersion = this.EdoProgram;

            reportObj.Function = "СЧФДОП";
            reportObj.CreateDate = DateTime.Now;
            reportObj.DocName = "Универсальный передаточный документ";
            reportObj.EconomicLifeDocName = "Документ об отгрузке товаров (выполнении работ), передаче имущественных прав (документ об оказании услуг)";
            reportObj.DocNumber = docJournal.Code;
            reportObj.DocDate = docJournal.DeliveryDate?.Date ?? DateTime.Now.Date;

            if(!string.IsNullOrEmpty(sender?.EmchdId))
                reportObj.FinSubjectCreator = $"{sender.EmchdPersonSurname} {sender.EmchdPersonName} {sender.EmchdPersonPatronymicSurname}";
            else
                reportObj.FinSubjectCreator = $"{sender.Name}, ИНН: {sender.Inn}";

            if(sender.Inn.Length == 10)
            {
                var sellerOrganizationExchangeParticipant = new Reporter.Entities.OrganizationExchangeParticipantEntity();

                sellerOrganizationExchangeParticipant.JuridicalInn = sender.Inn;
                sellerOrganizationExchangeParticipant.JuridicalKpp = sender.Kpp;
                sellerOrganizationExchangeParticipant.OrgName = sender.Name;

                reportObj.SellerEntity = sellerOrganizationExchangeParticipant;
                reportObj.ShipperEntity = sellerOrganizationExchangeParticipant;
            }
            else if(sender.Inn.Length == 12)
            {
                var sellerJuridicalEntity = new Reporter.Entities.JuridicalEntity();
                sellerJuridicalEntity.Inn = sender.Inn;

                sellerJuridicalEntity.Surname = _xmlUtils.ParseCertAttribute(senderCertificate.Subject, "SN");
                var firstMiddleName = _xmlUtils.ParseCertAttribute(senderCertificate.Subject, "G");
                sellerJuridicalEntity.Name = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
                sellerJuridicalEntity.Patronymic = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;

                reportObj.SellerEntity = sellerJuridicalEntity;
                reportObj.ShipperEntity = sellerJuridicalEntity;
            }

            var senderContractor = docJournal?.DocMaster?.DocGoods?.Seller;

            if (sender?.Address?.RussianAddress != null)
            {
                var zipCode = sender?.Address?.RussianAddress?.ZipCode;
                var region = sender?.Address?.RussianAddress?.Region;
                var street = string.IsNullOrEmpty(sender?.Address?.RussianAddress?.Street) ? null : sender.Address.RussianAddress.Street;
                var city = string.IsNullOrEmpty(sender?.Address?.RussianAddress?.City) ? null : sender.Address.RussianAddress.City;
                var locality = string.IsNullOrEmpty(sender?.Address?.RussianAddress?.Locality) ? null : sender.Address.RussianAddress.Locality;
                var territory = string.IsNullOrEmpty(sender?.Address?.RussianAddress?.Territory) ? null : sender.Address.RussianAddress.Territory;
                var building = string.IsNullOrEmpty(sender?.Address?.RussianAddress?.Building) ? null : sender.Address.RussianAddress.Building;
                var appartment = string.IsNullOrEmpty(sender?.Address?.RussianAddress?.Apartment) ? null : sender.Address.RussianAddress.Apartment;

                if(street?.StartsWith("ул ") ?? false)
                {
                    street = street.Substring(3);
                }

                reportObj.SellerAddress = new Reporter.Entities.Address
                {
                    CountryCode = "643",
                    CountryName = "Россия",
                    RussianIndex = zipCode,
                    RussianRegionCode = region,
                    RussianRegionName = region == "25" ? "Приморский край" : null,
                    RussianCity = city,
                    RussianStreet = street,
                    RussianHouse = building,
                    RussianFlat = appartment
                };
            }
            else
            {
                reportObj.SellerAddress = new Reporter.Entities.Address
                {
                    CountryCode = "643",
                    CountryName = "Россия",
                    ForeignTextAddress = senderContractor?.Address
                };
            }

            if(senderContractor != null)
                reportObj.ShipperAddress = new Reporter.Entities.Address
                {
                    CountryCode = "643",
                    CountryName = "Россия",
                    ForeignTextAddress = senderContractor.Address
                };

            if (receiver.Inn.Length == 10)
            {
                var buyerOrganizationExchangeParticipant = new Reporter.Entities.OrganizationExchangeParticipantEntity();

                buyerOrganizationExchangeParticipant.JuridicalInn = receiver.Inn;
                buyerOrganizationExchangeParticipant.JuridicalKpp = receiver.Kpp;
                buyerOrganizationExchangeParticipant.OrgName = receiver.Name;

                reportObj.BuyerEntity = buyerOrganizationExchangeParticipant;
                reportObj.ConsigneeEntity = buyerOrganizationExchangeParticipant;
            }
            else if(receiver.Inn.Length == 12)
            {
                var buyerJuridicalEntity = new Reporter.Entities.JuridicalEntity();
                buyerJuridicalEntity.Inn = receiver.Inn;

                reportObj.BuyerEntity = buyerJuridicalEntity;
                reportObj.ConsigneeEntity = buyerJuridicalEntity;
            }

            reportObj.BuyerAddress = new Reporter.Entities.Address
            {
                CountryCode = "643",
                CountryName = "Россия",
                ForeignTextAddress = receiver.Address
            };

            var receiverContractor = docJournal?.DocMaster?.DocGoods?.Customer;

            if (receiverContractor != null)
                reportObj.ConsigneeAddress = new Reporter.Entities.Address
                {
                    CountryCode = "643",
                    CountryName = "Россия",
                    ForeignTextAddress = receiverContractor.Address
                };

            reportObj.CurrencyName = "Российский рубль";
            reportObj.CurrencyCode = "643";
            reportObj.DeliveryDocuments = new List<Reporter.Entities.DeliveryDocument>
                {
                    new Reporter.Entities.DeliveryDocument
                    {
                        DocumentName = "Универсальный передаточный документ",
                        DocumentNumber = reportObj.DocNumber,
                        DocumentDate = docJournal.DeliveryDate?.Date ?? DateTime.Now.Date
                    }
                };

            reportObj.ContentOperation = "Товары переданы";
            reportObj.ShippingDate = docJournal.DeliveryDate?.Date ?? DateTime.Now.Date;
            //reportObj.BasisDocumentName = "Без документа-основания";

            if(contract != null)
            {
                reportObj.BasisDocumentName = "Договор поставки";
                reportObj.BasisDocumentNumber = contract.Value.Key;
                reportObj.BasisDocumentDate = DateTime.ParseExact(contract.Value.Value, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);
            }

            if (!(string.IsNullOrEmpty(Employee) || string.IsNullOrEmpty(EmployeePosition)))
            {
                reportObj.PersonTransferedTheGoods = new Reporter.Entities.OrganizationEmployee
                {
                    Position = EmployeePosition,
                    Surname = Employee.Substring(0, Employee.IndexOf(' ')),
                    Name = Employee.Substring(Employee.IndexOf(' ') + 1),
                    OtherInfo = Employee
                };
            }

            var additionalInfoList = new List<Reporter.Entities.AdditionalInfo>();

            if (edoGoodChannel != null)
            {
                if (!string.IsNullOrEmpty(edoGoodChannel.NumberUpdId))
                    additionalInfoList.Add(new Reporter.Entities.AdditionalInfo { Key = edoGoodChannel.NumberUpdId, Value = docJournal.Code });

                if (!string.IsNullOrEmpty(edoGoodChannel.OrderDateUpdId))
                    additionalInfoList.Add(new Reporter.Entities.AdditionalInfo { Key = edoGoodChannel.OrderDateUpdId, Value = docJournal.DocMaster.DocDatetime.ToString("dd.MM.yyyy") });

                if (docJournalTags != null)
                {
                    if (!string.IsNullOrEmpty(edoGoodChannel.OrderNumberUpdId))
                    {
                        var docJournalTag = docJournalTags.FirstOrDefault(t => t.IdDoc == docJournal.IdDocMaster && t.IdTad == 137);

                        if (docJournalTag == null)
                            throw new Exception("Отсутствует номер заказа покупателя.");

                        additionalInfoList.Add(new Reporter.Entities.AdditionalInfo { Key = edoGoodChannel.OrderNumberUpdId, Value = docJournalTag?.TagValue ?? string.Empty });
                    }

                    if (!string.IsNullOrEmpty(edoGoodChannel.GlnShipToUpdId))
                    {
                        string glnShipTo = null;
                        var shipToGlnJournalTag = docJournalTags.FirstOrDefault(t => t.IdDoc == docJournal.IdDocMaster && t.IdTad == 222);

                        if (shipToGlnJournalTag != null)
                        {
                            glnShipTo = shipToGlnJournalTag.TagValue;
                        }
                        else if (WebService.Controllers.FinDbController.GetInstance().LoadedConfig)
                        {
                            var docOrderInfo = WebService.Controllers.FinDbController.GetInstance().GetDocOrderInfoByIdDocAndOrderStatus(docJournal.IdDocMaster.Value);
                            glnShipTo = docOrderInfo?.GlnShipTo;
                        }

                        if (!string.IsNullOrEmpty(glnShipTo))
                            additionalInfoList.Add(new Reporter.Entities.AdditionalInfo { Key = edoGoodChannel.GlnShipToUpdId, Value = glnShipTo });
                    }
                }

                foreach (var keyValuePair in edoGoodChannel.EdoValuesPairs)
                    additionalInfoList.Add(new Reporter.Entities.AdditionalInfo { Key = keyValuePair.Key, Value = keyValuePair.Value });
            }

            if (additionalInfoList.Count > 0)
                reportObj.AdditionalInfos = additionalInfoList;

            var productList = new List<Reporter.Entities.Product>();
            int i = 0;
            foreach (var detail in docJournal.DocGoodsDetailsIs)
            {
                if (detail.Good == null)
                    continue;

                var barCode = detail.Good.BarCodes?.FirstOrDefault(r => r.IsPrimary == false && r.BarCode != null)?.BarCode;

                var subtotal = Math.Round(detail.Quantity * ((decimal)detail.Price - (decimal)detail.DiscountSumm), 2);
                var vat = (decimal)Math.Round(subtotal * detail.TaxRate / (detail.TaxRate + 100), 2, MidpointRounding.AwayFromZero);

                decimal price = 0;

                if (detail.Quantity > 0)
                    price = (decimal)Math.Round((subtotal - vat) / detail.Quantity, 2, MidpointRounding.AwayFromZero);
                else
                    price = (decimal)Math.Round(detail.Price - detail.DiscountSumm - detail.TaxSumm, 2);

                var product = new Reporter.Entities.Product()
                {
                    Number = ++i,
                    Description = detail.Good.Name,
                    UnitCode = "796",
                    Quantity = detail.Quantity,
                    Price = price,
                    BarCode = barCode,
                    UnitName = "шт",
                    Subtotal = subtotal,
                    TaxAmount = vat,
                    SubtotalWithVatExcluded = subtotal - vat,
                    VatRate = detail.TaxRate
                };

                var refGood = detail.Good;

                product.OriginCode = refGood?.Country?.NumCode?.ToString();

                if (!string.IsNullOrEmpty(refGood?.CustomsNo))
                {
                    product.CustomsDeclarationCode = refGood?.CustomsNo;
                    product.OriginCountryName = refGood?.Country?.Name?.ToString();
                }

                if(isMarked && markedCodes != null)
                {
                    product.MarkedCodes = markedCodes.Where(m => m.IdGood == detail.IdGood).Select(m => m.DmLabel)?.ToList() ?? new List<string>();

                    if (product.MarkedCodes.Count != product.Quantity)
                        throw new Exception("Количество кодов маркировки не совпадает с количеством товара.");
                }

                if (edoGoodChannel != null)
                {
                    var idChannel = edoGoodChannel.IdChannel;
                    if (!string.IsNullOrEmpty(edoGoodChannel.DetailBuyerCodeUpdId))
                    {
                        var goodMatching = _abt.RefGoodMatchings.FirstOrDefault(r => r.IdChannel == idChannel && r.IdGood == refGood.Id && r.Disabled == 0);

                        if (goodMatching == null)
                        {
                            var docDateTime = docJournal.DocMaster.DocDatetime.Date;

                            goodMatching = _abt.RefGoodMatchings.FirstOrDefault(r => r.DisabledDatetime != null && r.IdChannel == idChannel &&
                            r.IdGood == refGood.Id && r.Disabled == 1 && r.DisabledDatetime.Value >= docDateTime);
                        }

                        if (goodMatching == null)
                            throw new Exception("Не все товары сопоставлены с кодами покупателя.");

                        if (!string.IsNullOrEmpty(goodMatching?.CustomerArticle))
                            product.AdditionalInfos.Add(new Reporter.Entities.AdditionalInfo { Key = edoGoodChannel.DetailBuyerCodeUpdId, Value = goodMatching.CustomerArticle });
                        else
                            throw new Exception("Не для всех товаров заданы коды покупателя.");
                    }

                    if (!string.IsNullOrEmpty(edoGoodChannel.DetailBarCodeUpdId))
                        product.AdditionalInfos.Add(new Reporter.Entities.AdditionalInfo { Key = edoGoodChannel.DetailBarCodeUpdId, Value = barCode });

                    if (!string.IsNullOrEmpty(edoGoodChannel.DetailPositionUpdId))
                        product.AdditionalInfos.Add(new Reporter.Entities.AdditionalInfo { Key = edoGoodChannel.DetailPositionUpdId, Value = i.ToString() });
                }

                productList.Add(product);
            }

            reportObj.Products = productList;
            reportObj.SignType = Reporter.Enums.SignTypeEnum.QualifiedElectronicDigitalSignature;
            reportObj.SignDate = DateTime.Now.Date;

            if (!string.IsNullOrEmpty(sender?.EmchdId))
            {
                reportObj.MethodOfConfirmingAuthority = Reporter.Enums.MethodOfConfirmingAuthorityEnum.EmchdDataInDocument;
                reportObj.ElectronicPowerOfAttorney = new Reporter.Entities.ElectronicPowerOfAttorney
                {
                    RegistrationNumber = sender?.EmchdId,
                    RegistrationDate = sender.EmchdBeginDate.Value,
                    SystemIdentificationInfo = $"https://m4d.nalog.gov.ru/emchd/check-status?guid={sender?.EmchdId}"
                };

                reportObj.SignerSurname = sender.EmchdPersonSurname;
                reportObj.SignerName = sender.EmchdPersonName;
                reportObj.SignerPatronymic = sender.EmchdPersonPatronymicSurname;
                reportObj.SignerPosition = sender.EmchdPersonPosition;
            }
            else
            {
                reportObj.MethodOfConfirmingAuthority = Reporter.Enums.MethodOfConfirmingAuthorityEnum.DigitalSignature;

                reportObj.SignerPosition = _xmlUtils.ParseCertAttribute(senderCertificate.Subject, "T");
                reportObj.SignerSurname = _xmlUtils.ParseCertAttribute(senderCertificate.Subject, "SN");
                var firstMiddleName = _xmlUtils.ParseCertAttribute(senderCertificate.Subject, "G");
                reportObj.SignerName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
                reportObj.SignerPatronymic = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;
            }

            return reportObj;
        }
    }
}
