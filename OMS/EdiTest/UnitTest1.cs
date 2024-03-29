﻿using System;
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
        public void TestConnectDataBaseAbt()
        {
            AbtDbContext _abtDbContext;
            using (_abtDbContext = new AbtDbContext( "User Id=redmine;Password=Ecwiegrool;Data Source=192.168.2.37/orcl", true ))
            {
                List<DocJournal> traderDocs = _abtDbContext
                                .DocJournals
                                .Where( doc => doc.Id == 881296300
                                    && doc.ActStatus >= 4 )
                                .ToList();

                var contractors = _abtDbContext.RefContractors.ToList();
            }
        }

        [TestMethod]
        public void TestConnectDataBaseEdi()
        {
            EdiDbContext _ediDbContext;

            using (_ediDbContext = new EdiDbContext( "User Id=EDI;Password=byntuhfwbz;Data Source=192.168.2.13/orcl.vladivostok.wera" ))
            {
                var connectedBuyers = _ediDbContext.ConnectedBuyers.ToList();
                var mapGoods = _ediDbContext.MapGoods//.ToList();
                    .Include( "MapGoodByBuyers" ).ToList();
            }
        }

        [TestMethod]
        public void TestChangeGateway()
        {
            ChangeGateway( "192.168.2.15" );
        }

        [TestMethod]
        public void ExportExcelDataTest()
        {
            var _ediDbContext = new EdiDbContext( "Data Source=192.168.2.18/orcl.findb;User Id=edi;Password=byntuhfwbz" );
            var orders = _ediDbContext.DocOrders.ToList();

            ExcelColumnCollection columnCollection = new ExcelColumnCollection();

            columnCollection.AddColumn("Id", "ID");
            columnCollection.AddColumn( "Number", "Номер" );
            columnCollection.AddColumn( "Status", "Статус", ExcelType.Int64 );

            List<ExcelDocumentData> docs = new List<ExcelDocumentData>();

            var doc = new ExcelDocumentData( columnCollection, orders.ToArray() );
            doc.SheetName = "Лист1";

            docs.Add( doc );

            ExcelFileWorker worker = new ExcelFileWorker( "C:\\Users\\systech\\Desktop\\export\\Orders.xls", docs );

            worker.ExportData();
            worker.SaveFile();
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
            string password = "byntuhfwbz";

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
            edo.Authenticate(true);
            var userDocCreator = edo.GetUniversalTransferDocumentWithHyphens(new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationDetailsWithHyphens
            {
                Inn = "7750370238",
                Kpp = "770100101",
                OrgType = Diadoc.Api.DataXml.OrganizationType.LegalEntity,
                OrgName = "ЗАО Очень Древний Папирус",
                Address = new Diadoc.Api.DataXml.Address
                {
                    Item = new Diadoc.Api.DataXml.RussianAddress
                    {
                        Region = "66"
                    }
                }
            },
            new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationDetailsWithHyphens
            {
                Inn = "9500000005",
                Kpp = "667301001",
                OrgType = Diadoc.Api.DataXml.OrganizationType.LegalEntity,
                OrgName = "ООО Тестовое Юрлицо обычное",
                Address = new Diadoc.Api.DataXml.Address
                {
                    Item = new Diadoc.Api.DataXml.RussianAddress
                    {
                        Region = "66"
                    }
                }
            },
            "01.01.2020",
            "134",
            Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphensFunction.СЧФ,
            null,
            new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTable
            {
                Item = new[]
                    {
                        new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItem
                        {
                            Product = "товар",
                            Unit = "796",
                            Quantity = 10,
                            QuantitySpecified = true,
                            WithoutVat = Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemWithoutVat.True,
                            SubtotalSpecified = true,
                            Subtotal = 0,
                            ItemMark = Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemItemMark.PropertyRights,
                            ItemIdentificationNumbers = new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemItemIdentificationNumber[]
                            {
                                new Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableItemItemIdentificationNumber
                                {
                                    ItemsElementName = new []{ Diadoc.Api.DataXml.ItemsChoiceType.Unit },
                                    Items = new string[]{"4564385435465"},
                                    TransPackageId = "6352386234233"
                                }
                            }
                        }
                    },

                WithoutVat = Diadoc.Api.DataXml.Utd820.Hyphens.InvoiceTableWithoutVat.True,
                TotalSpecified=true,
                Total=7960
            },
            new[]
            {
                new Diadoc.Api.DataXml.ExtendedSignerDetails_SellerTitle
                {
                    SignerType = Diadoc.Api.DataXml.ExtendedSignerDetailsBaseSignerType.LegalEntity,
                    FirstName = "Иван",
                    MiddleName = "Иванович",
                    LastName = "Иванов",
                    SignerOrganizationName = "ЗАО Очень Древний Папирус",
                    Inn = "7750370238",
                    Position = "директор"
                }
            },
            0, "643", "Писаренко А.Н.");

            try
            {
                var docFile = edo.GenerateTitleXml("UniversalTransferDocument",
                    "СЧФ", "utd820_05_01_01_hyphen", 0, userDocCreator);

                var xmlString = Encoding.GetEncoding(1251).GetString(docFile.Content);

                docFile.SaveContentToFile($"C:\\Users\\systech\\Desktop\\{docFile.FileName}");

                var universalTransferDocumentString = Encoding.UTF8.GetString(userDocCreator.SerializeToXml());
                var reporter = new Reporter.Reporters.UniversalTransferDocumentReporter();
                universalTransferDocumentString = string.Join("\n", universalTransferDocumentString.Split('\n').Skip(1).ToArray());
                reporter.LoadFromXml(universalTransferDocumentString);
            }
            catch(WebException webEx)
            {

            }
        }

        [TestMethod]
        public void SignCryptographyTest()
        {
            var filePath = "C:\\Users\\systech\\Desktop\\ON_NSCHFDOPPRMARK_2BM-7750370234-4012052808304878702630000000000_2BM-7750370234-4012052808304878702630000000004_20210702_f9e2a63a-c7af-4642-ad60-67b5c7c9a221.xml";
            var signerCertificate = new System.Security.Cryptography.X509Certificates.
                X509Certificate2("C:\\Users\\systech\\Desktop\\Certs\\CertsForTest\\newcert.cer");

            var crypt = new WinApiCryptWrapper(signerCertificate);
            crypt.SetupPinCode("1");

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
                edo.SendXmlDocument("f7f2df36-f192-48c3-8f22-b9f9f77576ec", 
                    "c7acae2e-f301-46b4-a9cc-d8803239c2db", true,
                    contentBytes, "СЧФДОП");
            }
            catch(WebException webEx)
            {

            }
        }

        [TestMethod]
        public void ReceiveOrderTest()
        {
            string filePath = null;

            if (!string.IsNullOrEmpty(filePath))
            {
                var xmlDocument = new System.Xml.XmlDocument();
                xmlDocument.Load(filePath);

                var ordersProcessor = new OrdersProcessor(new List<string>(new string[]
                {
                xmlDocument.LastChild.OuterXml
                }));

                var ediDbContext = new EdiDbContext("Data Source=192.168.2.18/orcl.findb;User Id=edi;Password=byntuhfwbz");

                ordersProcessor.Init(null, ediDbContext);
                ordersProcessor.Run();
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

            using (var abt = new AbtDbContext("Data Source=192.168.2.13/orcl.vladivostok.wera;User Id=edi;Password=byntuhfwbz"))
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
                        Employee = new Diadoc.Api.DataXml.Utd820.Hyphens.Employee
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

                document.UseSignerDetails(signer);

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

                    var docGoodDetailLabels = abt?.Database?
                    .SqlQuery<string>($"select DM_LABEL from doc_goods_details_labels where id_doc = {idDoc} and id_good = {idGood}")?
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
            var cert = crypto.GetCertificateWithPrivateKey("1AE6FE62C7DEE1C4CA5AAAF9A9B33AFA95640753", false);
            edo.Authenticate(false, cert, "2538150215");

            var document = edo.GetDocument("c28f297c-e379-494f-8972-6768c648f279", "555d0768-0f41-436d-b6e4-6f04dab14ea3");
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

        private void InsertMapGoods(string mainGln, List<string> barCodes)
        {
            AbtDbContext _abtDbContext;
            var barCodesStr = string.Join( "', '", barCodes );
            var _ediDbContext = new EdiDbContext( "Data Source=192.168.2.18/orcl.findb;User Id=edi;Password=byntuhfwbz" );

            string sql = "SELECT rg.id as ID, rbc.bar_code as BAR_CODE, rg.name as NAME " +
                "FROM abt.ref_goods rg, ABT.REF_BAR_CODES RBC " +
                "WHERE RBC.id_good = rg.id AND rbc.bar_code in ('"+ barCodesStr + 
                "') AND RBC.ID_GOOD IN(SELECT id_object FROM REF_GROUP_ITEMS WHERE ID_PARENT IN " +
                "(SELECT id FROM ABT.REF_GROUPS CONNECT BY PRIOR id = id_parent START WITH ID = 485596300))";

            using(_abtDbContext = new AbtDbContext( "Data Source=192.168.2.13/orcl.vladivostok.wera;User Id=edi;Password=byntuhfwbz" ))
            {
                var command = new Oracle.ManagedDataAccess.Client.OracleCommand();
                command.Connection = (Oracle.ManagedDataAccess.Client.OracleConnection)_abtDbContext.Database.Connection;
                command.CommandType = System.Data.CommandType.Text;
                command.CommandText = sql;

                command.Connection.Open();
                var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var mapGood = new MapGood() {
                        Id = Guid.NewGuid().ToString(),
                        BarCode = (string)reader["BAR_CODE"],
                        IdGood = (decimal)reader["ID"],
                        Name = (string)reader["NAME"],
                        MapGoodByBuyers = new List<MapGoodByBuyer>()
                    };

                    _ediDbContext.MapGoods.Add(mapGood);

                    var glnItemGood = new MapGoodByBuyer() {
                        Gln = mainGln,
                        IdMapGood = mapGood.Id,
                        MapGood = mapGood
                    };

                    mapGood.MapGoodByBuyers.Add( glnItemGood );
                }

                reader.Close();
                command.Connection.Close();
            }
            _ediDbContext.SaveChanges();
        }

        private void GetOrdersByNumbers(string buyerGln, List<string> orderNumbers)
        {
            var edi = Edi.GetInstance();
            edi.Authenticate("4607971729990");

            var events = edi.GetNewEvents();

            foreach (var e in events)
            {
                if (e?.EventContent?.GetType() != typeof( SkbKontur.EdiApi.Client.Types.Messages.BoxEventsContents.Inbox.NewInboxMessageEventContent ))
                    continue;

                var content = (SkbKontur.EdiApi.Client.Types.Messages.BoxEventsContents.Inbox.NewInboxMessageEventContent)e.EventContent;

                if (content != null)
                {
                    var messageData = edi.NewInboxMessageEventHandler( content );
                    var messageBodyString = Encoding.UTF8.GetString( messageData.MessageBody, 0, messageData.MessageBody.Length );

                    if (messageBodyString.Contains( "UNH+" ))
                        continue;

                    if (string.IsNullOrEmpty( messageBodyString ))
                        continue;

                    var messageString = string.Join( "\n", messageBodyString.Split( '\n' ).Skip( 1 ).ToArray() );

                    var message = Xml.DeserializeString<EDIMessage>( messageString );

                    if (string.IsNullOrEmpty( message?.Order?.Number ))
                        continue;

                    if (orderNumbers.Exists(n => n==message?.Order?.Number) && buyerGln == message?.Order?.Buyer?.gln)
                    {
                        var ordersList = new List<string>();
                        ordersList.Add( messageBodyString );

                        var ordersProcessor = new OrdersProcessor( ordersList );
                        var _ediDbContext = new EdiDbContext( "Data Source=192.168.2.18/orcl.findb;User Id=edi;Password=byntuhfwbz" );

                        if (_ediDbContext.DocOrders.FirstOrDefault( d =>
                          d.GlnBuyer == buyerGln && d.Number == message.Order.Number ) != null)
                            continue;

                        ordersProcessor.Init( edi, _ediDbContext );
                        ordersProcessor.Run();
                    }
                }
            }
        }
    }
}
