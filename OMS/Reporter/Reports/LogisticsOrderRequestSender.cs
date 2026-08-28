using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reporter.Entities;
using Reporter.XsdClasses.OnZakzvgo_1_969_01_05_01_01;

namespace Reporter.Reports
{
    public class LogisticsOrderRequestSender : IReport
    {
        private const string codeOfRussia = "643";

        private XmlUtils _xmlUtils;

        public LogisticsOrderRequestSender()
        {
            _xmlUtils = new XmlUtils();
        }

        /// <summary>
        /// Идентификатор файла
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Версия формата
        /// </summary>
        public const string FormatVersion = "5.01";

        /// <summary>
        /// Версия программы, с помощью которой сформирован файл
        /// </summary>
        public string EdoProgramVersion { get; set; }

        /// <summary>
        /// Дата формирования файла обмена информации грузоотправителя
        /// </summary>
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// Наименование экономического субъекта, составляющего файл обмена информации грузоотправителя
        /// </summary>
        public string DocumentCreator { get; set; }

        /// <summary>
        /// Функция
        /// </summary>
        public string Function { get; set; }

        /// <summary>
        /// Порядковый номер заказа и заявки
        /// </summary>
        public string Number { get; set; }

        /// <summary>
        /// Дата заказа и заявки
        /// </summary>
        public DateTime OrderDate { get; set; }

        /// <summary>
        /// Указания, необходимые для выполнения фитосанитарных, санитарных, карантинных требований, установленных законодательством Российской Федерации
        /// </summary>
        public string SanitaryRequirements { get; set; }

        /// <summary>
        /// Требования о необходимости предоставления транспортного средства для перевозки пищевых продуктов
        /// </summary>
        public string FoodRequirements { get; set; }

        /// <summary>
        /// Реквизиты договора об организации перевозки груза
        /// </summary>
        public TransportDocumentContract TransportContract { get; set; }

        /// <summary>
        /// Сведения о перевозчике
        /// </summary>
        public LogisticsParticipantType Carrier { get; set; }

        /// <summary>
        /// Сведения о грузоотправителе
        /// </summary>
        public LogisticsParticipantType Shipper { get; set; }

        /// <summary>
        /// Пункт подачи транспортного средства
        /// </summary>
        public SupplyPoint SupplyPoint { get; set; }

        /// <summary>
        /// Адреса пунктов погрузки и выгрузки
        /// </summary>
        public LogisticAddressPoint[] AddressPoints { get; set; }

        /// <summary>
        /// Описание груза
        /// </summary>
        public Cargo[] Cargoes { get; set; }

        /// <summary>
        /// Параметры транспортного средства, необходимые для осуществления перевозки груза
        /// </summary>
        public VehicleParameters VehicleParameters { get; set; }

        /// <summary>
        /// Подписант
        /// </summary>
        public SignerInfo SignerInfo { get; set; }


        public void Parse(byte[] content)
        {
            var xmlString = Encoding.GetEncoding(1251).GetString(content);
            Parse(xmlString);
        }

        public void Parse(string content)
        {

        }

