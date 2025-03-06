using System;
using EdiProcessingUnit.Edi;
using System.Net.Http;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using EdiProcessingUnit.Infrastructure;
using System.Linq;
using System.Collections.Generic;
using EdiProcessingUnit.WorkingUnits;
using EdiProcessingUnit.Edi.Model;
using SkbKontur.EdiApi.Client.Types.Common;
using Cryptography.WinApi;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using FileWorker;
using Diadoc.Api;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EdiTest
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void TestMethod1()
		{
			HttpClient client = new HttpClient();
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create( "https://edi-api.kontur.ru/V1/Messages/GetInboxMessage?boxId=4b9b19f0-c924-43cb-a941-500a845d43c7&messageId=e67c738e-346a-11eb-acc1-4f55bc11eb94" );

			request.Method = "GET";
			request.Accept = "application/json";
			request.Headers.Add( "Authorization", "KonturEdiAuth konturediauth_api_client_id=Virey-474656da-179d-4d9f-a971-a3f8e8068d27, konturediauth_token=vx+TCNs9A0NyjiPbqothlct11Aexb8x950thZGz5w/zoIRfcyee4oFKOdroSWpGK9QzVT45TOb/eOLW3VxUGcbwhv3eWvy0Sgony/LTRd3Ge70GIFbz1YUe79tng7Wfr0LhnYo94lASQv7O8h5yoIe3giVbsmLe/q15/1IOiNXHk1dDWHsxwyCvWlbT5h5IKSbDj3GIUcTtAN49rfSEljQ==" );

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			var reader = new StreamReader(response.GetResponseStream());

			StringBuilder output = new StringBuilder();
			output.Append( reader.ReadToEnd() );
			response.Close();
			var result = output.ToString();

			InboxMessage message = JsonConvert.DeserializeObject<InboxMessage>(result);
			var messageBodyString = Encoding.UTF8.GetString( message.Data.MessageBody, 0, message.Data.MessageBody.Length );
			messageBodyString = string.Join( "\n", messageBodyString.Split( '\n' ).Skip( 1 ).ToArray() );

			var messageResult = Xml.DeserializeString<EDIMessage>( messageBodyString );

			//var order = new OrdersProcessor().ValidateNewMessage( messageResult );
		}

        [TestMethod]
        public void TestChangeGateway()
        {
            ChangeGateway( "192.168.2.15" );
        }

        [TestMethod]
        public void ImportExcelDataTest()
        {
            ExcelColumnCollection columnCollection = new ExcelColumnCollection();

            columnCollection.AddColumn( "Id", "ID" );
            columnCollection.AddColumn( "Number", "Номер" );
            columnCollection.AddColumn( "Status", "Статус" );

            var doc = new ExcelDocumentData( columnCollection );

            List<ExcelDocumentData> docs = new List<ExcelDocumentData>(new ExcelDocumentData[] {
                doc
            } );

            ExcelFileWorker worker = new ExcelFileWorker( "C:\\Users\\systech\\Desktop\\export\\Orders.xls", docs );

            worker.ImportData<DocOrder>();

            var result = doc.Data.Cast<DocOrder>().ToList();
        }

        [TestMethod]
        public void SoledPassword()
        {
            string password = "pass";

            var position = 6;
            var shift = 7;
            string salt = "uc*nwex^wgx#kriior&gcier+irerzqqp?wiqavb";

            var passwordData = Encoding.ASCII.GetBytes(password);
            var saltData = Encoding.ASCII.GetBytes(salt);

            int i = 0;
            foreach(var p in passwordData)
            {
                var b = (byte)(((int)p + (int)saltData[(position + i * shift) % saltData.Length]) % 128);
                saltData[(position + i * shift) % saltData.Length] = b;
                i++;
            }

            byte[] skitalaBytes = new byte[40];

            for(int j = 0; j < 5; j++)
            {
                for (int k = 0; k < 8; k++)
                {
                    skitalaBytes[8 * j + k] = saltData[j + 5 * k];
                }
            }

            string result = Encoding.ASCII.GetString(skitalaBytes);

            //получение пароля
            skitalaBytes = Encoding.ASCII.GetBytes(result);
            saltData = Encoding.ASCII.GetBytes(salt);


            var bytes = new byte[40];

            for(int j = 0; j < 8; j++)
            {
                for(int k = 0; k < 5; k++)
                {
                    bytes[j * 5 + k] = skitalaBytes[j + k * 8];
                }
            }

            password = "";
            List<byte> passData = new List<byte>();
            i = 0;
            while(saltData[(position + i * shift) % bytes.Length] != bytes[(position + i * shift) % bytes.Length])
            {
                byte b;
                if(saltData[(position + i * shift) % bytes.Length] > bytes[(position + i * shift) % bytes.Length])
                {
                    b = (byte)(128 + (int)bytes[(position + i * shift) % bytes.Length] - (int)saltData[(position + i * shift) % bytes.Length]);
                }
                else
                {
                    b = (byte)(bytes[(position + i * shift) % bytes.Length] - saltData[(position + i * shift) % bytes.Length]);
                }
                passData.Add(b);
                i++;
            }
            password = Encoding.ASCII.GetString(passData.ToArray());
        }

        [TestMethod]
        public void EdoGenerateXmlTest()
        {
            var edo = EdiProcessingUnit.Edo.Edo.GetInstance();
            edo.Authenticate(true, null, "2504000010");
            var userDocCreator = edo.GetUniversalTransferDocument(new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationDetailsUtd970
            {
                Inn = "2504000010",
                Kpp = "253901001",
                OrgType = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item2,
                OrgName = "ООО \"Вирэй\"",
                Address = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AddressUtd970
                {
                    Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.RussianAddressUtd970
                    {
                        Region = "25"
                    }
                }
            },
            new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationDetailsUtd970
            {
                Inn = "9500000005",
                Kpp = "667301001",
                OrgType = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item2,
                OrgName = "ООО Тестовое Юрлицо обычное",
                Address = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AddressUtd970
                {
                    Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.RussianAddressUtd970
                    {
                        Region = "66"
                    }
                }
            },
            "01.01.2020",
            "134",
            Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocumentFunction.СЧФДОП,
            new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TransferInfo
            {
                OperationInfo = "Товары переданы",
                TransferDate = DateTime.Now.ToString("dd.MM.yyyy")
            },
            new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTable
            {
                Item = new[]
                    {
                        new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItem
                        {
                            Product = "товар",
                            Unit = "796",
                            Quantity = 10,
                            QuantitySpecified = true,
                            SubtotalSpecified = true,
                            Subtotal = 1560,
                            VatSpecified = true,
                            Vat = 260,
                            SubtotalWithVatExcludedSpecified = true,
                            SubtotalWithVatExcluded = 1300,
                            PriceSpecified = true,
                            Price = 130,
                            ItemMark = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemMark.Item4,
                            ItemIdentificationNumbers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemIdentificationNumber[]
                            {
                                new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemIdentificationNumber
                                {
                                    ItemsElementName = new []{ Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ItemsChoiceType.Unit },
                                    Items = new string[]{"4564385435465"},
                                    TransPackageId = "6352386234233"
                                }
                            }
                        }
                    },

                TotalSpecified = true,
                Total = 1560,
                VatSpecified = true,
                Vat = 260,
                TotalWithVatExcludedSpecified = true,
                TotalWithVatExcluded = 1300
            },
            new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signers()
            {
                Signer = new[]
                {
                    new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signer
                    {
                        Fio = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Fio
                        {
                            FirstName = "Ирина",
                            MiddleName = "Васильевна",
                            LastName = "Бельтюкова"
                        },
                        Position = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerPosition
                        {
                            PositionSource = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerPositionPositionSource.Manual,
                            Value = "Директор по продажам"
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
                                        RegistrationNumber = "b591a89f-ffe1-439c-9f04-e02444839231",
                                        IssuerInn = "2504000010"
                                    }
                                }
                            }
                        }
                    }
                }
            },
            0, "643", "Писаренко А.Н.");

            try
            {
                var docFile = edo.GenerateTitleXml("UniversalTransferDocument",
                    "СЧФДОП", "utd970_05_03_01", 0, userDocCreator);

                var xmlString = Encoding.GetEncoding(1251).GetString(docFile.Content);

                docFile.SaveContentToFile($"C:\\Users\\systech\\Desktop\\Files\\{docFile.FileName}");

                var universalTransferDocumentString = Encoding.UTF8.GetString(userDocCreator.SerializeToXml());
                //var reporter = new Reporter.Reporters.UniversalTransferDocumentReporter();
                //universalTransferDocumentString = string.Join("\n", universalTransferDocumentString.Split('\n').Skip(1).ToArray());
                //reporter.LoadFromXml(universalTransferDocumentString);
            }
            catch (WebException webEx)
            {

            }
            catch (Exception ex)
            {

            }
        }

        [TestMethod]
        public void SignCryptographyTest()
        {
            var filePath = @"C:\Users\systech\Desktop\Машиночитаемая доверенность\ON_EMCHD_20231116_e785f785-d763-464b-bdf4-56737bc3cb95.xml";

            var crypto = new WinApiCryptWrapper();
            var signerCertificate = crypto.GetCertificateWithPrivateKey("A1908BDFF81A0F3940D6EDB428263E48A9C05927", false);

            var crypt = new WinApiCryptWrapper(signerCertificate);

            var contentBytes = File.ReadAllBytes(filePath);

            var signature = crypt.Sign(contentBytes, true);
            var signatureString = Convert.ToBase64String(signature);

            File.WriteAllBytes($"{filePath}.sig", signature);
        }

        [TestMethod]
        public void SendDiadokTest()
        {
            var filePath = "C:\\Users\\systech\\Desktop\\Файлы\\ON_NSCHFDOPPR_2BM-2720030404-272001001-201408210227045615873_2BM-2504000010-2012052808301120662630000000000_20210730_76042b1d-bbea-4643-96a0-42358b43c10c.xml";
            var edo = EdiProcessingUnit.Edo.Edo.GetInstance();
            var crypto = new WinApiCryptWrapper();
            var cert = crypto.GetCertificateWithPrivateKey("0DE1BF746CC43954D0312518732E84621F6432FF", false);

            edo.Authenticate(false, cert);
            var contentBytes = File.ReadAllBytes(filePath);
            try
            {
                var content = new Diadoc.Api.Proto.Events.SignedContent
                {
                    Content = contentBytes
                };

                edo.SendXmlDocument("f7f2df36-f192-48c3-8f22-b9f9f77576ec", 
                    "c7acae2e-f301-46b4-a9cc-d8803239c2db", true,
                    new List<Diadoc.Api.Proto.Events.SignedContent>(new[] { content }), "СЧФДОП");
            }
            catch(WebException webEx)
            {

            }
        }

        [TestMethod]
        public void GenerateShipmentDocumentTest()
        {
            var edo = EdiProcessingUnit.Edo.Edo.GetInstance();
            var crypto = new WinApiCryptWrapper();
            var cert = crypto.GetCertificateWithPrivateKey("0DE1BF746CC43954D0312518732E84621F6432FF", false);
            edo.Authenticate(false, cert);
            var receiverOrganization = edo.GetKontragentByInnKpp("2504000010"); //"c7acae2e-f301-46b4-a9cc-d8803239c2db"
            receiverOrganization.Certificate = cert;

            var consignor = edo.GetKontragentByInnKpp("2539108495");
            consignor.Certificate = crypto.GetCertificateWithPrivateKey("F88D4A47F8C9E5783535D50D4E20F1B0FB421892", false);

            using (var abt = new AbtDbContext())
            {
                string employee = abt.SelectSingleValue("select const_value from ref_const where id = 1200");
                var d = abt.DocJournals.FirstOrDefault(j => j.Id == 937877300);
                

                ////
                var document = new Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphens()
                {
                    Function = Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphensFunction.ДОП,
                    DocumentNumber = d.Code,
                    DocumentDate = d.DeliveryDate?.Date.ToString("dd.MM.yyyy"),
                    Currency = "643",
                    DocumentCreator = ParseCertAttribute(consignor.Certificate.Subject, "SN") + " " + ParseCertAttribute(consignor.Certificate.Subject, "G"),
                    Table = new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTable
                    {
                        TotalSpecified = true,
                        VatSpecified = true,
                        TotalWithVatExcludedSpecified = true,
                        Total = (decimal)d.DocGoodsI.TotalSumm,
                        Vat = (decimal)d.DocGoodsI.TaxSumm,
                        TotalWithVatExcluded = (decimal)(d.DocGoodsI.TotalSumm - d.DocGoodsI.TaxSumm)
                    },
                    TransferInfo = new Diadoc.Api.DataXml.Utd820.Hyphens.TransferInfo
                    {
                        Employee = new Diadoc.Api.DataXml.Employee
                        {
                            Position = "Зав. складом",
                            EmployeeInfo = employee,
                            LastName = employee.Substring(0, employee.IndexOf(' ')),
                            FirstName = employee.Substring(employee.IndexOf(' ') + 1),
                        },
                        OperationInfo = "Товары переданы",
                        TransferDate = d.DeliveryDate?.Date.ToString("dd.MM.yyyy")
                    }
                };


                Diadoc.Api.DataXml.RussianAddress senderAddress = consignor?.Address?.RussianAddress != null ?
                                new Diadoc.Api.DataXml.RussianAddress
                                {
                                    ZipCode = consignor.Address.RussianAddress.ZipCode,
                                    Region = consignor.Address.RussianAddress.Region,
                                    Street = string.IsNullOrEmpty(consignor?.Address?.RussianAddress?.Street) ? null : consignor.Address.RussianAddress.Street,
                                    City = string.IsNullOrEmpty(consignor?.Address?.RussianAddress?.City) ? null : consignor.Address.RussianAddress.City,
                                    Locality = string.IsNullOrEmpty(consignor?.Address?.RussianAddress?.Locality) ? null : consignor.Address.RussianAddress.Locality,
                                    Territory = string.IsNullOrEmpty(consignor?.Address?.RussianAddress?.Territory) ? null : consignor.Address.RussianAddress.Territory,
                                    Building = string.IsNullOrEmpty(consignor?.Address?.RussianAddress?.Building) ? null : consignor.Address.RussianAddress.Building
                                } : null;

                document.Sellers = new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationInfoWithHyphens[]
                {
                        new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationInfoWithHyphens
                        {
                            Item = new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationDetailsWithHyphens
                            {
                                Inn = consignor.Inn,
                                Kpp = consignor.Kpp,
                                OrgType = consignor.Inn.Length == 12 ? Diadoc.Api.DataXml.OrganizationType.IndividualEntity : Diadoc.Api.DataXml.OrganizationType.LegalEntity,
                                OrgName = consignor.Name,
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

                var firstMiddleName = ParseCertAttribute(consignor.Certificate.Subject, "G");
                string signerFirstName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
                string signerMiddleName = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;

                var signer = new[]
                {
                                new Diadoc.Api.DataXml.ExtendedSignerDetails_SellerTitle
                                {
                                    FirstName = signerFirstName,
                                    MiddleName = signerMiddleName,
                                    LastName = ParseCertAttribute(consignor.Certificate.Subject, "SN"),
                                    SignerOrganizationName = ParseCertAttribute(consignor.Certificate.Subject, "CN"),
                                    Inn = ParseCertAttribute(consignor.Certificate.Subject, "ИНН").TrimStart('0'),
                                    Position = ParseCertAttribute(consignor.Certificate.Subject, "T")
                                }
                            };

                if (signer.First().Inn == consignor.Inn)
                    signer.First().SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.LegalEntity;
                else if (signer.First().Inn?.Length == 12)
                {
                    signer.First().SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.PhysicalPerson;
                    signer.First().SignerPowersBase = signer.First().Position;
                }
                else
                    signer.First().SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.IndividualEntity;

                document.Signers = signer;

                document.DocumentShipments = new Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphensDocumentShipment[]
                {
                                new Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphensDocumentShipment
                                {
                                    Name = "Реализация (акт, накладная, УПД)",
                                    Number = $"п/п 1-{d.DocGoodsDetailsIs.Count}, №{d.Code}",
                                    Date = d.DeliveryDate?.Date.ToString("dd.MM.yyyy")
                                }
                };

                var details = new List<Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItem>();

                foreach (var docJournalDetail in d.DocGoodsDetailsIs)
                {
                    var refGood = abt.RefGoods?
                    .FirstOrDefault(r => r.Id == docJournalDetail.IdGood);

                    if (refGood == null)
                        continue;

                    var barCode = abt.RefBarCodes?
                        .FirstOrDefault(b => b.IdGood == docJournalDetail.IdGood && b.IsPrimary == false)?
                        .BarCode;

                    string countryCode = abt.SelectSingleValue("select NUM_CODE from REF_COUNTRIES where id in" +
                        $"(select ID_COUNTRY from REF_GOODS where ID = {refGood.Id})");

                    var vat = (decimal)Math.Round(docJournalDetail.TaxSumm * docJournalDetail.Quantity, 2);
                    var subtotal = Math.Round(docJournalDetail.Quantity * (decimal)docJournalDetail.Price, 2);

                    var detail = new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItem
                    {
                        Product = refGood.Name,
                        Unit = "796",
                        Quantity = docJournalDetail.Quantity,
                        QuantitySpecified = true,
                        VatSpecified = true,
                        Vat = vat,
                        PriceSpecified = true,
                        Price = (decimal)Math.Round(docJournalDetail.Price - docJournalDetail.TaxSumm, 2),
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
                            detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateUcd736AndUtd820.Item0;
                            break;
                        case 10:
                            detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateUcd736AndUtd820.Item10;
                            break;
                        case 18:
                            detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateUcd736AndUtd820.Item18;
                            break;
                        case 20:
                            detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateUcd736AndUtd820.Item20;
                            break;
                        default:
                            detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateUcd736AndUtd820.Item0;
                            break;
                    }

                    decimal idGood = docJournalDetail.IdGood, idDoc = (decimal)d.IdDocMaster;

                    var docGoodDetailLabels = abt?.Database?
                    .SqlQuery<string>($"select DM_LABEL from doc_goods_details_labels where id_doc = {idDoc} and id_good = {idGood}")?
                    .ToList() ?? new List<string>();

                    if (docGoodDetailLabels.Count > 0)
                    {
                        detail.ItemMark = Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemItemMark.Item4;
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
                            detail.ItemIdentificationNumbers[0].Items[j] = doc.Substring(0, 31);
                            j++;
                        }
                    }

                    details.Add(detail);
                }

                document.Table.Item = details.ToArray();
                ////

                var resultDocument = edo.GenerateTitleXml("UniversalTransferDocument",
                    "ДОП", "utd820_05_01_01_hyphen", 0, document);


                resultDocument.SaveContentToFile("C:\\Users\\systech\\Desktop\\Files\\" + resultDocument.FileName);
            }
        }

        [TestMethod]
        public void GetDocumentTest()
        {
            var edo = EdiProcessingUnit.Edo.Edo.GetInstance();
            var crypto = new WinApiCryptWrapper();
            var cert = crypto.GetCertificateWithPrivateKey("439C9C0937713DEEA5334DB7228585A55B11498C", false);
            edo.Authenticate(false, cert, "2504000010");

            var document = edo.GetDocument("6f5b2a5a-dbc6-410b-b462-5951c8cf0ff1", "3bb9e00f-b759-45b4-a264-bf9cc80c4e12");
        }

        [TestMethod]
        public void SendMessagesTest()
        {
            var edi = Edi.GetInstance();
            edi.Authenticate("4607196304521");


            var ordrspXml = new System.Xml.XmlDocument();
            ordrspXml.Load(@"C:\Users\systech\Desktop\ORDRSP.xml");

            var desadvXml = new System.Xml.XmlDocument();
            desadvXml.Load(@"C:\Users\systech\Desktop\DESADV.xml");

            var ordrspProcessor = new OrderResponsesProcessor(new List<string>(new string[] { ordrspXml.OuterXml }));
            ordrspProcessor.Init();
            ordrspProcessor.Run();

            var desadvProcessor = new EdiProcessingUnit.ProcessorUnits.DespatchAdviceProcessor(new List<string>(new string[] { desadvXml.OuterXml }));
            desadvProcessor.Init();
            desadvProcessor.Run();
        }

        [TestMethod]
        public void CreateMarkedDocumentTest()
        {
            var orgInn = "253800573557";
            var personalCertificates = new WinApiCryptWrapper().GetAllGostPersonalCertificates();
            var certs = personalCertificates.Where(c => orgInn == GetOrgInnFromCertificate(c) && c.NotAfter > DateTime.Now).OrderByDescending(c => c.NotBefore);
            var orgCertificate = certs.FirstOrDefault();

            var crypt = new WinApiCryptWrapper(orgCertificate);
            Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphens document = null;

            EdiProcessingUnit.Edo.Edo.GetInstance().Authenticate(false, orgCertificate, orgInn);
            var receiverOrganization = EdiProcessingUnit.Edo.Edo.GetInstance().GetKontragentByInnKpp("254305893970");
            var signerDetails = EdiProcessingUnit.Edo.Edo.GetInstance().GetExtendedSignerDetails(Diadoc.Api.Proto.Invoicing.Signers.DocumentTitleType.UtdSeller);

            using (var abtDbContext = new AbtDbContext())
            {
                decimal idDocJournal = 998537600;
                var docJournal = abtDbContext.DocJournals.First(d => d.Id == idDocJournal);
                string employee = abtDbContext.SelectSingleValue("select const_value from ref_const where id = 1200");

                var labels = (from label in abtDbContext.DocGoodsDetailsLabels
                              where label.IdDocSale == idDocJournal
                              select label)?.ToList() ?? new List<DocGoodsDetailsLabels>();

                //KonturEdoClient.HonestMark.HonestMarkClient.GetInstance().Authorization(orgCertificate);
                //var markedCodesInfo = KonturEdoClient.HonestMark.HonestMarkClient.GetInstance().GetMarkCodesInfo(KonturEdoClient.HonestMark.ProductGroupsEnum.None, 
                //    labels.Select(l=>l.DmLabel).ToArray()).Where(l => l?.CisInfo?.Status != "RETIRED").ToList();

                //labels = labels.Where(l => markedCodesInfo.Exists(m => m.CisInfo.RequestedCis == l.DmLabel)).ToList();

                Diadoc.Api.DataXml.RussianAddress receiverAddress = receiverOrganization?.Address?.RussianAddress != null ?
                new Diadoc.Api.DataXml.RussianAddress
                {
                    ZipCode = receiverOrganization.Address.RussianAddress.ZipCode,
                    Region = receiverOrganization.Address.RussianAddress.Region ?? "25",
                    Street = string.IsNullOrEmpty(receiverOrganization?.Address?.RussianAddress?.Street) ? null : receiverOrganization.Address.RussianAddress.Street,
                    City = string.IsNullOrEmpty(receiverOrganization?.Address?.RussianAddress?.City) ? null : receiverOrganization.Address.RussianAddress.City,
                    Locality = string.IsNullOrEmpty(receiverOrganization?.Address?.RussianAddress?.Locality) ? null : receiverOrganization.Address.RussianAddress.Locality,
                    Territory = string.IsNullOrEmpty(receiverOrganization?.Address?.RussianAddress?.Territory) ? null : receiverOrganization.Address.RussianAddress.Territory,
                    Building = string.IsNullOrEmpty(receiverOrganization?.Address?.RussianAddress?.Building) ? null : receiverOrganization.Address.RussianAddress.Building
                } : null;

                var senderAddress = new Diadoc.Api.DataXml.RussianAddress
                {
                    ZipCode = "690033",
                    City = "Владивосток",
                    Street = "Постышева",
                    Building="31",
                    Region="25",
                    Apartment = string.Empty,
                    Block=string.Empty,
                    Locality=string.Empty,
                    Territory = string.Empty
                };

                document = CreateShipmentDocument(abtDbContext, docJournal, senderAddress, orgInn, null, "ИП Пойс Нина Филипповна", orgCertificate,
                    receiverAddress, "254305893970", null, "ИП Мигеркина Лада Романовна", labels, docJournal.Code, employee, signerDetails, true);
            }

            var generatedFile = EdiProcessingUnit.Edo.Edo.GetInstance().GenerateTitleXml("UniversalTransferDocument",
            "ДОП", "utd820_05_01_01_hyphen", 0, document);

            generatedFile.SaveContentToFile($@"C:\Users\systech\Desktop\Files\{generatedFile.FileName}");

            //byte[] signature = crypt.Sign(generatedFile.Content, true);
            //var message = EdiProcessingUnit.Edo.Edo.GetInstance().SendXmlDocument(_consignor.OrgId, SelectedOrganization.OrgId, false, generatedFile.Content, "ДОП", signature);
        }

        [TestMethod]
        public void FinDbTest()
        {
            SetFinDbConfiguration();
            var finDbController = WebService.Controllers.FinDbController.GetInstance();
            try
            {
                var result = finDbController.GetDocOrderInfoByIdDocAndOrderStatus(1083997900);
                var ediChannels = finDbController.GetEdiChannels();
            }
            catch(WebException webEx)
            {

            }
            catch (Exception ex)
            {

            }
        }

        [TestMethod]
        public void ExecuteProceduresTest()
        {
            using(var ediDbContext = new EdiDbContext())
            {
                try
                {
                    ediDbContext.ExecuteProcedure("EDI.TRANSFER_GOOD_MAPPING_FROM_EDI");
                }
                catch(Exception ex)
                {

                }
            }
        }

        [TestMethod]
        public void SendResponsesTest()
        {
            using (var ediDbContext = new EdiDbContext())
            {
                var processorFactory = new EdiProcessingUnit.EdiProcessorFactory(ediDbContext);
                processorFactory.OrganizationGln = "4607971729990";

                var docOrders = ediDbContext.DocOrders.Where(d => 
                d.Number == "РОТКз068314" && d.GlnSender == "4650093209994" && d.OrderDate > new DateTime(2024, 1, 1)).ToList();
                //processorFactory.RunProcessor(new OrderResponsesProcessor(docOrders));
                //processorFactory.RunProcessor(new EdiProcessingUnit.ProcessorUnits.DespatchAdviceProcessor(docOrders));
            }
        }

        [TestMethod]
        public void GetDocumentTypesTest()
        {
            var edo = EdiProcessingUnit.Edo.Edo.GetInstance();
            var crypto = new WinApiCryptWrapper();
            var cert = crypto.GetCertificateWithPrivateKey("DEEBCA5A51641044D042AEFA0E9569EEA57CE716", false);
            edo.Authenticate(false, cert, "2504000010");
            var docType = edo.GetDocumentTypes().DocumentTypes.FirstOrDefault(d => d.Name == "Torg2");
            var document = edo.GetDocument("13d9568b-5e35-4084-9e9e-2435ca788554", "e527de5d-c092-43a4-98b8-6117d64c54d2");
        }

        [TestMethod]
        public void GetEdoDocumentsForSendingTest()
        {
            try
            {
                var edo = EdiProcessingUnit.Edo.Edo.GetInstance();
                var crypto = new WinApiCryptWrapper();
                var cert = crypto.GetCertificateWithPrivateKey("439C9C0937713DEEA5334DB7228585A55B11498C", false);
                edo.Authenticate(false, cert, "2504000010");

                var organization = new EdiProcessingUnit.Edo.Models.Kontragent
                {
                    Name = "ООО \"ВИРЭЙ\"",
                    Inn = "2504000010",
                    Kpp = "253901001",
                    EmchdId = "b591a89f-ffe1-439c-9f04-e02444839231",
                    EmchdBeginDate = DateTime.ParseExact("2024-11-13", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                    EmchdEndDate = DateTime.ParseExact("2029-11-13", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                    EmchdPersonInn = "253605132573",
                    EmchdPersonSurname = "Бельтюкова",
                    EmchdPersonName = "Ирина",
                    EmchdPersonPatronymicSurname = "Васильевна",
                    EmchdPersonPosition = "Директор по продажам"
                };

                edo.SetOrganizationParameters(organization);
                var documents = GetDocumentsForEdoAutomaticSend(organization);
                var document = documents.FirstOrDefault(d => d.GlnShipTo == "4610018015192");
                //var edoValuesPairs = document?.RefEdoGoodChannel?.EdoValuesPairs?.ToList();

                var clientEdoProcessor = new SendEdoDocumentsProcessingUnit.Processors.ClientEdoProcessor();
                var universalDocument = clientEdoProcessor.GetUniversalDocumentV2(document, organization, document.Details, document.RefEdoGoodChannel);
                var generatedFile = edo.GenerateTitleXml("UniversalTransferDocument", "СЧФДОП", "utd970_05_03_01", 0, universalDocument);
                var xml = Encoding.GetEncoding(1251).GetString(generatedFile.Content);
                generatedFile.SaveContentToFile($"C:\\Users\\systech\\Desktop\\Files\\{generatedFile.FileName}");
            }
            catch (Exception ex)
            {

            }
        }

        private List<EdiProcessingUnit.Edo.Models.UniversalTransferDocumentV2> GetDocumentsForEdoAutomaticSend(EdiProcessingUnit.Edo.Models.Kontragent organization)
        {
            using (var abt = new AbtDbContext())
            {
                var fileController = new WebService.Controllers.FileController();
                var dateTimeLastPeriod = fileController.GetApplicationConfigParameter<DateTime>("KonturEdo", "DocsDateTime");

                var fromDateParam = new Oracle.ManagedDataAccess.Client.OracleParameter(@"FromDate", dateTimeLastPeriod);
                fromDateParam.OracleDbType = Oracle.ManagedDataAccess.Client.OracleDbType.Date;

                string sqlString = string.Empty;
                var properties = typeof(EdiProcessingUnit.Edo.Models.UniversalTransferDocumentV2).GetProperties();

                foreach (var property in properties)
                {
                    var colAttribute = property?.GetCustomAttributes(false)?
                        .FirstOrDefault(c => c as System.ComponentModel.DataAnnotations.Schema.ColumnAttribute != null) as System.ComponentModel.DataAnnotations.Schema.ColumnAttribute;

                    if (colAttribute != null)
                        sqlString += sqlString == string.Empty ? $"{colAttribute.Name} as {property.Name}" : $", {colAttribute.Name} as {property.Name}";
                }

                sqlString = $"select {sqlString} from VIEW_INVOICES_EDO_AUTOMATIC_1 D where D.DOC_DATE >= :FromDate and D.ORDER_DATE >= :FromDate" +
                    $" and SELLER_INN = '{organization.Inn}' and SELLER_KPP = '{organization.Kpp}'" +
                    " and exists(select * from log_actions where id_object = D.ID_DOC_MASTER and id_action = D.PERMISSION_STATUS and action_datetime > sysdate - 14)";
                var docs = abt.Database.SqlQuery<EdiProcessingUnit.Edo.Models.UniversalTransferDocumentV2>(sqlString, fromDateParam).ToList();

                docs = docs.Select(u =>
                {
                    try
                    {
                        return u.Init(abt);
                    }
                    catch (Exception ex)
                    {
                        //_log.Log(ex);
                        //MailReporter.Add(ex, $"Ошибка в документе {u.InvoiceNumber}: ");
                        return null;
                    }
                }).Where(u => u != null).ToList();
                var edoValuesPairs = docs?.FirstOrDefault(d => d.GlnShipTo == "4610018015192")?.RefEdoGoodChannel?.EdoValuesPairs?.ToList();
                return docs;
            }
        }

        private void SetFinDbConfiguration()
        {
            var finDbController = WebService.Controllers.FinDbController.GetInstance();

            var data = finDbController.GetCipherContentForConnect("KonturEdo");
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
        }

        private string ParseCertAttribute(string certData, string attributeName)
        {
            string result = String.Empty;
            try
            {
                if (certData == null || certData == "") return result;

                attributeName = attributeName + "=";

                if (!certData.Contains(attributeName)) return result;

                int start = certData.IndexOf(attributeName);

                if (start > 0 && !certData.Substring(0, start).EndsWith(" "))
                {
                    attributeName = " " + attributeName;

                    if (!certData.Contains(attributeName)) return result;
                }

                start = certData.IndexOf(attributeName) + attributeName.Length;

                int length = certData.IndexOf('=', start) == -1 ? certData.Length - start : certData.IndexOf(", ", start) - start;

                if (length == 0) return result;
                if (length > 0)
                {
                    result = certData.Substring(start, length);

                }
                else
                {
                    result = certData.Substring(start);
                }
                return result;

            }
            catch (Exception)
            {
                return result;
            }
        }

        private void ChangeGateway(string newIpAddress)
        {
            var managementClass = new System.Management.ManagementClass( "Win32_NetworkAdapterConfiguration" );
            var objMOC = managementClass.GetInstances();

            var hostname = Dns.GetHostName();
            var localIpAddress = Dns.GetHostAddresses( hostname )?.Where( i => !i.IsIPv6LinkLocal )?.FirstOrDefault()?.ToString();

            if (!string.IsNullOrEmpty( localIpAddress ))
                foreach (var objMO in objMOC)
                {
                    var currentIpAddress = (string[])((System.Management.ManagementObject)objMO)["IPAddress"];
                    var gateWay = (string[])((System.Management.ManagementObject)objMO)["DefaultIPGateway"];

                    if ((currentIpAddress?.Any( c => c == localIpAddress ) ?? false))
                    {
                        var objNewGate = ((System.Management.ManagementObject)objMO).GetMethodParameters( "SetGateways" );
                        objNewGate["DefaultIPGateway"] = new string[] { newIpAddress };

                        var objSetIP = ((System.Management.ManagementObject)objMO).InvokeMethod( "SetGateways", objNewGate, null );
                    }
                }
        }

        public string GetOrgInnFromCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate)
        {
            var inn = ParseCertAttribute(certificate.Subject, "ИНН").TrimStart('0');

            if (string.IsNullOrEmpty(inn) || inn.Length == 12)
            {
                var crypt = new WinApiCryptWrapper(certificate);
                var orgInn = crypt.GetValueBySubjectOid("1.2.643.100.4");

                if(!string.IsNullOrEmpty(orgInn))
                    inn = orgInn;
            }

            return inn;
        }

        private Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphens CreateShipmentDocument(
            AbtDbContext abt,
            DocJournal d,
            Diadoc.Api.DataXml.RussianAddress senderOrganizationRussianAddress,
            string senderOrganizationInn,
            string senderOrganizationKpp,
            string senderOrganizationName,
            System.Security.Cryptography.X509Certificates.X509Certificate2 senderOrganizationCertificate,
            Diadoc.Api.DataXml.RussianAddress receiverOrganizationRussianAddress,
            string receiverOrganizationInn,
            string receiverOrganizationKpp,
            string receiverOrganizationName,
            List<DocGoodsDetailsLabels> detailsLabels, 
            string documentNumber, 
            string employee = null,
            Diadoc.Api.Proto.Invoicing.Signers.ExtendedSignerDetails signerDetails = null,
            bool considerOnlyLabeledGoods = false)
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
                Currency = "643",
                DocumentCreator = ParseCertAttribute(senderOrganizationCertificate.Subject, "SN") + " " + ParseCertAttribute(senderOrganizationCertificate.Subject, "G"),
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
                document.TransferInfo.Employee = new Diadoc.Api.DataXml.Employee
                {
                    Position = "Зав. складом",
                    EmployeeInfo = employee,
                    LastName = employee.Substring(0, employee.IndexOf(' ')),
                    FirstName = employee.Substring(employee.IndexOf(' ') + 1)
                };
            }


            Diadoc.Api.DataXml.RussianAddress senderAddress = senderOrganizationRussianAddress != null ?
                            new Diadoc.Api.DataXml.RussianAddress
                            {
                                ZipCode = senderOrganizationRussianAddress.ZipCode,
                                Region = senderOrganizationRussianAddress.Region,
                                Street = string.IsNullOrEmpty(senderOrganizationRussianAddress?.Street) ? null : senderOrganizationRussianAddress.Street,
                                City = string.IsNullOrEmpty(senderOrganizationRussianAddress?.City) ? null : senderOrganizationRussianAddress.City,
                                Locality = string.IsNullOrEmpty(senderOrganizationRussianAddress?.Locality) ? null : senderOrganizationRussianAddress.Locality,
                                Territory = string.IsNullOrEmpty(senderOrganizationRussianAddress?.Territory) ? null : senderOrganizationRussianAddress.Territory,
                                Building = string.IsNullOrEmpty(senderOrganizationRussianAddress?.Building) ? null : senderOrganizationRussianAddress.Building
                            } : null;

            document.Sellers = new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationInfoWithHyphens[]
            {
                new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationInfoWithHyphens
                {
                    Item = new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationDetailsWithHyphens
                    {
                        Inn = senderOrganizationInn,
                        Kpp = senderOrganizationKpp,
                        OrgType = senderOrganizationInn.Length == 12 ? Diadoc.Api.DataXml.OrganizationType.IndividualEntity : Diadoc.Api.DataXml.OrganizationType.LegalEntity,
                        OrgName = senderOrganizationName,
                        Address = new Diadoc.Api.DataXml.Address
                        {
                            Item = senderAddress
                        }
                    }
                }
            };

            Diadoc.Api.DataXml.RussianAddress receiverAddress = receiverOrganizationRussianAddress != null ?
                new Diadoc.Api.DataXml.RussianAddress
                {
                    ZipCode = receiverOrganizationRussianAddress.ZipCode,
                    Region = receiverOrganizationRussianAddress.Region,
                    Street = string.IsNullOrEmpty(receiverOrganizationRussianAddress?.Street) ? null : receiverOrganizationRussianAddress.Street,
                    City = string.IsNullOrEmpty(receiverOrganizationRussianAddress?.City) ? null : receiverOrganizationRussianAddress.City,
                    Locality = string.IsNullOrEmpty(receiverOrganizationRussianAddress?.Locality) ? null : receiverOrganizationRussianAddress.Locality,
                    Territory = string.IsNullOrEmpty(receiverOrganizationRussianAddress?.Territory) ? null : receiverOrganizationRussianAddress.Territory,
                    Building = string.IsNullOrEmpty(receiverOrganizationRussianAddress?.Building) ? null : receiverOrganizationRussianAddress.Building
                } : null;

            document.Buyers = new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationInfoWithHyphens[]
            {
                new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationInfoWithHyphens
                {
                    Item = new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationDetailsWithHyphens
                    {
                        Inn = receiverOrganizationInn,
                        Kpp = receiverOrganizationKpp,
                        OrgType = receiverOrganizationInn.Length == 12 ? Diadoc.Api.DataXml.OrganizationType.IndividualEntity : Diadoc.Api.DataXml.OrganizationType.LegalEntity,
                        OrgName = receiverOrganizationName,
                        Address = new Diadoc.Api.DataXml.Address
                        {
                            Item = receiverAddress
                        }
                    }
                }
            };

            var firstMiddleName = ParseCertAttribute(senderOrganizationCertificate.Subject, "G");
            string signerFirstName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
            string signerMiddleName = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;

            var signer = new[]
            {
                                new Diadoc.Api.DataXml.ExtendedSignerDetails_SellerTitle
                                {
                                    SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.LegalEntity,
                                    FirstName = signerFirstName,
                                    MiddleName = signerMiddleName,
                                    LastName = ParseCertAttribute(senderOrganizationCertificate.Subject, "SN"),
                                    SignerOrganizationName = ParseCertAttribute(senderOrganizationCertificate.Subject, "CN"),
                                    Inn = GetOrgInnFromCertificate(senderOrganizationCertificate),
                                    Position = ParseCertAttribute(senderOrganizationCertificate.Subject, "T")
                                }
                            };


            if (signer.First().Inn?.Length == 12)
                signer.First().SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.IndividualEntity;

            //if (signerDetails != null)
            //{
            //    signer.First().Inn = GetOrgInnFromCertificate(senderOrganizationCertificate);
            //    signer.First().SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.LegalEntity;
            //    signer.First().SignerPowers = (Diadoc.Api.DataXml.ExtendedSignerDetails_SellerTitleSignerPowers)Convert.ToInt32(signerDetails.SignerPowers);

            //    if (signerDetails.SignerStatus == Diadoc.Api.Proto.Invoicing.Signers.SignerStatus.SellerEmployee)
            //        signer.First().SignerStatus = Diadoc.Api.DataXml.ExtendedSignerDetailsSignerStatus.SellerEmployee;
            //    else if (signerDetails.SignerStatus == Diadoc.Api.Proto.Invoicing.Signers.SignerStatus.InformationCreatorEmployee)
            //        signer.First().SignerStatus = Diadoc.Api.DataXml.ExtendedSignerDetailsSignerStatus.InformationCreatorEmployee;
            //    else if (signerDetails.SignerStatus == Diadoc.Api.Proto.Invoicing.Signers.SignerStatus.OtherOrganizationEmployee)
            //        signer.First().SignerStatus = Diadoc.Api.DataXml.ExtendedSignerDetailsSignerStatus.OtherOrganizationEmployee;
            //    else if (signerDetails.SignerStatus == Diadoc.Api.Proto.Invoicing.Signers.SignerStatus.AuthorizedPerson)
            //        signer.First().SignerStatus = Diadoc.Api.DataXml.ExtendedSignerDetailsSignerStatus.AuthorizedPerson;
            //}

            document.Signers = signer;

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
                    var refGood = abt.RefGoods?
                    .FirstOrDefault(r => r.Id == docJournalDetail.IdGood);

                    if (refGood == null)
                        continue;

                    var docGoodDetailLabels = detailsLabels.Where(l => l.IdGood == docJournalDetail.IdGood).ToList();

                    if (considerOnlyLabeledGoods && docGoodDetailLabels.Count == 0)
                        continue;

                    var barCode = abt.RefBarCodes?
                        .FirstOrDefault(b => b.IdGood == docJournalDetail.IdGood && b.IsPrimary == false)?
                        .BarCode;

                    string countryCode = abt.SelectSingleValue("select NUM_CODE from REF_COUNTRIES where id in" +
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
                        Unit = "796",
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
                            detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateUcd736AndUtd820.Item0;
                            break;
                        case 10:
                            detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateUcd736AndUtd820.Item10;
                            break;
                        case 18:
                            detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateUcd736AndUtd820.Item18;
                            break;
                        case 20:
                            detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateUcd736AndUtd820.Item20;
                            break;
                        default:
                            detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateUcd736AndUtd820.Item0;
                            break;
                    }

                    decimal idGood = docJournalDetail.IdGood;

                    if (docGoodDetailLabels.Count > 0)
                    {
                        detail.ItemMark = Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemItemMark.Item4;
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
                    var refGood = abt.RefGoods?
                    .FirstOrDefault(r => r.Id == docJournalDetail.IdGood);

                    if (refGood == null)
                        continue;

                    var docGoodDetailLabels = detailsLabels.Where(l => l.IdGood == docJournalDetail.IdGood).ToList();

                    if (considerOnlyLabeledGoods && docGoodDetailLabels.Count == 0)
                        continue;

                    var barCode = abt.RefBarCodes?
                        .FirstOrDefault(b => b.IdGood == docJournalDetail.IdGood && b.IsPrimary == false)?
                        .BarCode;

                    string countryCode = abt.SelectSingleValue("select NUM_CODE from REF_COUNTRIES where id in" +
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
                        Unit = "796",
                        Quantity = quantity,
                        QuantitySpecified = true,
                        PriceSpecified = true,
                        Price = (decimal)Math.Round(docJournalDetail.Price - docJournalDetail.DiscountSumm, 2),
                        SubtotalSpecified = true,
                        Subtotal = subtotal,
                        SubtotalWithVatExcludedSpecified = true,
                        WithoutVat = Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemWithoutVat.@true,
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
                        detail.ItemMark = Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemItemMark.Item4;
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
                document.Table.WithoutVat = Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableWithoutVat.@true;

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
    }
}
