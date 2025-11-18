using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reporter.Entities;
using Reporter.Enums;
using Reporter.XsdClasses.OnNschfdopprUtd970;

namespace Reporter.Reports
{
    public class UniversalTransferSellerDocumentUtd970 : IReport
    {
        private const string codeOfRussia = "643";

        private XmlUtils _xmlUtils;

        public UniversalTransferSellerDocumentUtd970()
        {
            _xmlUtils = new XmlUtils();
        }

        #region Properties
        #region Общая информация

        /// <summary>
        /// Идентификатор файла
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Версия формата
        /// </summary>
        public const string FormatVersion = "5.03";

        /// <summary>
        /// Версия программы, с помощью которой сформирован файл
        /// </summary>
        public string EdoProgramVersion { get; set; }

        #endregion

        #region Сведения об участниках электронного документооборота

        /// <summary>
        /// Идентификатор оператора электронного документооборота отправителя файла обмена информации покупателя
        /// </summary>
        public string EdoId { get; set; }

        /// <summary>
        /// Идентификатор участника документооборота – отправителя файла обмена информации покупателя
        /// </summary>
        public string SenderEdoId { get; set; }

        /// <summary>
        /// Идентификатор участника документооборота – получателя файла обмена информации покупателя
        /// </summary>
        public string ReceiverEdoId { get; set; }

        #endregion

        #region Документ
        /// <summary>
        /// Код документа  по КНД
        /// </summary>
        public const string KND = "1115131";

        /// <summary>
        /// Дата и время формирования файла обмена счета-фактуры (информации продавца)
        /// </summary>
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// Функция
        /// </summary>
        public string Function { get; set; }

        /// <summary>
        /// Наименование первичного документа, определенное организацией (согласованное сторонами сделки)
        /// </summary>
        public string DocName { get; set; }

        /// <summary>
        /// Порядковый номер счета-фактуры (строка 1 счета-фактуры), документа об отгрузке товаров (выполнении работ), передаче имущественных прав (документа об оказании услуг)
        /// </summary>
        public string DocNumber { get; set; }

        /// <summary>
        /// Наименование документа по факту хозяйственной жизни
        /// </summary>
        public string EconomicLifeDocName { get; set; }

        /// <summary>
        /// Наименование экономического субъекта – составителя файла обмена счета-фактуры (информации продавца)
        /// </summary>
        public string FinSubjectCreator { get; set; }

        /// <summary>
        /// Код из Общероссийского классификатора валют
        /// </summary>
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Дата составления (выписки) счета-фактуры (строка 1 счета-фактуры), документа об отгрузке товаров (выполнении работ), передаче имущественных прав (документа об оказании услуг)
        /// </summary>
        public DateTime DocDate { get; set; }

        #region Сведения о продавце (строки 2, 2а, 2б счета-фактуры)

        public object SellerEntity { get; set; }
        public Address SellerAddress { get; set; }

        #endregion

        #region Сведения о грузоотправителе (строка 3 счета-фактуры)

        public bool SameShipper { get; set; }
        public object ShipperEntity { get; set; } = null;
        public Address ShipperAddress { get; set; }

        #endregion

        #region Грузополучатель и его адрес (строка 4 счета-фактуры)

        public object ConsigneeEntity { get; set; }
        public Address ConsigneeAddress { get; set; }

        #endregion

        #region Сведения о покупателе (строки 6, 6а, 6б счета-фактуры)

        public object BuyerEntity { get; set; }
        public Address BuyerAddress { get; set; }

        #endregion

        #region Дополнительные сведения об участниках факта хозяйственной жизни, основаниях и обстоятельствах его проведения

        /// <summary>
        /// Наименование валюты
        /// </summary>
        public string CurrencyName { get; set; }

        #endregion

        #region Информационное поле факта хозяйственной жизни 1

        public List<AdditionalInfo> AdditionalInfos { get; set; }

        #endregion

        #region Реквизиты документа, подтверждающего отгрузку товаров (работ, услуг, имущественных прав)

        public List<DeliveryDocument> DeliveryDocuments { get; set; } = new List<DeliveryDocument>();

        #endregion
        #endregion

        #region Сведения таблицы счета-фактуры (содержание факта хозяйственной жизни 2 - наименование и другая информация об отгруженных товарах (выполненных работах, оказанных услугах), о переданных имущественных правах
        #region Сведения о товарах
        public List<Product> Products { get; set; }
        #endregion

        #region Всего к оплате

        #endregion
        #endregion

        #region Содержание факта хозяйственной жизни 3 – сведения о факте отгрузки товаров (выполнения работ), передачи имущественных прав (о предъявлении оказанных услуг)
        #region Сведения о передаче (сдаче) товаров (результатов работ), имущественных прав (о предъявлении оказанных услуг)

        /// <summary>
        /// Содержание операции
        /// </summary>
        public string ContentOperation { get; set; }