        public string GetXmlContent()
        {
            var xsdDocument = new Файл();

            xsdDocument.ИдФайл = FileName;
            xsdDocument.ВерсПрог = EdoProgramVersion;

            xsdDocument.Документ = new ФайлДокумент();
            xsdDocument.Документ.ДатИнфГО = CreateDate.ToString("dd.MM.yyyy");
            xsdDocument.Документ.ВрИнфГО = CreateDate.ToString("HH:mm:ss");
            xsdDocument.Документ.НаимЭкСубСост = DocumentCreator;

            if(Function == "Заявка")
                xsdDocument.Документ.Функция = ФайлДокументФункция.Заявка;
            else
                xsdDocument.Документ.Функция = ФайлДокументФункция.Заказ;

            if (xsdDocument.Документ.Функция == ФайлДокументФункция.Заявка && TransportContract != null)
            {
                xsdDocument.Документ.ДогОргПрвз = new РеквДокТип();
                xsdDocument.Документ.ДогОргПрвз.НаимДок = TransportContract.DocumentName;
                xsdDocument.Документ.ДогОргПрвз.НомерДок = TransportContract.DocumentNumber;
                xsdDocument.Документ.ДогОргПрвз.ДатаДок = TransportContract.DocumentDate.ToString("dd.MM.yyyy");

                if (TransportContract.Contractors != null && TransportContract.Contractors.Count > 0)
                {
                    xsdDocument.Документ.ДогОргПрвз.ИдРекСост = new ИдРекСостТип[TransportContract.Contractors.Count];

                    int i = 0;
                    foreach (var contractor in TransportContract.Contractors)
                    {
                        xsdDocument.Документ.ДогОргПрвз.ИдРекСост[i] = new ИдРекСостТип();
                        xsdDocument.Документ.ДогОргПрвз.ИдРекСост[i].Item = contractor.OrgInn;

                        if (contractor.OrgInn.Length == 12)
                            xsdDocument.Документ.ДогОргПрвз.ИдРекСост[i].ItemElementName = ItemChoiceType.ИННФЛ;
                        else
                            xsdDocument.Документ.ДогОргПрвз.ИдРекСост[i].ItemElementName = ItemChoiceType.ИННЮЛ;

                        i++;
                    }
                }
            }

            xsdDocument.Документ.СодИнфГО = new ФайлДокументСодИнфГО();
            xsdDocument.Документ.СодИнфГО.СодОпер = ФайлДокументСодИнфГОСодОпер.Предоставлениезаказаизаявкинаперевозкугрузаавтомобильнымтранспортом;
            xsdDocument.Документ.СодИнфГО.НомЗак = Number;
            xsdDocument.Документ.СодИнфГО.ДатаЗак = OrderDate.ToString("dd.MM.yyyy");
            xsdDocument.Документ.СодИнфГО.УкНормПрвз = SanitaryRequirements;
            xsdDocument.Документ.СодИнфГО.ПрвзПищПрод = FoodRequirements;

            if(Carrier != null)
            {
                xsdDocument.Документ.СодИнфГО.СвПрв = GetLogisticsParticipant(Carrier);
            }

            if(Shipper != null)
            {
                xsdDocument.Документ.СодИнфГО.СвГО = GetLogisticsParticipant(Shipper);
            }

            if (SupplyPoint != null)
            {
                xsdDocument.Документ.СодИнфГО.ПунктПод = new ФайлДокументСодИнфГОПунктПод();

                xsdDocument.Документ.СодИнфГО.ПунктПод.ДатВрПод = SupplyPoint.DateTime.ToString("dd.MM.yyyyTHH:mm:ss");

                if (SupplyPoint.IsUtcUsed)
                {
                    xsdDocument.Документ.СодИнфГО.ПунктПод.ДатВрПод = xsdDocument.Документ.СодИнфГО.ПунктПод.ДатВрПод + "+10:00";
                    xsdDocument.Документ.СодИнфГО.ПунктПод.НалКоорТочВрПод = ФайлДокументСодИнфГОПунктПодНалКоорТочВрПод.Item1;
                }
                else
                    xsdDocument.Документ.СодИнфГО.ПунктПод.НалКоорТочВрПод = ФайлДокументСодИнфГОПунктПодНалКоорТочВрПод.Item0;

                if (SupplyPoint.Address != null)
                {
                    xsdDocument.Документ.СодИнфГО.ПунктПод.АдрПунктПод = new АдресПользТип
                    {
                        Адрес = GetParticipantAddress(SupplyPoint.Address.Address)
                    };

                    if (!string.IsNullOrEmpty(SupplyPoint.Address.Gln))
                        xsdDocument.Документ.СодИнфГО.ПунктПод.АдрПунктПод.ГЛН = SupplyPoint.Address.Gln;

                    if (!string.IsNullOrEmpty(SupplyPoint.Address.Comment))
                        xsdDocument.Документ.СодИнфГО.ПунктПод.АдрПунктПод.АдрКоммент = SupplyPoint.Address.Comment;

                    if (SupplyPoint.Address.Coordinates != null)
                        xsdDocument.Документ.СодИнфГО.ПунктПод.АдрПунктПод.Коорд = new КоордТип
                        {
                            Долгота = SupplyPoint.Address.Coordinates.Value.Key,
                            Широта = SupplyPoint.Address.Coordinates.Value.Value
                        };
                }
            }

            if(AddressPoints != null && AddressPoints.Count() > 0)
            {
                xsdDocument.Документ.СодИнфГО.АдрПункт = new ФайлДокументСодИнфГОАдрПункт[AddressPoints.Count()];

                int i = 0;
                foreach (var addressPoint in AddressPoints)
                {
                    xsdDocument.Документ.СодИнфГО.АдрПункт[i] = new ФайлДокументСодИнфГОАдрПункт();

                    if (addressPoint.Operation == Enums.LogisticAddressPointOperationEnum.Loading)
                        xsdDocument.Документ.СодИнфГО.АдрПункт[i].Опер = ФайлДокументСодИнфГОАдрПунктОпер.Погрузка;
                    else if (addressPoint.Operation == Enums.LogisticAddressPointOperationEnum.Unloading)
                        xsdDocument.Документ.СодИнфГО.АдрПункт[i].Опер = ФайлДокументСодИнфГОАдрПунктОпер.Выгрузка;

                    if (!string.IsNullOrEmpty(addressPoint.PositionPoint))
                        xsdDocument.Документ.СодИнфГО.АдрПункт[i].ПорНомПункт = addressPoint.PositionPoint;

                    if(addressPoint.AddressPoint != null)
                    {
                        xsdDocument.Документ.СодИнфГО.АдрПункт[i].АдресПункт = new АдресПользТип();

                        if (!string.IsNullOrEmpty(addressPoint.AddressPoint.Gln))
                            xsdDocument.Документ.СодИнфГО.АдрПункт[i].АдресПункт.ГЛН = addressPoint.AddressPoint.Gln;

                        if (!string.IsNullOrEmpty(addressPoint.AddressPoint.Comment))
                            xsdDocument.Документ.СодИнфГО.АдрПункт[i].АдресПункт.АдрКоммент = addressPoint.AddressPoint.Comment;

                        if (addressPoint.AddressPoint.Coordinates != null)
                            xsdDocument.Документ.СодИнфГО.АдрПункт[i].АдресПункт.Коорд = new КоордТип
                            {
                                Долгота = addressPoint.AddressPoint.Coordinates.Value.Key,
                                Широта = addressPoint.AddressPoint.Coordinates.Value.Value
                            };

                        if(addressPoint.AddressPoint.Address != null)
                            xsdDocument.Документ.СодИнфГО.АдрПункт[i].АдресПункт.Адрес = GetParticipantAddress(addressPoint.AddressPoint.Address);
                    }

                    if(addressPoint.OwnerOrganization != null)
                    {
                        xsdDocument.Документ.СодИнфГО.АдрПункт[i].ОргВладИнфр = new ФайлДокументСодИнфГОАдрПунктОргВладИнфр
                        {
                            НаимВладИнфр = addressPoint.OwnerOrganization.OrgName,
                            ИННВладИнфр = addressPoint.OwnerOrganization.OrgInn
                        };
                    }

                    i++;
                }
            }

            if(Cargoes != null && Cargoes.Count() > 0)
            {
                xsdDocument.Документ.СодИнфГО.ОпГруз = new ФайлДокументСодИнфГООпГруз[Cargoes.Count()];

                int i = 0;
                foreach(var cargo in Cargoes)
                {
                    xsdDocument.Документ.СодИнфГО.ОпГруз[i] = new ФайлДокументСодИнфГООпГруз
                    {
                        НаимГруз = cargo.Name,
                        СостГруз = cargo.Condition,
                        Объем = cargo.Volume,
                        ВидТар = cargo.ContainerType,
                        КолГрМест = cargo.PlaceCount
                    };

                    if (cargo.WeighingMethod == Enums.WeighingMethodEnum.WeighingByTotalMass)
                        xsdDocument.Документ.СодИнфГО.ОпГруз[i].МетОпрМасс = ФайлДокументСодИнфГООпГрузМетОпрМасс.Item01;
                    else if (cargo.WeighingMethod == Enums.WeighingMethodEnum.WeighingByAxes)
                        xsdDocument.Документ.СодИнфГО.ОпГруз[i].МетОпрМасс = ФайлДокументСодИнфГООпГрузМетОпрМасс.Item02;
                    else if (cargo.WeighingMethod == Enums.WeighingMethodEnum.CalculatedWeight)
                        xsdDocument.Документ.СодИнфГО.ОпГруз[i].МетОпрМасс = ФайлДокументСодИнфГООпГрузМетОпрМасс.Item03;

                    if (cargo.PossibilityDistributionAlongPlatform == Enums.PossibilityDistributionAlongPlatformEnum.Possible)
                        xsdDocument.Документ.СодИнфГО.ОпГруз[i].РаспрГр = ФайлДокументСодИнфГООпГрузРаспрГр.Item0;
                    else if (cargo.PossibilityDistributionAlongPlatform == Enums.PossibilityDistributionAlongPlatformEnum.NotPossible)
                        xsdDocument.Документ.СодИнфГО.ОпГруз[i].РаспрГр = ФайлДокументСодИнфГООпГрузРаспрГр.Item1;

                    if (cargo.CargoDivisibility == Enums.CargoDivisibilityEnum.Divisible)
                        xsdDocument.Документ.СодИнфГО.ОпГруз[i].ДелГр = ФайлДокументСодИнфГООпГрузДелГр.Item1;
                    else if (cargo.CargoDivisibility == Enums.CargoDivisibilityEnum.Indivisible)
                        xsdDocument.Документ.СодИнфГО.ОпГруз[i].ДелГр = ФайлДокументСодИнфГООпГрузДелГр.Item0;

                    if (cargo.CargoPlacesWeight != null)
                        xsdDocument.Документ.СодИнфГО.ОпГруз[i].МасГруз = new МассаТип
                        {
                            МасБрутЗнач = cargo.CargoPlacesWeight.Gross
                        };

                    if (cargo.PlacesCargoDimensions != null)
                        xsdDocument.Документ.СодИнфГО.ОпГруз[i].РазмерГрМест = new ГабарТип
                        {
                            ВысЗнач = cargo.PlacesCargoDimensions.Height,
                            ДлЗнач = cargo.PlacesCargoDimensions.Length,
                            ШирЗнач = cargo.PlacesCargoDimensions.Width
                        };

                    if(cargo.DeliveryPoints != null && cargo.DeliveryPoints.Count() > 0)
                    {
                        xsdDocument.Документ.СодИнфГО.ОпГруз[i].Пункт = new ФайлДокументСодИнфГООпГрузПункт[cargo.DeliveryPoints.Count()];

                        int j = 0;
                        foreach(var deliveryPoint in cargo.DeliveryPoints)
                        {
                            xsdDocument.Документ.СодИнфГО.ОпГруз[i].Пункт[j] = new ФайлДокументСодИнфГООпГрузПункт();

                            xsdDocument.Документ.СодИнфГО.ОпГруз[i].Пункт[j].Выгр = deliveryPoint.UnloadingPoint;
                            xsdDocument.Документ.СодИнфГО.ОпГруз[i].Пункт[j].Погр = deliveryPoint.LoadingPoint;
                            xsdDocument.Документ.СодИнфГО.ОпГруз[i].Пункт[j].КолГрМест = deliveryPoint.PlaceCount;

                            j++;
                        }
                    }

                    i++;
                }
            }

            if(VehicleParameters != null)
            {
                xsdDocument.Документ.СодИнфГО.ПарТСПрвз = new ФайлДокументСодИнфГОПарТСПрвз
                {
                    Тип = VehicleParameters.Type,
                    Грузопод = VehicleParameters.WeightCapacity,
                    Вместим = VehicleParameters.VolumeCapacity
                };
            }

            if(SignerInfo != null)
            {
                xsdDocument.Документ.ПодпИнфГО = new ПодписантТип
                {
                    Должн = SignerInfo.Position
                };

                var signer = xsdDocument.Документ.ПодпИнфГО;

                if (SignerInfo.MethodOfConfirmingAuthorityEnum == Enums.MethodOfConfirmingAuthorityEnum.DigitalSignature)
                    signer.СпосПодтПолном = ПодписантТипСпосПодтПолном.Item1;
                else if (SignerInfo.MethodOfConfirmingAuthorityEnum == Enums.MethodOfConfirmingAuthorityEnum.EmchdInPackageOfElectronicDocuments)
                    signer.СпосПодтПолном = ПодписантТипСпосПодтПолном.Item2;
                else if (SignerInfo.MethodOfConfirmingAuthorityEnum == Enums.MethodOfConfirmingAuthorityEnum.EmchdDataInDocument)
                {
                    signer.СпосПодтПолном = ПодписантТипСпосПодтПолном.Item3;

                    if (SignerInfo.ElectronicPowerOfAttorney == null)
                        throw new Exception("Не указана машиночитаемая доверенность.");

                    signer.СвДоверЭл = new[]
                    {
                        new ПодписантТипСвДоверЭл
                        {
                            НомДовер = SignerInfo.ElectronicPowerOfAttorney.RegistrationNumber,
                            ДатаДовер = SignerInfo.ElectronicPowerOfAttorney.RegistrationDate.ToString("dd.MM.yyyy"),
                            ИдСистХран = SignerInfo.ElectronicPowerOfAttorney.SystemIdentificationInfo
                        }
                    };
                }
                else if (SignerInfo.MethodOfConfirmingAuthorityEnum == Enums.MethodOfConfirmingAuthorityEnum.EmchdDataInOtherSystem)
                    signer.СпосПодтПолном = ПодписантТипСпосПодтПолном.Item4;
                else if (SignerInfo.MethodOfConfirmingAuthorityEnum == Enums.MethodOfConfirmingAuthorityEnum.PaperPowerOfAttorney)
                    signer.СпосПодтПолном = ПодписантТипСпосПодтПолном.Item5;
                else if (SignerInfo.MethodOfConfirmingAuthorityEnum == Enums.MethodOfConfirmingAuthorityEnum.Other)
                    signer.СпосПодтПолном = ПодписантТипСпосПодтПолном.Item6;

                if (SignerInfo.SignType == Enums.SignTypeEnum.QualifiedElectronicDigitalSignature)
                    signer.ТипПодпис = ПодписантТипТипПодпис.Item1;
                else if (SignerInfo.SignType == Enums.SignTypeEnum.SimpleElectronicDigitalSignature)
                    signer.ТипПодпис = ПодписантТипТипПодпис.Item2;
                else if (SignerInfo.SignType == Enums.SignTypeEnum.NonQualifiedElectronicDigitalSignature)
                    signer.ТипПодпис = ПодписантТипТипПодпис.Item3;

                signer.ФИО = new ФИОТип
                {
                    Фамилия = SignerInfo.Surname,
                    Имя = SignerInfo.Name,
                    Отчество = SignerInfo.Patronymic
                };
            }

            string xml = _xmlUtils.SerializeObject<Файл>(xsdDocument, Encoding.GetEncoding(1251));
            return $"<?xml version=\"1.0\" encoding=\"windows-1251\"?>{xml}";
        }

