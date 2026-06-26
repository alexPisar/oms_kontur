using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilitesLibrary.Service;
using Reporter.Enums;
using Reporter.Entities;
using Reporter.XsdClasses.OnNschfdoppokUtd970;

namespace Reporter.Reports
{
    public class UniversalTransferBuyerDocumentUtd970 : IReport
    {
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

        #region Документ об отгрузке товаров (выполнении работ), передаче имущественных прав (документ об оказании услуг), включающий в себя счет-фактуру(информация покупателя), или документ об отгрузке товаров(выполнении работ), передаче имущественных прав(документ об оказании услуг) (информация покупателя)

        /// <summary>
        /// Код документа  по КНД
        /// </summary>
        public const string KND = "1115132";

        /// <summary>
        /// Дата и время формирования файла обмена информации покупателя
        /// </summary>
        public DateTime CreateBuyerFileDate { get; set; }

        /// <summary>
        /// Наименование экономического субъекта – составителя файла обмена информации покупателя
        /// </summary>
        public string FinSubjectCreator { get; set; }

        /// <summary>
        /// Основание, по которому экономический субъект является составителем файла обмена счета-фактуры (информации продавца)
        /// </summary>
        public DocumentDetails ReasonOfCreateFile { get; set; }

        #region Идентификация файла обмена счета-фактуры (информации продавца) или файла обмена информации продавца

        /// <summary>
        /// Идентификатор файла обмена информации продавца
        /// </summary>
        public string SellerFileId { get; set; }

        /// <summary>
        /// Дата и время формирования файла обмена информации продавца
        /// </summary>
        public DateTime CreateSellerFileDate { get; set; }

        /// <summary>
        /// Электронная подпись файла обмена информации продавца
        /// </summary>
        public string Signature { get; set; }

        #endregion

        #region Содержание факта хозяйственной жизни 4 - сведения о принятии товаров (результатов выполненных работ), имущественных прав (о подтверждении факта оказания услуг)

        /// <summary>
        /// Наименование первичного документа, согласованное сторонами сделки 
        /// </summary>
        public string DocName { get; set; }

        /// <summary>
        /// Функция
        /// </summary>
        public string Function { get; set; }

        /// <summary>
        /// Порядковый номер (строка 1 счета-фактуры), документа об отгрузке товаров (выполнении работ), передаче имущественных прав (документа об оказании услуг)
        /// </summary>
        public string SellerInvoiceNumber { get; set; }

        /// <summary>
        /// Дата документа об отгрузке товаров (выполнении работ), передаче имущественных прав (документа об оказании услуг)
        /// </summary>
        public DateTime SellerInvoiceDate { get; set; }

        /// <summary>
        /// Вид операции
        /// </summary>
        public string OperationType { get; set; }

        #region Сведения о принятии товаров (результатов выполненных работ), имущественных прав (о подтверждении факта оказания услуг)

        /// <summary>
        /// Содержание операции (текст)
        /// </summary>
        public string ContentOperationText { get; set; }

        /// <summary>
        /// Дата принятия товаров (результатов выполненных работ), имущественных прав (подтверждения факта оказания услуг)
        /// </summary>
        public DateTime DateReceive { get; set; }

        #region Содержание операции
        /// <summary>
        /// Код итога приёмки товара
        /// </summary>
        public AcceptResultEnum AcceptResult { get; set; }
        /*
        /// <summary>
        /// Наименование документа, оформляющего расхождения
        /// </summary>
        public string DocumentDiscrepancyName { get; set; }

        /// <summary>
        /// Код вида документа о расхождениях
        /// </summary>
        public DocumentDiscrepancyTypeEnum DocumentDiscrepancyType { get; set; }

        /// <summary>
        /// Номер документа покупателя о расхождениях
        /// </summary>
        public string DocumentDiscrepancyNumber { get; set; }

        /// <summary>
        /// Дата документа о расхождениях
        /// </summary>
        public DateTime? DocumentDiscrepancyDate { get; set; } = null;

        /// <summary>
        /// Идентификатор файла обмена документа о расхождениях, сформированного покупателем
        /// </summary>
        public string IdDocumentDiscrepancy { get; set; }*/

        public DocumentDetails DiscrepancyDocument { get; set; }
        #endregion

        #region Сведения о лице, принявшем товары (груз)
        /// <summary>
        /// Работник организации покупателя или иное лицо
        /// </summary>
        public List<object> OrganizationEmployeeOrAnotherPerson { get; set; }
        #endregion
        #endregion