        /// <summary>
        /// Вид операции
        /// </summary>
        public string OperationType { get; set; }

        /// <summary>
        /// Дата отгрузки товаров (передачи результатов работ), передачи имущественных прав (предъявления оказанных услуг)
        /// </summary>
        public DateTime? ShippingDate { get; set; } = null;

        #region Основание отгрузки товаров (передачи результатов работ), передачи  имущественных прав (предъявления оказанных услуг)

        /// <summary>
        /// Наименование документа - основания
        /// </summary>
        public string BasisDocumentName { get; set; }

        /// <summary>
        /// Номер документа - основания
        /// </summary>
        public string BasisDocumentNumber { get; set; }

        /// <summary>
        /// Дата документа - основания
        /// </summary>
        public DateTime? BasisDocumentDate { get; set; } = null;

        /// <summary>
        /// Дополнительные сведения
        /// </summary>
        public string BasisDocumentOtherInfo { get; set; }

        /// <summary>
        /// Идентификатор документа - основания
        /// </summary>
        public string BasisDocumentId { get; set; }

        #endregion

        #region Сведения о лице, передавшем товар (имущество)

        public object PersonTransferedTheGoods { get; set; }

        #endregion

        #endregion
        #endregion

        #region Сведения о лице, подписывающем файл обмена счета-фактуры (информации продавца) в электронной форме

        /// <summary>
        /// Тип подписи
        /// </summary>
        public SignTypeEnum SignType { get; set; }

        /// <summary>
        /// Способ подтверждения полномочий представителя на подписание документа
        /// </summary>
        public MethodOfConfirmingAuthorityEnum MethodOfConfirmingAuthority { get; set; }

        /// <summary>
        /// Сведения о доверенности в электронной форме в машиночитаемом виде, используемой для подтверждения полномочий представителя
        /// </summary>
        public ElectronicPowerOfAttorney ElectronicPowerOfAttorney { get; set; }

        /// <summary>
        /// ИНН организации
        /// </summary>
        public string JuridicalInn { get; set; }

        /// <summary>
        /// Дата подписи
        /// </summary>
        public DateTime? SignDate { get; set; }

        /// <summary>
        /// Должность
        /// </summary>
        public string SignerPosition { get; set; }

        /// <summary>
        /// Фамилия
        /// </summary>
        public string SignerSurname { get; set; }

        /// <summary>
        /// Имя
        /// </summary>
        public string SignerName { get; set; }

        /// <summary>
        /// Отчество
        /// </summary>
        public string SignerPatronymic { get; set; }

        #endregion
        #endregion

        #region Parse Methods
        public void Parse(byte[] content)
        {
            var xmlString = Encoding.GetEncoding(1251).GetString(content);
            Parse(xmlString);
        }