        private УчастникТип GetLogisticsParticipant(LogisticsParticipantType participantType)
        {
            if (participantType.Contact == null)
                throw new Exception("Не заданы контактные данные.");

            var result = new УчастникТип
            {
                Конт = new КонтактТип
                {
                    Тлф = new string[] { participantType.Contact.Phone }
                }
            };

            if (!string.IsNullOrEmpty(participantType.Contact.Email))
                result.Конт.ЭлПочта = new string[] { participantType.Contact.Email };

            if (!string.IsNullOrEmpty(participantType.Contact.OtherData))
                result.Конт.ИнКонт = participantType.Contact.OtherData;

            if(participantType.Address != null)
                result.Адрес = GetParticipantAddress(participantType.Address);

            if (participantType.Item as OrganizationExchangeParticipantEntity != null)
            {
                result.ИдСв = new ИдСвТип
                {
                    Item = new СвЮЛУчТип
                    {
                        НаимОрг = ((OrganizationExchangeParticipantEntity)participantType.Item).OrgName,
                        ИННЮЛ = ((OrganizationExchangeParticipantEntity)participantType.Item).JuridicalInn,
                        КПП = ((OrganizationExchangeParticipantEntity)participantType.Item).JuridicalKpp
                    }
                };
            }
            else if(participantType.Item as IndividualEntity != null)
            {
                result.ИдСв = new ИдСвТип
                {
                    Item = new СвФЛТип
                    {
                        ИННФЛ = ((IndividualEntity)participantType.Item).Inn,
                        ИныеСвед = ((IndividualEntity)participantType.Item).OtherInfo,
                        ФИО = new ФИОТип
                        {
                            Фамилия = ((IndividualEntity)participantType.Item).Surname,
                            Имя = ((IndividualEntity)participantType.Item).Name,
                            Отчество = ((IndividualEntity)participantType.Item).Patronymic
                        }
                    }
                };
            }
            else if (participantType.Item as JuridicalEntity != null)
            {
                result.ИдСв = new ИдСвТип
                {
                    Item = new СвИПТип
                    {
                        ИННФЛ = ((JuridicalEntity)participantType.Item).Inn,
                        ИныеСвед = ((JuridicalEntity)participantType.Item).OtherInfo,
                        ОГРНИП = ((JuridicalEntity)participantType.Item).CertificateOfFederalRegistration,
                        ФИО = new ФИОТип
                        {
                            Фамилия = ((JuridicalEntity)participantType.Item).Surname,
                            Имя = ((JuridicalEntity)participantType.Item).Name,
                            Отчество = ((JuridicalEntity)participantType.Item).Patronymic
                        }
                    }
                };
            }

            return result;
        }

        private АдресТип GetParticipantAddress(Address addressObj)
        {
            var addressXml = new АдресТип();

            if (addressObj?.CountryCode == codeOfRussia && string.IsNullOrEmpty(addressObj?.ForeignTextAddress))
            {
                var russianAddress = new АдрРФТип();
                russianAddress.КодРегион = addressObj.RussianRegionCode;

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
                foreignAddress.АдрТекст = addressObj.ForeignTextAddress;

                addressXml.Item = foreignAddress;
            }

            return addressXml;
        }
    }
}