        #region Информационное поле факта хозяйственной жизни 4

        /// <summary>
        /// Идентификатор  файла информационного поля
        /// </summary>
        public string FileInformationFieldId { get; set; }

        /// <summary>
        /// Идентификатор текстовой информации
        /// </summary>
        public string TextInformationId { get; set; }

        /// <summary>
        /// Значение текстовой информации
        /// </summary>
        public string TextInformation { get; set; }

        #endregion
        #endregion

        #region Подписант

        /// <summary>
        /// Подписант
        /// </summary>
        public List<object> SignerInfoListObj { get; set; }
        public SignerInfo SignerInfo
        {
            get {
                return SignerInfoListObj?.FirstOrDefault() as SignerInfo;
            }

            set {
                SignerInfoListObj = new List<object>(new[] { value });
            }
        }
        #endregion
        #endregion
        #endregion

        #region Parse Methods
        public void Parse(string content)
        {
            var xsdDocument = Xml.DeserializeEntity<Файл>(content);

            FileName = xsdDocument?.ИдФайл;
            EdoProgramVersion = xsdDocument?.ВерсПрог;

            var document = xsdDocument.Документ;

            if (!string.IsNullOrEmpty(document.ДатаИнфПок))
                CreateBuyerFileDate = DateTime.ParseExact($"{document.ДатаИнфПок} {document.ВремИнфПок}", "dd.MM.yyyy HH.mm.ss", System.Globalization.CultureInfo.InvariantCulture);

            FinSubjectCreator = document.НаимЭконСубСост;
            ReasonOfCreateFile = GetDocumentDetails(document.ОснДоверОргСост);

            var sellerInfo = document.ИдИнфПрод;

            if (sellerInfo != null)
            {
                SellerFileId = sellerInfo.ИдФайлИнфПр;
                Signature = sellerInfo.ЭП?.FirstOrDefault();

                if (!string.IsNullOrEmpty(sellerInfo.ДатаФайлИнфПр))
                    CreateSellerFileDate = DateTime.ParseExact($"{sellerInfo.ДатаФайлИнфПр} {sellerInfo.ВремФайлИнфПр}", "dd.MM.yyyy HH.mm.ss", System.Globalization.CultureInfo.InvariantCulture);
            }

            var contentOfEconomicLife = document?.СодФХЖ4;

            if (contentOfEconomicLife == null)
                return;

            DocName = contentOfEconomicLife.НаимДокОпрПр;
            Function = contentOfEconomicLife.Функция;
            SellerInvoiceNumber = contentOfEconomicLife.ПорНомДокИнфПр;
            OperationType = contentOfEconomicLife.ВидОпер;

            FileInformationFieldId = contentOfEconomicLife.ИнфПолФХЖ4?.ИдФайлИнфПол;
            TextInformationId = contentOfEconomicLife.ИнфПолФХЖ4?.ТекстИнф?.FirstOrDefault()?.Идентиф;
            TextInformation = contentOfEconomicLife.ИнфПолФХЖ4?.ТекстИнф?.FirstOrDefault()?.Значен;

            if (!string.IsNullOrEmpty(document.СодФХЖ4.ДатаДокИнфПр))
                SellerInvoiceDate = DateTime.ParseExact(document.СодФХЖ4.ДатаДокИнфПр, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);

            if (!string.IsNullOrEmpty(contentOfEconomicLife.СвПрин?.СодОпер))
                ContentOperationText = contentOfEconomicLife.СвПрин.СодОпер;

            if (!string.IsNullOrEmpty(contentOfEconomicLife.СвПрин?.ДатаПрин))
                DateReceive = DateTime.ParseExact(contentOfEconomicLife.СвПрин.ДатаПрин, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);

            var contentOperation = contentOfEconomicLife.СвПрин?.КодСодОпер;
            if (contentOperation != null)
            {
                if (contentOperation.КодИтога == ФайлДокументСодФХЖ4СвПринКодСодОперКодИтога.Item1)
                    AcceptResult = AcceptResultEnum.GoodsAcceptedWithoutDiscrepancy;
                else if (contentOperation.КодИтога == ФайлДокументСодФХЖ4СвПринКодСодОперКодИтога.Item2)
                    AcceptResult = AcceptResultEnum.GoodsAcceptedWithDiscrepancy;
                else if (contentOperation.КодИтога == ФайлДокументСодФХЖ4СвПринКодСодОперКодИтога.Item3)
                    AcceptResult = AcceptResultEnum.GoodsNotAccepted;
            }

            DiscrepancyDocument = GetDocumentDetails(document.СодФХЖ4.СвПрин.КодСодОпер.РеквДокРасх);

            var personInfo = contentOfEconomicLife.СвПрин?.СвЛицПрин;

            if (personInfo != null)
            {
                if (personInfo.Item.GetType() == typeof(ФайлДокументСодФХЖ4СвПринСвЛицПринРабОргПок))
                {
                    var orgPersonInfo = (ФайлДокументСодФХЖ4СвПринСвЛицПринРабОргПок)personInfo.Item;
                    OrganizationEmployeeOrAnotherPerson = new List<object>(new object[] { new OrganizationEmployee() });
                    ((OrganizationEmployee)OrganizationEmployeeOrAnotherPerson.First()).Position = orgPersonInfo.Должность;
                    ((OrganizationEmployee)OrganizationEmployeeOrAnotherPerson.First()).OtherInfo = orgPersonInfo.ИныеСвед;
                    ((OrganizationEmployee)OrganizationEmployeeOrAnotherPerson.First()).Surname = orgPersonInfo.ФИО?.Фамилия;
                    ((OrganizationEmployee)OrganizationEmployeeOrAnotherPerson.First()).Name = orgPersonInfo.ФИО?.Имя;
                    ((OrganizationEmployee)OrganizationEmployeeOrAnotherPerson.First()).Patronymic = orgPersonInfo.ФИО?.Отчество;
                }
                else if (personInfo.Item.GetType() == typeof(ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицо))
                {
                    var otherPerson = (ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицо)personInfo.Item;
                    OrganizationEmployeeOrAnotherPerson = new List<object>(new object[] { new AnotherPerson() });

                    if (otherPerson.Item?.GetType() == typeof(ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицоФЛПрин))
                    {
                        var otherIndividualPerson = (ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицоФЛПрин)otherPerson.Item;
                        var trustedIndividual = new TrustedIndividual();
                        trustedIndividual.ReasonOfTrustDocument = GetDocumentDetails(otherIndividualPerson.ОснДоверФЛ);
                        trustedIndividual.OtherInfo = otherIndividualPerson.ИныеСвед;
                        trustedIndividual.Surname = otherIndividualPerson?.ФИО?.Фамилия;
                        trustedIndividual.Name = otherIndividualPerson?.ФИО?.Имя;
                        trustedIndividual.Patronymic = otherIndividualPerson?.ФИО?.Отчество;
                        trustedIndividual.PersonInn = otherIndividualPerson?.ИННФЛПрин;
                        ((AnotherPerson)OrganizationEmployeeOrAnotherPerson.First()).Item = trustedIndividual;
                    }
                    else if (otherPerson.Item?.GetType() == typeof(ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицоПредОргПрин))
                    {
                        var otherOrgPerson = (ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицоПредОргПрин)otherPerson.Item;
                        var organizationRepresentative = new OrganizationRepresentative();
                        organizationRepresentative.OrgName = otherOrgPerson.НаимОргПрин;
                        organizationRepresentative.Position = otherOrgPerson.Должность;
                        organizationRepresentative.OtherInfo = otherOrgPerson.ИныеСвед;
                        organizationRepresentative.OrgInn = otherOrgPerson.ИННОргПрин;
                        organizationRepresentative.ReasonOrgTrustDocument = GetDocumentDetails(otherOrgPerson.ОснДоверОргПрин);
                        organizationRepresentative.ReasonTrustPersonDocument = GetDocumentDetails(otherOrgPerson.ОснПолнПредПрин);
                        organizationRepresentative.Surname = otherOrgPerson.ФИО?.Фамилия;
                        organizationRepresentative.Name = otherOrgPerson.ФИО?.Имя;
                        organizationRepresentative.Patronymic = otherOrgPerson.ФИО?.Отчество;
                        ((AnotherPerson)OrganizationEmployeeOrAnotherPerson.First()).Item = organizationRepresentative;
                    }
                }
            }
            
            var signerInfo = document?.Подписант?.FirstOrDefault();
            if (signerInfo != null)
            {
                SignerInfo = new SignerInfo
                {
                    Position = signerInfo.Должн,
                    SignDate = DateTime.ParseExact(signerInfo.ДатаПодДок, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture),
                    OtherInfo = signerInfo.ДопСведПодп,
                    Surname = signerInfo?.ФИО?.Фамилия,
                    Name = signerInfo?.ФИО?.Имя,
                    Patronymic = signerInfo?.ФИО.Отчество
                };

                if (signerInfo.ТипПодпис == ФайлДокументПодписантТипПодпис.Item1)
                {
                    SignerInfo.SignType = SignTypeEnum.QualifiedElectronicDigitalSignature;
                }
                else if (signerInfo.ТипПодпис == ФайлДокументПодписантТипПодпис.Item2)
                {
                    SignerInfo.SignType = SignTypeEnum.SimpleElectronicDigitalSignature;
                }
                else if (signerInfo.ТипПодпис == ФайлДокументПодписантТипПодпис.Item3)
                {
                    SignerInfo.SignType = SignTypeEnum.NonQualifiedElectronicDigitalSignature;
                }

                if (signerInfo.СпосПодтПолном == ФайлДокументПодписантСпосПодтПолном.Item1)
                {
                    SignerInfo.MethodOfConfirmingAuthorityEnum = MethodOfConfirmingAuthorityEnum.DigitalSignature;
                }
                else if (signerInfo.СпосПодтПолном == ФайлДокументПодписантСпосПодтПолном.Item2)
                {
                    SignerInfo.MethodOfConfirmingAuthorityEnum = MethodOfConfirmingAuthorityEnum.EmchdInPackageOfElectronicDocuments;
                }
                else if (signerInfo.СпосПодтПолном == ФайлДокументПодписантСпосПодтПолном.Item3)
                {
                    SignerInfo.MethodOfConfirmingAuthorityEnum = MethodOfConfirmingAuthorityEnum.EmchdDataInDocument;

                    var item = signerInfo.Items.FirstOrDefault() as ФайлДокументПодписантСвДоверЭл;
                    SignerInfo.ElectronicPowerOfAttorney = new ElectronicPowerOfAttorney
                    {
                        RegistrationNumber = item?.НомДовер,
                        RegistrationDate = DateTime.ParseExact(item?.ДатаВыдДовер, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture),
                        SystemIdentificationInfo = item?.ИдСистХран,
                        UrlSystem = item?.УРЛСист
                    };
                }
                else if (signerInfo.СпосПодтПолном == ФайлДокументПодписантСпосПодтПолном.Item4)
                {
                    SignerInfo.MethodOfConfirmingAuthorityEnum = MethodOfConfirmingAuthorityEnum.EmchdDataInOtherSystem;
                }
                else if (signerInfo.СпосПодтПолном == ФайлДокументПодписантСпосПодтПолном.Item5)
                {
                    SignerInfo.MethodOfConfirmingAuthorityEnum = MethodOfConfirmingAuthorityEnum.PaperPowerOfAttorney;

                    var item = signerInfo.Items.FirstOrDefault() as ФайлДокументПодписантСвДоверБум;
                    SignerInfo.PaperPowerOfAttorney = new PaperPowerOfAttorney
                    {
                        InternalNumber = item?.ВнНомДовер,
                        Date = DateTime.ParseExact(item?.ДатаВыдДовер, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture),
                        IdentificationInfo = item?.СвИдДовер,
                        Surname = item?.ФИО?.Фамилия,
                        Name = item?.ФИО?.Имя,
                        Patronymic = item?.ФИО?.Отчество
                    };
                }
                else if (signerInfo.СпосПодтПолном == ФайлДокументПодписантСпосПодтПолном.Item6)
                {
                    SignerInfo.MethodOfConfirmingAuthorityEnum = MethodOfConfirmingAuthorityEnum.Other;
                }
            }
        }