        public void Parse(string content)
        {
            var xsdDocument = _xmlUtils.DeserializeString<Файл>(content);

            FileName = xsdDocument?.ИдФайл;
            EdoProgramVersion = xsdDocument?.ВерсПрог;

            var document = xsdDocument.Документ;
            if (document != null)
            {
                if (!(string.IsNullOrEmpty(document.ДатаИнфПр) || string.IsNullOrEmpty(document.ВремИнфПр)))
                    CreateDate = DateTime.ParseExact($"{document.ДатаИнфПр} {document.ВремИнфПр}", "dd.MM.yyyy HH.mm.ss", System.Globalization.CultureInfo.InvariantCulture);

                if (document.Функция == ФайлДокументФункция.СЧФ)
                    Function = "СЧФ";
                else if (document.Функция == ФайлДокументФункция.СЧФДОП)
                    Function = "СЧФДОП";
                else if (document.Функция == ФайлДокументФункция.ДОП)
                    Function = "ДОП";
                else if (document.Функция == ФайлДокументФункция.СвРК)
                    Function = "СвРК";
                else if (document.Функция == ФайлДокументФункция.СвЗК)
                    Function = "СвЗК";

                DocName = document.НаимДокОпр;
                DocNumber = document.СвСчФакт?.НомерДок;

                if (!string.IsNullOrEmpty(document.СвСчФакт?.ДатаДок))
                    DocDate = DateTime.ParseExact(document.СвСчФакт.ДатаДок, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);

                var goods = document.ТаблСчФакт?.СведТов ?? new ФайлДокументТаблСчФактСведТов[] { };
                Products = new List<Product>();

                foreach (var good in goods)
                {
                    var product = new Product()
                    {
                        Description = good.НаимТов,
                        Quantity = good.КолТов,
                        Price = good.ЦенаТов,
                        TaxAmount = good.СумНал?.Item as decimal?,
                        Subtotal = good.СтТовУчНал,
                        Number = Convert.ToInt32(good.НомСтр)
                    };

                    if (good?.ИнфПолФХЖ2 != null && (good?.ИнфПолФХЖ2?.Count() ?? 0) > 0)
                    {
                        var barCodeField = good.ИнфПолФХЖ2.FirstOrDefault(i => i.Идентиф == "Штрихкод");

                        if (barCodeField == null)
                            barCodeField = good.ИнфПолФХЖ2.FirstOrDefault(i => i.Идентиф == "штрихкод");

                        if (barCodeField != null)
                            product.BarCode = barCodeField.Значен;
                    }

                    if (!string.IsNullOrEmpty(good?.ДопСведТов?.КодТов))
                    {
                        if (string.IsNullOrEmpty(product.BarCode))
                        {
                            if (good.ДопСведТов.КодТов.StartsWith("0") && good.ДопСведТов.КодТов.Length == 14)
                                product.BarCode = good.ДопСведТов.КодТов.TrimStart('0');
                        }
                    }

                    product.MarkedCodes = new List<string>();
                    product.TransportPackingIdentificationCode = new List<string>();

                    if (good?.ДопСведТов?.НомСредИдентТов != null)
                    {
                        foreach (var code in good.ДопСведТов.НомСредИдентТов)
                        {
                            if (!string.IsNullOrEmpty(code.ИдентТрансУпак))
                                product.TransportPackingIdentificationCode.Add(code.ИдентТрансУпак);

                            if (code.Items != null)
                            {
                                product.MarkedCodes.AddRange(code.Items);

                                string markedCodeExample = code.Items?.FirstOrDefault();
                                if (string.IsNullOrEmpty(product.BarCode) && !string.IsNullOrEmpty(markedCodeExample))
                                    if (markedCodeExample.Length == 31)
                                        product.BarCode = markedCodeExample.Substring(0, 16).TrimStart('0', '1').TrimStart('0');
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(product.BarCode))
                        product.BarCode = good?.ДопСведТов?.КодТов;

                    Products.Add(product);
                }
            }
        }
        #endregion

        #region GetXmlContent
        public string GetXmlContent()
        {
            var xsdDocument = new Файл();

            xsdDocument.ИдФайл = FileName;
            xsdDocument.ВерсПрог = EdoProgramVersion;

            xsdDocument.Документ = new ФайлДокумент();
            xsdDocument.Документ.ДатаИнфПр = CreateDate.ToString("dd.MM.yyyy");
            xsdDocument.Документ.ВремИнфПр = CreateDate.ToString("HH.mm.ss");

            if (Function == "СЧФ")
                xsdDocument.Документ.Функция = ФайлДокументФункция.СЧФ;
            else if (Function == "СЧФДОП")
                xsdDocument.Документ.Функция = ФайлДокументФункция.СЧФДОП;
            else if (Function == "ДОП")
                xsdDocument.Документ.Функция = ФайлДокументФункция.ДОП;
            else if (Function == "СвРК")
                xsdDocument.Документ.Функция = ФайлДокументФункция.СвРК;
            else if (Function == "СвЗК")
                xsdDocument.Документ.Функция = ФайлДокументФункция.СвЗК;

            xsdDocument.Документ.НаимДокОпр = DocName;
            xsdDocument.Документ.ПоФактХЖ = EconomicLifeDocName;
            xsdDocument.Документ.НаимЭконСубСост = FinSubjectCreator;

            xsdDocument.Документ.СвСчФакт = new ФайлДокументСвСчФакт();
            xsdDocument.Документ.СвСчФакт.НомерДок = DocNumber;
            xsdDocument.Документ.СвСчФакт.ДатаДок = DocDate.ToString("dd.MM.yyyy");

            xsdDocument.Документ.СвСчФакт.СвПрод = new УчастникТип[1];

            if (SellerEntity != null)
            {
                xsdDocument.Документ.СвСчФакт.СвПрод[0] = GetParticipantType(SellerEntity);

                if (SellerAddress != null)
                    xsdDocument.Документ.СвСчФакт.СвПрод[0].Адрес = GetParticipantAddress(SellerAddress);
            }

            if (SameShipper)
            {
                xsdDocument.Документ.СвСчФакт.ГрузОт = new ФайлДокументСвСчФактГрузОт[] 
                {
                    new ФайлДокументСвСчФактГрузОт
                    {
                        Item = ФайлДокументСвСчФактГрузОтОнЖе.онже
                    }
                };
            }
            else if(ShipperEntity != null)
            {
                xsdDocument.Документ.СвСчФакт.ГрузОт = new ФайлДокументСвСчФактГрузОт[] 
                {
                    new ФайлДокументСвСчФактГрузОт
                    {
                        Item = GetParticipantType(ShipperEntity)
                    }
                };

                if(xsdDocument.Документ.СвСчФакт.ГрузОт[0]?.Item as УчастникТип != null && ShipperAddress != null)
                    (xsdDocument.Документ.СвСчФакт.ГрузОт[0].Item as УчастникТип).Адрес = GetParticipantAddress(ShipperAddress);
            }

            xsdDocument.Документ.СвСчФакт.СвПокуп = new УчастникТип[1];

            if (BuyerEntity != null)
            {
                xsdDocument.Документ.СвСчФакт.СвПокуп[0] = GetParticipantType(BuyerEntity);

                if (BuyerAddress != null)
                    xsdDocument.Документ.СвСчФакт.СвПокуп[0].Адрес = GetParticipantAddress(BuyerAddress);
            }

            if (ConsigneeEntity != null)
            {
                xsdDocument.Документ.СвСчФакт.ГрузПолуч = new УчастникТип[]
                {
                    GetParticipantType(ConsigneeEntity)
                };

                if(xsdDocument.Документ.СвСчФакт.ГрузПолуч[0] != null && ConsigneeAddress != null)
                    xsdDocument.Документ.СвСчФакт.ГрузПолуч[0].Адрес = GetParticipantAddress(ConsigneeAddress);
            }

            xsdDocument.Документ.СвСчФакт.ДенИзм = new ФайлДокументСвСчФактДенИзм();
            xsdDocument.Документ.СвСчФакт.ДенИзм.НаимОКВ = CurrencyName;
            xsdDocument.Документ.СвСчФакт.ДенИзм.КодОКВ = CurrencyCode;

            if (DeliveryDocuments.Count > 0)
                xsdDocument.Документ.СвСчФакт.ДокПодтвОтгрНом = DeliveryDocuments?
                    .Select(d => new РеквДокТип
                    {
                        РеквНаимДок = d.DocumentName,
                        РеквНомерДок = d.DocumentNumber,
                        РеквДатаДок = d.DocumentDate.ToString("dd.MM.yyyy")
                    })?.ToArray();

            if(AdditionalInfos != null)
            {
                if(AdditionalInfos.Count > 0)
                {
                    xsdDocument.Документ.СвСчФакт.ИнфПолФХЖ1 = new ФайлДокументСвСчФактИнфПолФХЖ1();

                    xsdDocument.Документ.СвСчФакт.ИнфПолФХЖ1.ТекстИнф = new ТекстИнфТип[AdditionalInfos.Count];

                    int i = 0;
                    foreach (var additionalInfo in AdditionalInfos)
                    {
                        xsdDocument.Документ.СвСчФакт.ИнфПолФХЖ1.ТекстИнф[i] = new ТекстИнфТип
                        {
                            Идентиф = additionalInfo.Key,
                            Значен = additionalInfo.Value
                        };
                        i++;
                    }
                }
            }

            xsdDocument.Документ.ТаблСчФакт = new ФайлДокументТаблСчФакт();

            if (Products != null && Products.Count > 0)
                xsdDocument.Документ.ТаблСчФакт.СведТов = Products?
                    .Select(p =>
                    {
                        decimal taxAmount = p.TaxAmount ?? 0;
                        decimal price = p.Price ?? 0;
                        decimal quantity = p.Quantity ?? 0;

                        var good = new ФайлДокументТаблСчФактСведТов
                        {
                            НомСтр = p.Number.ToString(),
                            НаимТов = p.Description,
                            НаимЕдИзм = p.UnitName,
                            ОКЕИ_Тов = p.UnitCode,
                            КолТов = quantity,
                            КолТовSpecified = true,
                            ЦенаТов = price,
                            ЦенаТовSpecified = true,
                            СтТовБезНДС = p.SubtotalWithVatExcluded ?? price * quantity,
                            СтТовБезНДСSpecified = true,
                            СумНал = new СумНДСТип()
                        };

                        if (p.Subtotal != null)
                        {
                            good.СтТовУчНал = p.Subtotal.Value;
                            good.СтТовУчНалSpecified = true;
                        }

                        if (taxAmount == 0)
                        {
                            good.СумНал.Item = new СумНДСТипБезНДС();
                            good.НалСт = ФайлДокументТаблСчФактСведТовНалСт.Item0;

                            if (p.Subtotal == null)
                            {
                                good.СтТовУчНал = good.СтТовБезНДС;
                                good.СтТовУчНалSpecified = true;
                            }
                        }
                        else
                        {
                            good.СумНал.Item = taxAmount;

                            if (p.Subtotal == null)
                            {
                                good.СтТовУчНал = good.СтТовБезНДС + taxAmount;
                                good.СтТовУчНалSpecified = true;
                            }

                            if (p.VatRate == 10)
                                good.НалСт = ФайлДокументТаблСчФактСведТовНалСт.Item10;
                            else if (p.VatRate == 20)
                                good.НалСт = ФайлДокументТаблСчФактСведТовНалСт.Item20;
                            else
                                good.НалСт = ФайлДокументТаблСчФактСведТовНалСт.НДСисчисляетсяналоговымагентом;

                        }

                        good.Акциз = new СумАкцизТип();
                        if (p.WithoutExcise)
                            good.Акциз.Item = СумАкцизТипБезАкциз.безакциза;
                        else
                            good.Акциз.Item = p.ExciseSumm;

                        if (!string.IsNullOrEmpty(p.CustomsDeclarationCode))
                        {
                            good.СвДТ = new ФайлДокументТаблСчФактСведТовСвДТ[1];
                            good.СвДТ[0] = new ФайлДокументТаблСчФактСведТовСвДТ();
                            good.СвДТ[0].КодПроисх = p.OriginCode;

                            if (p.OriginCode != codeOfRussia)
                                good.СвДТ[0].НомерДТ = p.CustomsDeclarationCode;
                        }

                        good.ДопСведТов = new ФайлДокументТаблСчФактСведТовДопСведТов();
                        good.ДопСведТов.КодТов = p.BarCode;
                        
                        if(!string.IsNullOrEmpty(p.OriginCountryName))
                            good.ДопСведТов.КрНаимСтрПр = new string[] { p.OriginCountryName };

                        if (p.MarkedCodes != null && p.MarkedCodes.Count > 0)
                        {
                            good.ДопСведТов.НомСредИдентТов = new ФайлДокументТаблСчФактСведТовДопСведТовНомСредИдентТов[1];
                            good.ДопСведТов.НомСредИдентТов[0] = new ФайлДокументТаблСчФактСведТовДопСведТовНомСредИдентТов();

                            good.ДопСведТов.НомСредИдентТов[0].Items = p.MarkedCodes.ToArray();
                            good.ДопСведТов.НомСредИдентТов[0].ItemsElementName = p.MarkedCodes.Select(m => ItemsChoiceType.КИЗ).ToArray();
                        }
                        else if (p.TransportPackingIdentificationCode != null && p.TransportPackingIdentificationCode.Count > 0)
                        {
                            good.ДопСведТов.НомСредИдентТов = p.TransportPackingIdentificationCode.Select(t => new ФайлДокументТаблСчФактСведТовДопСведТовНомСредИдентТов { ИдентТрансУпак = t }).ToArray();
                        }

                        if (p.AdditionalInfos.Count > 0)
                            good.ИнфПолФХЖ2 = new ТекстИнфТип[p.AdditionalInfos.Count];

                        int j = 0;
                        foreach(var additionalInfo in p.AdditionalInfos)
                        {
                            good.ИнфПолФХЖ2[j] = new ТекстИнфТип
                            {
                                Идентиф = additionalInfo.Key,
                                Значен = additionalInfo.Value
                            };
                            j++;
                        }

                        return good;
                    })?.ToArray();

            xsdDocument.Документ.ТаблСчФакт.ВсегоОпл = new ФайлДокументТаблСчФактВсегоОпл();
            xsdDocument.Документ.ТаблСчФакт.ВсегоОпл.СтТовБезНДСВсего = xsdDocument.Документ.ТаблСчФакт.СведТов.Sum(c => c.СтТовБезНДС);
            xsdDocument.Документ.ТаблСчФакт.ВсегоОпл.СтТовБезНДСВсегоSpecified = true;

            xsdDocument.Документ.ТаблСчФакт.ВсегоОпл.СтТовУчНалВсего = xsdDocument.Документ.ТаблСчФакт.СведТов.Sum(c => c.СтТовУчНал);
            xsdDocument.Документ.ТаблСчФакт.ВсегоОпл.СтТовУчНалВсегоSpecified = true;

            var taxAmountTolal = Products?.Sum(p => (p.TaxAmount ?? 0)) ?? 0;

            xsdDocument.Документ.ТаблСчФакт.ВсегоОпл.СумНалВсего = new СумНДСТип();

            if (taxAmountTolal == 0)
            {
                xsdDocument.Документ.ТаблСчФакт.ВсегоОпл.СумНалВсего.Item = СумНДСТипБезНДС.безНДС;
            }
            else
            {
                xsdDocument.Документ.ТаблСчФакт.ВсегоОпл.СумНалВсего.Item = taxAmountTolal;
            }

            if (!string.IsNullOrEmpty(ContentOperation))
            {
                xsdDocument.Документ.СвПродПер = new ФайлДокументСвПродПер();
                xsdDocument.Документ.СвПродПер.СвПер = new ФайлДокументСвПродПерСвПер();
                xsdDocument.Документ.СвПродПер.СвПер.СодОпер = ContentOperation;
                xsdDocument.Документ.СвПродПер.СвПер.ВидОпер = OperationType;

                if (ShippingDate != null)
                    xsdDocument.Документ.СвПродПер.СвПер.ДатаПер = ShippingDate.Value.ToString("dd.MM.yyyy");

                if (!string.IsNullOrEmpty(BasisDocumentName))
                {
                    var basisDocumentType = new РеквДокТип();

                    basisDocumentType.РеквНаимДок = BasisDocumentName;
                    basisDocumentType.РеквНомерДок = BasisDocumentNumber;
                    basisDocumentType.РеквДопСведДок = BasisDocumentOtherInfo;
                    basisDocumentType.РеквИдДок = BasisDocumentId;

                    if (BasisDocumentDate != null)
                        basisDocumentType.РеквДатаДок = BasisDocumentDate.Value.ToString("dd.MM.yyyy");

                    xsdDocument.Документ.СвПродПер.СвПер.Items = new object[]
                    {
                        basisDocumentType
                    };
                }
                else
                {
                    xsdDocument.Документ.СвПродПер.СвПер.Items = new object[] 
                    {
                        new ФайлДокументСвПродПерСвПерБезДокОснПер()
                    };
                }

                if(PersonTransferedTheGoods as OrganizationEmployee != null)
                {
                    var organizationEmployee = PersonTransferedTheGoods as OrganizationEmployee;
                    xsdDocument.Документ.СвПродПер.СвПер.СвЛицПер = new ФайлДокументСвПродПерСвПерСвЛицПер
                    {
                        Item = new ФайлДокументСвПродПерСвПерСвЛицПерРабОргПрод
                        {
                            Должность = organizationEmployee.Position,
                            ИныеСвед = organizationEmployee.OtherInfo,
                            ФИО = new ФИОТип
                            {
                                Фамилия = organizationEmployee.Surname,
                                Имя = organizationEmployee.Name,
                                Отчество = organizationEmployee.Patronymic
                            }
                        }
                    };
                }
                else if (PersonTransferedTheGoods as AnotherPerson != null)
                {
                    var anotherPerson = PersonTransferedTheGoods as AnotherPerson;

                    xsdDocument.Документ.СвПродПер.СвПер.СвЛицПер = new ФайлДокументСвПродПерСвПерСвЛицПер
                    {
                        Item = new ФайлДокументСвПродПерСвПерСвЛицПерИнЛицо()
                    };
                    var anotherPersonItem = xsdDocument.Документ.СвПродПер.СвПер.СвЛицПер.Item as ФайлДокументСвПродПерСвПерСвЛицПерИнЛицо;

                    if(anotherPerson.Item as OrganizationRepresentative != null)
                    {
                        var organizationRepresentative = anotherPerson.Item as OrganizationRepresentative;

                        anotherPersonItem.Item = new ФайлДокументСвПродПерСвПерСвЛицПерИнЛицоПредОргПер
                        {
                            Должность = organizationRepresentative.Position,
                            ИныеСвед = organizationRepresentative.OtherInfo,
                            ИННЮЛПер = organizationRepresentative.OrgInn,
                            НаимОргПер = organizationRepresentative.OrgName,
                            ОснДоверОргПер = GetDocumentData(organizationRepresentative.ReasonOrgTrustDocument),
                            ОснПолнПредПер = GetDocumentData(organizationRepresentative.ReasonTrustPersonDocument),
                            ФИО = new ФИОТип
                            {
                                Фамилия = organizationRepresentative.Surname,
                                Имя = organizationRepresentative.Name,
                                Отчество = organizationRepresentative.Patronymic
                            }
                        };
                    }
                    else if (anotherPerson.Item as TrustedIndividual != null)
                    {
                        var trustedIndividual = anotherPerson.Item as TrustedIndividual;

                        anotherPersonItem.Item = new ФайлДокументСвПродПерСвПерСвЛицПерИнЛицоФЛПер
                        {
                            ИННФЛПер = trustedIndividual.PersonInn,
                            ИныеСвед = trustedIndividual.OtherInfo,
                            ОснДоверФЛ = GetDocumentData(trustedIndividual.ReasonOfTrustDocument),
                            ФИО = new ФИОТип
                            {
                                Фамилия = trustedIndividual.Surname,
                                Имя = trustedIndividual.Name,
                                Отчество = trustedIndividual.Patronymic
                            }
                        };
                    }
                }
            }

            xsdDocument.Документ.Подписант = new ФайлДокументПодписант[]
            {
                new ФайлДокументПодписант
                {
                    Должн = SignerPosition,
                    ДатаПодДок = SignDate?.ToString("dd.MM.yyyy")
                }
            };

            var signer = xsdDocument.Документ.Подписант[0];

            if (MethodOfConfirmingAuthority == MethodOfConfirmingAuthorityEnum.DigitalSignature)
                signer.СпосПодтПолном = ФайлДокументПодписантСпосПодтПолном.Item1;
            else if (MethodOfConfirmingAuthority == MethodOfConfirmingAuthorityEnum.EmchdInPackageOfElectronicDocuments)
                signer.СпосПодтПолном = ФайлДокументПодписантСпосПодтПолном.Item2;
            else if (MethodOfConfirmingAuthority == MethodOfConfirmingAuthorityEnum.EmchdDataInDocument)
            {
                signer.СпосПодтПолном = ФайлДокументПодписантСпосПодтПолном.Item3;

                if (ElectronicPowerOfAttorney == null)
                    throw new Exception("Не указана машиночитаемая доверенность.");

                signer.Items = new[] 
                {
                    new ФайлДокументПодписантСвДоверЭл
                    {
                        НомДовер = ElectronicPowerOfAttorney.RegistrationNumber,
                        ДатаВыдДовер = ElectronicPowerOfAttorney.RegistrationDate.ToString("dd.MM.yyyy"),
                        ИдСистХран = ElectronicPowerOfAttorney.SystemIdentificationInfo,
                        УРЛСист = ElectronicPowerOfAttorney.UrlSystem
                    }
                };
            }
            else if (MethodOfConfirmingAuthority == MethodOfConfirmingAuthorityEnum.EmchdDataInOtherSystem)
                signer.СпосПодтПолном = ФайлДокументПодписантСпосПодтПолном.Item4;
            else if (MethodOfConfirmingAuthority == MethodOfConfirmingAuthorityEnum.PaperPowerOfAttorney)
                signer.СпосПодтПолном = ФайлДокументПодписантСпосПодтПолном.Item5;
            else if (MethodOfConfirmingAuthority == MethodOfConfirmingAuthorityEnum.Other)
                signer.СпосПодтПолном = ФайлДокументПодписантСпосПодтПолном.Item6;

            if (SignType == SignTypeEnum.QualifiedElectronicDigitalSignature)
                signer.ТипПодпис = ФайлДокументПодписантТипПодпис.Item1;
            else if(SignType == SignTypeEnum.SimpleElectronicDigitalSignature)
                signer.ТипПодпис = ФайлДокументПодписантТипПодпис.Item2;
            else if (SignType == SignTypeEnum.NonQualifiedElectronicDigitalSignature)
                signer.ТипПодпис = ФайлДокументПодписантТипПодпис.Item3;

            signer.ФИО = new ФИОТип
            {
                Фамилия = SignerSurname,
                Имя = SignerName,
                Отчество = SignerPatronymic
            };

            string xml = _xmlUtils.SerializeObject<Файл>(xsdDocument, Encoding.GetEncoding(1251));
            return $"<?xml version=\"1.0\" encoding=\"windows-1251\"?>{xml}";
        }
        #endregion

        private РеквДокТип GetDocumentData(DocumentDetails documentDetails)
        {
            РеквДокТип docData = null;

            if (documentDetails != null)
            {
                docData = new РеквДокТип
                {
                    РеквНаимДок = documentDetails.DocumentName,
                    РеквНомерДок = documentDetails.DocumentNumber ?? "Без номера",
                    РеквДатаДок = documentDetails.DocumentDate.ToString("dd.MM.yyyy"),
                    РеквИдФайлДок = documentDetails.FirstDocumentId,
                    РеквИдДок = documentDetails.DocumentId,
                    РИдСистХранД = documentDetails.DocumentSystemSavingId,
                    РеквУРЛСистДок = documentDetails.UrlAddress,
                    РеквДопСведДок = documentDetails.DocumentOtherInfo
                };

                if (documentDetails.FinSubjectCreators != null)
                {
                    docData.РеквИдРекСост = new ИдРекСостТип[documentDetails.FinSubjectCreators.Count()];

                    if (documentDetails.FinSubjectCreators.Count() > 0)
                    {
                        for (int i = 0; i < documentDetails.FinSubjectCreators.Count(); i++)
                        {
                            docData.РеквИдРекСост[i] = new ИдРекСостТип();

                            if (documentDetails?.FinSubjectCreators[i]?.ForeignOrganizationData != null)
                            {
                                var item = new СвИнНеУчТип
                                {
                                    КодСтр = documentDetails.FinSubjectCreators[i].ForeignOrganizationData?.CountryCode,
                                    НаимСтран = documentDetails.FinSubjectCreators[i].ForeignOrganizationData?.CountryName,
                                    Наим = documentDetails.FinSubjectCreators[i].ForeignOrganizationData?.Name,
                                    Идентиф = documentDetails.FinSubjectCreators[i].ForeignOrganizationData?.IdentificationInfo,
                                    ИныеСвед = documentDetails.FinSubjectCreators[i].ForeignOrganizationData?.OtherInfo
                                };

                                if (documentDetails.FinSubjectCreators[i].ForeignOrganizationData?.StatusId == "ИО")
                                {
                                    item.ИдСтат = СвИнНеУчТипИдСтат.ИО;
                                }
                                else if (documentDetails.FinSubjectCreators[i].ForeignOrganizationData?.StatusId == "ИГ")
                                {
                                    item.ИдСтат = СвИнНеУчТипИдСтат.ИГ;
                                }

                                docData.РеквИдРекСост[i].Item = item;
                                docData.РеквИдРекСост[i].ItemElementName = ItemChoiceType.ДаннИно;
                            }
                            else if (!string.IsNullOrEmpty(documentDetails?.FinSubjectCreators[i]?.JuridicalEntityInn))
                            {
                                docData.РеквИдРекСост[i].ItemElementName = ItemChoiceType.ИННЮЛ;
                                docData.РеквИдРекСост[i].Item = documentDetails.FinSubjectCreators[i].JuridicalEntityInn;
                            }
                            else if (!string.IsNullOrEmpty(documentDetails?.FinSubjectCreators[i]?.PersonInn))
                            {
                                docData.РеквИдРекСост[i].ItemElementName = ItemChoiceType.ИННФЛ;
                                docData.РеквИдРекСост[i].Item = documentDetails.FinSubjectCreators[i].PersonInn;
                            }
                            else if (!string.IsNullOrEmpty(documentDetails?.FinSubjectCreators[i]?.ExecutiveAuthorityOrganization))
                            {
                                docData.РеквИдРекСост[i].ItemElementName = ItemChoiceType.НаимОИВ;
                                docData.РеквИдРекСост[i].Item = documentDetails.FinSubjectCreators[i].ExecutiveAuthorityOrganization;
                            }
                        }
                    }
                }
            }

            return docData;
        }

        private УчастникТип GetParticipantType<T>(T participantObj) where T :class, new()
        {
            var participantXml = new УчастникТип();

            if (participantObj as OrganizationExchangeParticipantEntity != null)
            {
                participantXml.ИдСв = new УчастникТипИдСв();

                var organizationExchangeParticipant = participantObj as OrganizationExchangeParticipantEntity;

                var item = new УчастникТипИдСвСвЮЛУч();
                item.ИННЮЛ = organizationExchangeParticipant.JuridicalInn;
                item.КПП = organizationExchangeParticipant.JuridicalKpp;
                item.НаимОрг = organizationExchangeParticipant.OrgName;

                participantXml.ИдСв.Item = item;
            }
            else if(participantObj as IndividualEntity != null)
            {
                participantXml.ИдСв = new УчастникТипИдСв();

                var individualEntity = participantObj as IndividualEntity;

                var item = new УчастникТипИдСвСвФЛУч();
                item.ИННФЛ = individualEntity.Inn;
                item.ФИО = new ФИОТип
                {
                    Фамилия = individualEntity.Surname,
                    Имя = individualEntity.Name,
                    Отчество = individualEntity.Patronymic
                };
                item.ИныеСвед = individualEntity.OtherInfo;
                participantXml.ИдСв.Item = item;
            }
            else if(participantObj as JuridicalEntity != null)
            {
                participantXml.ИдСв = new УчастникТипИдСв();

                var juridicalEntity = participantObj as JuridicalEntity;

                var item = new УчастникТипИдСвСвИП();
                item.ИННФЛ = juridicalEntity.Inn;
                item.ИныеСвед = juridicalEntity.OtherInfo;
                item.СвГосРегИП = juridicalEntity.CertificateOfFederalRegistration;
                item.ФИО = new ФИОТип
                {
                    Фамилия = juridicalEntity.Surname,
                    Имя = juridicalEntity.Name,
                    Отчество = juridicalEntity.Patronymic
                };
                participantXml.ИдСв.Item = item;
            }

            return participantXml;
        }

        private АдресТип GetParticipantAddress(Address addressObj)
        {
            var addressXml = new АдресТип();

            if (addressObj?.CountryCode == codeOfRussia && string.IsNullOrEmpty(addressObj?.ForeignTextAddress))
            {
                var russianAddress = new АдрРФТип();
                russianAddress.КодРегион = addressObj.RussianRegionCode;
                russianAddress.НаимРегион = addressObj.RussianRegionName;

                if (!string.IsNullOrEmpty(addressObj.RussianCity))
                    russianAddress.Город = addressObj.RussianCity;

                if (!string.IsNullOrEmpty(addressObj.RussianIndex))
                    russianAddress.Индекс = addressObj.RussianIndex;

                if (!string.IsNullOrEmpty(addressObj.RussianStreet))
                    russianAddress.Улица = addressObj.RussianStreet;

                if (!string.IsNullOrEmpty(addressObj.RussianHouse))
                    russianAddress.Дом = addressObj.RussianHouse;

                if (!string.IsNullOrEmpty(addressObj.RussianFlat))
                    russianAddress.Кварт = addressObj.RussianFlat;

                addressXml.Item = russianAddress;
            }
            else if (addressObj != null)
            {
                var foreignAddress = new АдрИнфТип();
                foreignAddress.КодСтр = addressObj.CountryCode;
                foreignAddress.НаимСтран = addressObj.CountryName;
                foreignAddress.АдрТекст = addressObj.ForeignTextAddress;

                addressXml.Item = foreignAddress;
            }

            return addressXml;
        }
    }
}