        public void Parse(byte[] content)
        {
            var xmlString = Encoding.GetEncoding(1251).GetString(content);
            Parse(xmlString);
        }
        #endregion

        #region GetXmlContentMethods

        public string GetXmlContent()
        {
            var xsdDocument = new Файл();

            xsdDocument.ИдФайл = FileName;
            xsdDocument.ВерсПрог = EdoProgramVersion;

            xsdDocument.Документ = new ФайлДокумент();
            var document = xsdDocument.Документ;

            //Сведения об участниках документооборота
            //xsdDocument.СвУчДокОбор = new ФайлСвУчДокОбор();
            //xsdDocument.СвУчДокОбор.СвОЭДОтпр = new ФайлСвУчДокОборСвОЭДОтпр();
            //xsdDocument.СвУчДокОбор.СвОЭДОтпр.НаимОрг = EdoProviderOrgName;
            //xsdDocument.СвУчДокОбор.СвОЭДОтпр.ИННЮЛ = ProviderInn;
            //xsdDocument.СвУчДокОбор.СвОЭДОтпр.ИдЭДО = EdoId;
            //xsdDocument.СвУчДокОбор.ИдОтпр = SenderEdoId;
            //xsdDocument.СвУчДокОбор.ИдПол = ReceiverEdoId;

            //xsdDocument.ИнфПок = new ФайлИнфПок();
            document.ВремИнфПок = CreateBuyerFileDate.ToString("HH.mm.ss");
            document.ДатаИнфПок = CreateBuyerFileDate.ToString("dd.MM.yyyy");
            document.НаимЭконСубСост = FinSubjectCreator;

            document.ОснДоверОргСост = GetDocumentData(ReasonOfCreateFile);

            document.ИдИнфПрод = new ФайлДокументИдИнфПрод();
            document.ИдИнфПрод.ИдФайлИнфПр = SellerFileId;
            document.ИдИнфПрод.ВремФайлИнфПр = CreateSellerFileDate.ToString("HH.mm.ss");
            document.ИдИнфПрод.ДатаФайлИнфПр = CreateSellerFileDate.ToString("dd.MM.yyyy");
            document.ИдИнфПрод.ЭП = new string[] { Signature };

            document.СодФХЖ4 = new ФайлДокументСодФХЖ4();
            document.СодФХЖ4.НаимДокОпрПр = DocName;
            document.СодФХЖ4.Функция = Function;
            document.СодФХЖ4.ПорНомДокИнфПр = SellerInvoiceNumber;
            document.СодФХЖ4.ДатаДокИнфПр = SellerInvoiceDate.ToString("dd.MM.yyyy");
            document.СодФХЖ4.ВидОпер = OperationType;

            document.СодФХЖ4.СвПрин = new ФайлДокументСодФХЖ4СвПрин();
            document.СодФХЖ4.СвПрин.СодОпер = ContentOperationText;
            document.СодФХЖ4.СвПрин.ДатаПрин = DateReceive.ToString("dd.MM.yyyy");

            document.СодФХЖ4.СвПрин.КодСодОпер = new ФайлДокументСодФХЖ4СвПринКодСодОпер();
            if (AcceptResult == AcceptResultEnum.GoodsAcceptedWithoutDiscrepancy)
                document.СодФХЖ4.СвПрин.КодСодОпер.КодИтога = ФайлДокументСодФХЖ4СвПринКодСодОперКодИтога.Item1;
            else if (AcceptResult == AcceptResultEnum.GoodsAcceptedWithDiscrepancy)
                document.СодФХЖ4.СвПрин.КодСодОпер.КодИтога = ФайлДокументСодФХЖ4СвПринКодСодОперКодИтога.Item2;
            else if (AcceptResult == AcceptResultEnum.GoodsNotAccepted)
                document.СодФХЖ4.СвПрин.КодСодОпер.КодИтога = ФайлДокументСодФХЖ4СвПринКодСодОперКодИтога.Item3;

            /*document.СодФХЖ4.СвПрин.КодСодОпер.НаимДокРасх = DocumentDiscrepancyName;
            if (DocumentDiscrepancyType == DocumentDiscrepancyTypeEnum.DocumentWithDiscrepancy)
                document.СодФХЖ4.СвПрин.КодСодОпер.ВидДокРасх = ФайлИнфПокСодФХЖ4СвПринКодСодОперВидДокРасх.Item2;
            else if (DocumentDiscrepancyType == DocumentDiscrepancyTypeEnum.DocumentAboutDiscrepancy)
                document.СодФХЖ4.СвПрин.КодСодОпер.ВидДокРасх = ФайлИнфПокСодФХЖ4СвПринКодСодОперВидДокРасх.Item3;
            document.СодФХЖ4.СвПрин.КодСодОпер.НомДокРасх = DocumentDiscrepancyNumber;
            document.СодФХЖ4.СвПрин.КодСодОпер.ДатаДокРасх = DocumentDiscrepancyDate?.ToString("dd.MM.yyyy");
            document.СодФХЖ4.СвПрин.КодСодОпер.ИдФайлДокРасх = IdDocumentDiscrepancy;*/

            document.СодФХЖ4.СвПрин.КодСодОпер.РеквДокРасх = GetDocumentData(DiscrepancyDocument);

            var orgEmployeeOrAnotherPerson = OrganizationEmployeeOrAnotherPerson?.FirstOrDefault();
            if (orgEmployeeOrAnotherPerson?.GetType() == typeof(AnotherPerson))
            {
                var anotherPerson = orgEmployeeOrAnotherPerson as AnotherPerson;
                document.СодФХЖ4.СвПрин.СвЛицПрин = new ФайлДокументСодФХЖ4СвПринСвЛицПрин();
                document.СодФХЖ4.СвПрин.СвЛицПрин.Item = new ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицо();
                var anotherPersonItem = (ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицо)document.СодФХЖ4.СвПрин.СвЛицПрин.Item;
                if (anotherPerson?.Item?.GetType() == typeof(OrganizationRepresentative))
                {
                    var organizationRepresentative = anotherPerson.Item as OrganizationRepresentative;
                    anotherPersonItem.Item = new ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицоПредОргПрин();
                    ((ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицоПредОргПрин)anotherPersonItem.Item).Должность = organizationRepresentative.Position;
                    ((ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицоПредОргПрин)anotherPersonItem.Item).ИныеСвед = organizationRepresentative.OtherInfo;
                    ((ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицоПредОргПрин)anotherPersonItem.Item).НаимОргПрин = organizationRepresentative.OrgName;
                    ((ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицоПредОргПрин)anotherPersonItem.Item).ИННОргПрин = organizationRepresentative.OrgInn;
                    ((ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицоПредОргПрин)anotherPersonItem.Item).ОснДоверОргПрин = GetDocumentData(organizationRepresentative.ReasonOrgTrustDocument);
                    ((ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицоПредОргПрин)anotherPersonItem.Item).ОснПолнПредПрин = GetDocumentData(organizationRepresentative.ReasonTrustPersonDocument);
                    ((ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицоПредОргПрин)anotherPersonItem.Item).ФИО = new ФИОТип
                    {
                        Фамилия = organizationRepresentative.Surname,
                        Имя = organizationRepresentative.Name,
                        Отчество = organizationRepresentative.Patronymic
                    };
                }
                else if (anotherPerson?.Item?.GetType() == typeof(TrustedIndividual))
                {
                    var trustedIndividual = anotherPerson.Item as TrustedIndividual;
                    anotherPersonItem.Item = new ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицоФЛПрин();
                    ((ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицоФЛПрин)anotherPersonItem.Item).ОснДоверФЛ = GetDocumentData(trustedIndividual.ReasonOfTrustDocument);
                    ((ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицоФЛПрин)anotherPersonItem.Item).ИныеСвед = trustedIndividual.OtherInfo;
                    ((ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицоФЛПрин)anotherPersonItem.Item).ИННФЛПрин = trustedIndividual.PersonInn;
                    ((ФайлДокументСодФХЖ4СвПринСвЛицПринИнЛицоФЛПрин)anotherPersonItem.Item).ФИО = new ФИОТип
                    {
                        Фамилия = trustedIndividual.Surname,
                        Имя = trustedIndividual.Name,
                        Отчество = trustedIndividual.Patronymic
                    };
                }
            }
            else if (orgEmployeeOrAnotherPerson?.GetType() == typeof(OrganizationEmployee))
            {
                var organizationEmployee = orgEmployeeOrAnotherPerson as OrganizationEmployee;
                document.СодФХЖ4.СвПрин.СвЛицПрин = new ФайлДокументСодФХЖ4СвПринСвЛицПрин();
                document.СодФХЖ4.СвПрин.СвЛицПрин.Item = new ФайлДокументСодФХЖ4СвПринСвЛицПринРабОргПок();
                var orgEmployeeItem = (ФайлДокументСодФХЖ4СвПринСвЛицПринРабОргПок)document.СодФХЖ4.СвПрин.СвЛицПрин.Item;
                orgEmployeeItem.Должность = organizationEmployee.Position;
                orgEmployeeItem.ИныеСвед = organizationEmployee.OtherInfo;
                orgEmployeeItem.ФИО = new ФИОТип
                {
                    Фамилия = organizationEmployee.Surname,
                    Имя = organizationEmployee.Name,
                    Отчество = organizationEmployee.Patronymic
                };
            }

            if (!string.IsNullOrEmpty(FileInformationFieldId) && !string.IsNullOrEmpty(TextInformationId))
            {
                document.СодФХЖ4.ИнфПолФХЖ4 = new ФайлДокументСодФХЖ4ИнфПолФХЖ4();
                document.СодФХЖ4.ИнфПолФХЖ4.ИдФайлИнфПол = FileInformationFieldId;
                document.СодФХЖ4.ИнфПолФХЖ4.ТекстИнф = new ФайлДокументСодФХЖ4ИнфПолФХЖ4ТекстИнф[]
                {
                    new ФайлДокументСодФХЖ4ИнфПолФХЖ4ТекстИнф
                    {
                        Идентиф = TextInformationId,
                        Значен = TextInformation
                    }
                };
            }

            if (SignerInfo != null)
            {
                document.Подписант = new ФайлДокументПодписант[]
                {
                    new ФайлДокументПодписант
                    {
                        Должн = SignerInfo.Position,
                        ДатаПодДок = SignerInfo.SignDate.ToString("dd.MM.yyyy"),
                        ДопСведПодп = SignerInfo.OtherInfo,
                        ФИО = new ФИОТип
                        {
                            Фамилия = SignerInfo.Surname,
                            Имя = SignerInfo.Name,
                            Отчество = SignerInfo.Patronymic
                        }
                    }
                };

                if(SignerInfo.SignType == SignTypeEnum.QualifiedElectronicDigitalSignature)
                {
                    document.Подписант[0].ТипПодпис = ФайлДокументПодписантТипПодпис.Item1;
                    document.Подписант[0].ТипПодписSpecified = true;
                }
                else if (SignerInfo.SignType == SignTypeEnum.SimpleElectronicDigitalSignature)
                {
                    document.Подписант[0].ТипПодпис = ФайлДокументПодписантТипПодпис.Item2;
                    document.Подписант[0].ТипПодписSpecified = true;
                }
                else if (SignerInfo.SignType == SignTypeEnum.NonQualifiedElectronicDigitalSignature)
                {
                    document.Подписант[0].ТипПодпис = ФайлДокументПодписантТипПодпис.Item3;
                    document.Подписант[0].ТипПодписSpecified = true;
                }

                if(SignerInfo.MethodOfConfirmingAuthorityEnum == MethodOfConfirmingAuthorityEnum.DigitalSignature)
                {
                    document.Подписант[0].СпосПодтПолном = ФайлДокументПодписантСпосПодтПолном.Item1;
                }
                else if (SignerInfo.MethodOfConfirmingAuthorityEnum == MethodOfConfirmingAuthorityEnum.EmchdInPackageOfElectronicDocuments)
                {
                    document.Подписант[0].СпосПодтПолном = ФайлДокументПодписантСпосПодтПолном.Item2;
                }
                else if (SignerInfo.MethodOfConfirmingAuthorityEnum == MethodOfConfirmingAuthorityEnum.EmchdDataInDocument)
                {
                    document.Подписант[0].СпосПодтПолном = ФайлДокументПодписантСпосПодтПолном.Item3;

                    document.Подписант[0].Items = 
                        new object[]
                        {
                            new ФайлДокументПодписантСвДоверЭл
                            {
                                НомДовер = SignerInfo?.ElectronicPowerOfAttorney?.RegistrationNumber,
                                ДатаВыдДовер = SignerInfo?.ElectronicPowerOfAttorney?.RegistrationDate.ToString("dd.MM.yyyy"),
                                ИдСистХран = SignerInfo?.ElectronicPowerOfAttorney?.SystemIdentificationInfo,
                                УРЛСист = SignerInfo?.ElectronicPowerOfAttorney?.UrlSystem
                            }
                        };
                }
                else if (SignerInfo.MethodOfConfirmingAuthorityEnum == MethodOfConfirmingAuthorityEnum.EmchdDataInOtherSystem)
                {
                    document.Подписант[0].СпосПодтПолном = ФайлДокументПодписантСпосПодтПолном.Item4;
                }
                else if (SignerInfo.MethodOfConfirmingAuthorityEnum == MethodOfConfirmingAuthorityEnum.PaperPowerOfAttorney)
                {
                    document.Подписант[0].СпосПодтПолном = ФайлДокументПодписантСпосПодтПолном.Item5;

                    document.Подписант[0].Items =
                        new object[]
                        {
                            new ФайлДокументПодписантСвДоверБум
                            {
                                ВнНомДовер = SignerInfo?.PaperPowerOfAttorney?.InternalNumber,
                                ДатаВыдДовер = SignerInfo?.PaperPowerOfAttorney?.Date.ToString("dd.MM.yyyy"),
                                СвИдДовер = SignerInfo?.PaperPowerOfAttorney?.IdentificationInfo,
                                ФИО = new ФИОТип
                                {
                                    Фамилия = SignerInfo?.PaperPowerOfAttorney?.Surname,
                                    Имя = SignerInfo?.PaperPowerOfAttorney?.Name,
                                    Отчество = SignerInfo?.PaperPowerOfAttorney?.Patronymic
                                }
                            }
                        };
                }
                else if (SignerInfo.MethodOfConfirmingAuthorityEnum == MethodOfConfirmingAuthorityEnum.Other)
                {
                    document.Подписант[0].СпосПодтПолном = ФайлДокументПодписантСпосПодтПолном.Item6;
                }
            }

            string xml = Xml.SerializeEntity<Файл>(xsdDocument, Encoding.GetEncoding(1251));
            return $"<?xml version=\"1.0\" encoding=\"windows-1251\"?>{xml}";
        }

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

        private DocumentDetails GetDocumentDetails(РеквДокТип documentData)
        {
            DocumentDetails documentDetails = null;

            if(documentData != null)
            {
                documentDetails = new DocumentDetails
                {
                    DocumentName = documentData.РеквНаимДок,
                    DocumentNumber = documentData.РеквНомерДок,
                    DocumentDate = DateTime.ParseExact(documentData.РеквДатаДок, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture),
                    FirstDocumentId = documentData.РеквИдФайлДок,
                    DocumentId = documentData.РеквИдДок,
                    DocumentSystemSavingId = documentData.РИдСистХранД,
                    UrlAddress = documentData.РеквУРЛСистДок,
                    DocumentOtherInfo = documentData.РеквДопСведДок
                };

                if(documentData.РеквИдРекСост != null)
                {
                    documentDetails.FinSubjectCreators = new FinSubjectCreator[documentData.РеквИдРекСост.Length];

                    if(documentData.РеквИдРекСост.Length > 0)
                    {
                        for (int i = 0; i < documentData.РеквИдРекСост.Length; i++)
                        {
                            documentDetails.FinSubjectCreators[i] = new FinSubjectCreator();

                            if (documentData.РеквИдРекСост[i].ItemElementName == ItemChoiceType.ИННЮЛ)
                                documentDetails.FinSubjectCreators[i].JuridicalEntityInn = (string)documentData.РеквИдРекСост[i].Item;
                            else if (documentData.РеквИдРекСост[i].ItemElementName == ItemChoiceType.ИННФЛ)
                                documentDetails.FinSubjectCreators[i].PersonInn = (string)documentData.РеквИдРекСост[i].Item;
                            else if (documentData.РеквИдРекСост[i].ItemElementName == ItemChoiceType.НаимОИВ)
                                documentDetails.FinSubjectCreators[i].ExecutiveAuthorityOrganization = (string)documentData.РеквИдРекСост[i].Item;
                            else if (documentData.РеквИдРекСост[i].ItemElementName == ItemChoiceType.ДаннИно)
                            {
                                var item = documentData.РеквИдРекСост[i].Item as СвИнНеУчТип;

                                documentDetails.FinSubjectCreators[i].ForeignOrganizationData = new ForeignOrganizationData
                                {
                                    CountryCode = item?.КодСтр,
                                    CountryName = item?.НаимСтран,
                                    Name = item?.Наим,
                                    IdentificationInfo = item?.Идентиф,
                                    OtherInfo = item?.ИныеСвед
                                };

                                if (item.ИдСтат == СвИнНеУчТипИдСтат.ИГ)
                                    documentDetails.FinSubjectCreators[i].ForeignOrganizationData.StatusId = "ИГ";
                                else if (item.ИдСтат == СвИнНеУчТипИдСтат.ИО)
                                    documentDetails.FinSubjectCreators[i].ForeignOrganizationData.StatusId = "ИО";
                            }
                        }
                    }
                }
            }

            return documentDetails;
        }

        #endregion
    }
}
