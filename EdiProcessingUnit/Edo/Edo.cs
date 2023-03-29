using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UtilitesLibrary.Logger;
using Diadoc;
using Diadoc.Api;
using Diadoc.Api.Http;
using Diadoc.Api.Proto;
using Diadoc.Api.DataXml;
using Diadoc.Api.Proto.Events;
using Diadoc.Api.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using Diadoc.Api.DataXml.Utd820.Hyphens;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using DataContextManagementUnit.DataAccess.Contexts.Edi;

namespace EdiProcessingUnit.Edo
{
	public class Edo
	{
		private const string TokenFileName = "edocache";
		private UtilityLog _log = UtilityLog.GetInstance();
		private EdoTokenCache _cache { get; set; }
		private readonly UtilitesLibrary.ConfigSet.Config _config = UtilitesLibrary.ConfigSet.Config.GetInstance();
		private string _authToken => _cache.Token ?? "";
		private string _partyId => _cache.PartyId ?? "";
        private string _orgCertInn;
        private string _actualBoxId;
		private DiadocHttpApi _api;
		private X509Certificate2 _certificate;

		public void SendUtd820Kontur(UniversalTransferDocumentWithHyphens doc)
		{
			//GeneratedFile f = CallApiSafe( new Func<GeneratedFile>( () 
			//	=> _api.GenerateUniversalTransferDocumentXmlForBuyer( _authToken ) ) );
			/*
			User user = CallApiSafe( new Func<User>( () => _api.GetMyUser( _authToken ) ) );
			OrganizationList userPerm = CallApiSafe( new Func<OrganizationList>( () => _api.GetMyOrganizations( _authToken, false ) ) );
			*/
		}

        
		public UniversalTransferDocumentWithHyphens GenerateUtd820Kontur(ViewInvoicHead doc , AbtDbContext AbtDbContext, EdiDbContext EdiDbContext)
		{
			var canSend = _api.CanSendInvoice( _authToken, _actualBoxId, _certificate.RawData );
			//var docTypes = _api.GetDocumentTypes( _authToken, _actualBoxId );
			/*
			var si = _api.GenerateSenderTitleXml(
				_authToken,
				_actualBoxId,
				docTypes.DocumentTypes.First().Name,
				docTypes.DocumentTypes.First().Functions.First().Name,
				docTypes.DocumentTypes.First().Functions.First().Versions.First().Version,
				);
				*/
			var org = _api.GetOrganizationByInnKpp( doc.CustomerInn, doc.CustomerKpp );

            
            var utd = GetUniversalTransferDocumentWithHyphens(
                new ExtendedOrganizationDetails_ManualFilling()
                {
                    BankName = doc.SellerBankName,
                    Phone = doc.SellerPhone,
                    Okpo = doc.SellerOkpo,
                    ShortOrgName = doc.SellerName,
                    CorrespondentAccount = doc.SellerCorAccount,
                },
                new ExtendedOrganizationDetails_ManualFilling()
                {
                    BankName = doc.CustomerBankName,
                    Phone = doc.CustomerPhone,
                    Okpo = doc.CustomerOkpo,
                    ShortOrgName = doc.CustomerName,
                    CorrespondentAccount = doc.CustomerCorAccount,
                },
                doc.DocDatetime.Value.ToShortDateString(),
                doc.Code,
                UniversalTransferDocumentWithHyphensFunction.СЧФДОП,
                new TransferInfo()
                {
                    Employee = new Diadoc.Api.DataXml.Utd820.Hyphens.Employee() { Position = doc.StorekeeperProf, EmployeeInfo = doc.Storekeeper }
                }, null, null, 1, "643", null);



			/*
			List<ViewInvoicDetail> docGoodsDetails = new List<ViewInvoicDetail>();
			docGoodsDetails = EdiDbContext.ViewInvoicDetails
				.Where( x => x.IdDocMaster.Value == Doc.Id ).ToList();

			var sellerContractor = AbtDbContext.RefContractors.Where( x => x.Id == Doc.SellerId ).ToList();
			var customerContractor = AbtDbContext.RefContractors.Where( x => x.Id == Doc.CustomerId ).ToList();

			var seller = GetOrganization( sellerContractor.FirstOrDefault() );
			var customer = GetOrganization( customerContractor.FirstOrDefault() );
			
			_utdObject = new Файл() {
				ВерсПрог = "Diadoc 1.0", // const
				Ид = Guid.NewGuid().ToString(),
				ВерсФорм = 5.02M,

				Документ = new Документ() {
					КНД = "1115125", // const
					Функция = "СЧФДОП", // const
					ДатаИнфПр = DateTime.Now.ToShortDateString(),
					ВремИнфПр = DateTime.Now.ToShortTimeString(),

					СвСчФакт = new СвСчФакт() {
						НомерСчФ = Doc.Code,
						ДатаСчФ = DateTime.Now.ToShortDateString(),
						КодОКВ = "643",// const
						СвПрод = new СвОрг[] { seller },

						СвПокуп = new СвОрг[] { customer },
						ДопСвФХЖ1 = new ДопСвФХЖ1() {
							КурсВал = "1", // const
							НаимОКВ = "Российский рубль" // const
						},

					},
					ТаблСчФакт = new ТаблСчФакт() {
						СведТов = GetLineItems( docGoodsDetails ),
						ВсегоОпл = new ВсегоОпл() {
							СтТовБезНДСВсего = Doc.TotalSumm.ToString(),
							СтТовУчНалВсего = (Doc.TotalSumm + Doc.TaxSumm).ToString(),
							СумНалВсего = new СумНалич() {
								СумНал = Doc.TaxSumm.ToString()
							}
						}
					},
					Пер = new Пер() {
						СвПер = new ПерСвПер() {
							СодОпер = "Перадача товаров/услуг", // const
							ДатаПер = Doc.DeliveryDate.Value.ToShortDateString(),
							ОснПер = new ОснПер[]
							{
								new ОснПер
								{
									НаимОсн = Doc.Dogovor,
									ДатаОсн=Doc.DocDatetime.Value.ToShortDateString(),
									НомОсн=Doc.ZCode,
								}
							},
							СвЛицПер = new СвЛицПер() {
								РабОргПрод = new РабОргПрод() {
									Должность = Doc.StorekeeperProf,
									ФИО = new ФИО() {
										//Фамилия = "Иванова",
										Имя = Doc.Storekeeper,
										//Отчество = "Ивановна"
									}
								}
							}
						}
					},

					Подписант = new Подписант[]
					{
						new Подписант
						{
							ОснПолн = $@"{Doc.Dogovor} {Doc.ZCode} {Doc.DocDatetime.Value.ToShortDateString()}",
							ОблПолн = "6", // const
							Статус = "2", // const
							ЮЛ = new ЮЛ() {
								ИННЮЛ = Doc.SellerInn,
								Должн = Doc.StorekeeperProf,
								НаимОрг = Doc.SellerFullName,
								ФИО = new ФИО() {
									Имя = Doc.Storekeeper,
								}
							}
						}

					}

				},

				СвУчДокОбор = new СвУчДокОбор() {
					ИдОтпр = "Тестовый документ",
					ИдПол = "Тестовый документ",

					СвОЭДОтпр = new СвОЭДОтпр() {
						ИдЭДО = "2BM",
						ИННЮЛ = Doc.SellerInn,
						НаимОрг = Doc.SellerFullName
					}

				},

			};

*/

			return utd;
		}

        /*
        public UniversalTransferDocument GetUniversalTransferDocument(ExtendedOrganizationDetails_ManualFilling seller,
            ExtendedOrganizationDetails_ManualFilling buyer,
            string documentDate,
            string documentNumber,
            UniversalTransferDocumentFunction documentFunction,
            TransferInfo transferInfo,
            InvoiceTable invoiceTable,
            ExtendedSignerDetails_SellerTitle[] signers,
            decimal currencyRate,
            string currency,
            string documentCreator)
        {
            UniversalTransferDocument utd = new UniversalTransferDocument();

            utd.Sellers = new ExtendedOrganizationInfo[]
                {
                    new ExtendedOrganizationInfo()
                    {
                        Item = seller
                    }
                };
            utd.Buyers = new ExtendedOrganizationInfo[]
                {
                    new ExtendedOrganizationInfo()
                    {
                        Item = buyer
                    }
                };

            utd.DocumentDate = documentDate;
            utd.DocumentNumber = documentNumber;
            utd.DocumentCreator = documentCreator;
            utd.Function = documentFunction;
            utd.TransferInfo = transferInfo;
            utd.CurrencyRate = currencyRate;
            utd.Currency = currency;
            utd.Table = invoiceTable;

            utd.UseSignerDetails(signers);

            return utd;
        }
        */

        public UniversalTransferDocumentWithHyphens GetUniversalTransferDocumentWithHyphens(
            ExtendedOrganizationDetails_ManualFilling seller,
            ExtendedOrganizationDetails_ManualFilling buyer,
            string documentDate,
            string documentNumber,
            UniversalTransferDocumentWithHyphensFunction documentFunction,
            TransferInfo transferInfo,
            InvoiceTable invoiceTable,
            ExtendedSignerDetails_SellerTitle[] signers,
            decimal currencyRate,
            string currency,
            string documentCreator)
        {
            var universalTransferDocumentWithHyphens = new UniversalTransferDocumentWithHyphens
            {
                Function = documentFunction,
                DocumentDate = documentDate,
                DocumentNumber = documentNumber,
                Currency = currency,
                DocumentCreator = documentCreator,
                Sellers = new[]
                {
                    new ExtendedOrganizationInfoWithHyphens
                    {
                        Item = seller
                    }
                },
                Buyers = new[]
                {
                    new ExtendedOrganizationInfoWithHyphens
                    {
                        Item = buyer
                    }
                },
                TransferInfo = transferInfo,
                Table=invoiceTable
            };

            universalTransferDocumentWithHyphens.UseSignerDetails(signers);

            return universalTransferDocumentWithHyphens;
        }

        public GeneratedFile GenerateTitleXml(string typeNameId,
            string function,
            string version,
            int titleIndex,
            object userDocument,
            string letterId = null, string documentId = null)
        {
            byte[] userDataContract = null;

            if (userDocument as UniversalTransferDocumentWithHyphens != null)
                userDataContract = ((UniversalTransferDocumentWithHyphens)userDocument).SerializeToXml();
            else if (userDocument as Diadoc.Api.DataXml.Utd820.UniversalTransferDocument != null)
                userDataContract = ((Diadoc.Api.DataXml.Utd820.UniversalTransferDocument)userDocument).SerializeToXml();
            else if (userDocument as Diadoc.Api.DataXml.Utd820.UniversalTransferDocumentBuyerTitle != null)
                userDataContract = ((Diadoc.Api.DataXml.Utd820.UniversalTransferDocumentBuyerTitle)userDocument).SerializeToXml();
            else if (userDocument as Diadoc.Api.DataXml.Ucd736.UniversalCorrectionDocument != null)
                userDataContract = ((Diadoc.Api.DataXml.Ucd736.UniversalCorrectionDocument)userDocument).SerializeToXml();
            else throw new Exception("Неопределённый тип документа");

            return _api.GenerateTitleXml(_authToken,
                _actualBoxId,
                typeNameId,
                function,
                version,
                titleIndex,
                userDataContract, false, null, 
                letterId, documentId);
        }

        public Message SendXmlDocument(string senderOrgId, 
            string recipientOrgId,
            bool isOurRecipient,
            byte[] xmlContent,
            string function,
            byte[] signature = null, 
            string comment=null, 
            string customDocumentId = null)
        {
            OrganizationList myOrganizations = CallApiSafe(new Func<OrganizationList>(() => _api.GetMyOrganizations(_authToken, false)));

            if ((myOrganizations?.Organizations?.Count ?? 0) == 0)
                throw new Exception("Не найдены свои организации по токену.");

            var senderOrganization = myOrganizations.Organizations.FirstOrDefault(o => o.OrgId == senderOrgId);

            if(senderOrganization == null)
                throw new Exception($"Не найдена организация с ID отправителя {senderOrgId}");

            Organization recipientOrganization;

            if (isOurRecipient)
            {
                recipientOrganization = myOrganizations.Organizations.FirstOrDefault(o => o.OrgId == recipientOrgId);

                if (recipientOrganization == null)
                    throw new Exception($"Не найдена своя организация-отправитель с ID {recipientOrgId}");
            }
            else
            {
                var counteragents = GetKontragents(senderOrgId);

                counteragents = counteragents.Where(c => c.Organization?.OrgId == recipientOrgId)?
                    .ToList() ?? new List<Counteragent>();

                if (counteragents.Count == 0)
                    throw new Exception("Не найдены контрагенты");

                var counteragent = counteragents.First();

                if (counteragent?.Organization == null)
                    throw new Exception("Не найдена организация");

                recipientOrganization = counteragent.Organization;
            }

            return SendDocumentAttachment(senderOrganization.Boxes.First().BoxId, recipientOrganization.Boxes.First().BoxId, "UniversalTransferDocument", function, "utd820_05_01_01_hyphen",
                xmlContent, signature, comment, customDocumentId);
        }

        public Message SendDocumentAttachment(string ourBoxId, string counteragentBoxId, string typeNameId, string function, string version,
            byte[] xmlContent, byte[] signature = null, string comment = null, string customDocumentId = null, string initialMessageId = null, string initialEntityId = null)
        {
            var documentAttachment = new DocumentAttachment
            {
                TypeNamedId = typeNameId,
                Function = function,
                Version = version,
                SignedContent = new SignedContent
                {
                    Content = xmlContent
                }
            };

            documentAttachment.Comment = comment;
            documentAttachment.CustomDocumentId = customDocumentId;

            if (signature != null)
                documentAttachment.SignedContent.Signature = signature;

            if(initialMessageId != null || initialEntityId != null)
                documentAttachment.InitialDocumentIds.Add(new DocumentId
                {
                    MessageId = initialMessageId,
                    EntityId = initialEntityId
                });

            var messageToPost = new MessageToPost
            {
                FromBoxId = ourBoxId ?? _actualBoxId,
                ToBoxId = counteragentBoxId
            };

            messageToPost.DocumentAttachments.Add(documentAttachment);
            return CallApiSafe(new Func<Message>(()=> { return _api.PostMessage(_authToken, messageToPost); }));
        }

        public List<Counteragent> GetKontragents(string orgId = null)
        {
            CounteragentList list;

            if (string.IsNullOrEmpty(orgId))
            {
                OrganizationList MyOrganizations = CallApiSafe(new Func<OrganizationList>(() => _api.GetMyOrganizations(_authToken, false)));
                Organization myOrganization = MyOrganizations.Organizations.First();

                list = CallApiSafe(new Func<CounteragentList>(() => { return _api.GetCounteragents(_authToken, myOrganization.OrgId, "IsMyCounteragent", null); }));
            }
            else
            {
                list = CallApiSafe(new Func<CounteragentList>(() => { return _api.GetCounteragents(_authToken, orgId, "IsMyCounteragent", null); }));
            }
            return list.Counteragents;
        }

        public List<Models.Kontragent> GetOrganizations(string orgId = null)
        {
            var counterAgents = new List<Counteragent>();

            if (string.IsNullOrEmpty(orgId))
                counterAgents = GetKontragents();
            else
                counterAgents = GetKontragents(orgId);

            List<Models.Kontragent> organizations = new List<Models.Kontragent>();

            foreach (var counteragent in counterAgents)
            {
                Models.Kontragent organization = new Models.Kontragent(counteragent.Organization?.FullName ?? "", 
                    counteragent?.Organization?.Inn ?? "", 
                    counteragent.Organization.Kpp);

                organization.OrgId = counteragent?.Organization?.OrgId ?? "";
                organization.Address = counteragent?.Organization?.Address;
                organization.FnsParticipantId = counteragent?.Organization?.FnsParticipantId;
                organizations.Add(organization);
            }

            return organizations;
        }

        public Models.Kontragent GetKontragentByInnKpp(string inn, string kpp = null, bool isNotRoaming = true)
        {
            var organizations = CallApiSafe(new Func<OrganizationList>(() => _api.GetOrganizationsByInnKpp(inn, kpp)));

            Organization organization = null;

            if(isNotRoaming)
                organization = organizations?.Organizations?.FirstOrDefault(o => !o.IsRoaming);
            else
                organization = organizations?.Organizations?.FirstOrDefault();

            return new Models.Kontragent(organization?.FullName, organization?.Inn, organization?.Kpp)
            {
                OrgId = organization?.OrgId,
                Address = organization?.Address
            };
        }

        public Diadoc.Api.Proto.Invoicing.Signers.ExtendedSignerDetails GetExtendedSignerDetails(Diadoc.Api.Proto.Invoicing.Signers.DocumentTitleType documentTitleType)
        {
            try
            {
                return _api.GetExtendedSignerDetails(_authToken, _actualBoxId, _certificate.RawData, documentTitleType);
            }
            catch
            {
                return null;
            }
        }

        public Diadoc.Api.Proto.Documents.Document GetDocument(string messageId, string entityId)
        {
            var document = CallApiSafe(new Func<Diadoc.Api.Proto.Documents.Document>(() => _api.GetDocument(_authToken, _actualBoxId, messageId, entityId)));
            return document;
        }

        public Message GetMessage(string messageId, string entityId, bool includedContent = false)
        {
            var message = CallApiSafe(new Func<Message>(() => _api.GetMessage(_authToken, _actualBoxId, messageId, entityId, false, includedContent)));
            return message;
        }

        public MessagePatch SendPatchRecipientXmlDocument(string messageId, int docType, string entityId, byte[] content, byte[] signature = null)
        {
            var recipientAttachment = new RecipientTitleAttachment
            {
                ParentEntityId = entityId,
                SignedContent = new SignedContent
                {
                    Content = content
                }
            };

            if (signature != null)
                recipientAttachment.SignedContent.Signature = signature;

            var messageToPost = new MessagePatchToPost
            {
                BoxId = _actualBoxId,
                MessageId = messageId
            };

            if (docType == (int)DocumentType.UniversalTransferDocument)
                messageToPost.AddUniversalTransferDocumentBuyerTitle(recipientAttachment);
            else if (docType == (int)DocumentType.XmlTorg12)
                messageToPost.AddXmlTorg12BuyerTitle(recipientAttachment);
            else if (docType == (int)DocumentType.XmlAcceptanceCertificate)
                messageToPost.AddXmlAcceptanceCertificateBuyerTitle(recipientAttachment);

            return CallApiSafe(new Func<MessagePatch>(() => { return _api.PostMessagePatch(_authToken, messageToPost); }));
        }

        public GeneratedFile GenerateRevocationRequestXml(string messageId, string entityId, string text, Diadoc.Api.Proto.Invoicing.Signer signer)
        {
            var revocationRequestInfo = new Diadoc.Api.Proto.Invoicing.RevocationRequestInfo
            {
                Comment = text,
                Signer = signer
            };

            return _api.GenerateRevocationRequestXml(_authToken, _actualBoxId, messageId, entityId, revocationRequestInfo, "revocation_request_02");
        }

        public void SendRevocationDocument(string messageId, string entityId, byte[] fileBytes, byte[] signature)
        {
            var signatureAttachment = new RevocationRequestAttachment()
            {
                ParentEntityId = entityId,
                SignedContent = new SignedContent
                {
                    Content = fileBytes,
                    Signature = signature
                }
            };

            var postMessage = new MessagePatchToPost
            {
                BoxId = _actualBoxId,
                MessageId = messageId
            };

            postMessage.AddRevocationRequestAttachment(signatureAttachment);
            var messagePatch = CallApiSafe(new Func<MessagePatch>(() => _api.PostMessagePatch(_authToken, postMessage)));
        }

        public MessagePatch SendPatchSignedDocument(string messageId, string parentEntityId, byte[] signature)
        {
            var messageToPost = new MessagePatchToPost
            {
                BoxId = _actualBoxId,
                MessageId = messageId
            };

            messageToPost.AddSignature(new DocumentSignature
            {
                ParentEntityId = parentEntityId,
                Signature = signature
            });

            return CallApiSafe(new Func<MessagePatch>(() => { return _api.PostMessagePatch(_authToken, messageToPost); }));
        }

        public GeneratedFile GenerateSignatureRejectionXml(string messageId, string entityId, string text, Diadoc.Api.Proto.Invoicing.Signer signer)
        {
            var xmlRejectionInfo = new Diadoc.Api.Proto.Invoicing.SignatureRejectionInfo
            {
                ErrorMessage = text,
                Signer = signer
            };

            return _api.GenerateSignatureRejectionXml(_authToken, _actualBoxId, messageId, entityId, xmlRejectionInfo);
        }

        public void SendRejectionDocument(string messageId, string entityId, byte[] fileBytes, byte[] signature)
        {
            var signatureRejectionAttachment = new XmlSignatureRejectionAttachment()
            {
                ParentEntityId = entityId,
                SignedContent = new SignedContent
                {
                    Content = fileBytes,
                    Signature = signature
                }
            };

            var postMessage = new MessagePatchToPost
            {
                BoxId = _actualBoxId,
                MessageId = messageId
            };

            postMessage.AddXmlSignatureRejectionAttachment(signatureRejectionAttachment);

            var messagePatch = CallApiSafe(new Func<MessagePatch>(() => _api.PostMessagePatch(_authToken, postMessage)));
        }

        public void SetOrganizationParameters(Models.Kontragent kAgent)
        {
            OrganizationList myOrganizations = CallApiSafe(new Func<OrganizationList>(() => _api.GetMyOrganizations(_authToken, false)));

            Organization organization;

            if (!string.IsNullOrEmpty(kAgent.Kpp))
            {
                organization = myOrganizations.Organizations.FirstOrDefault(o => o.Inn == kAgent.Inn && o.Kpp == kAgent.Kpp);

                if(organization == null)
                    organization = myOrganizations.Organizations.FirstOrDefault(o => o.Inn == kAgent.Inn);
            }
            else
                organization = myOrganizations.Organizations.FirstOrDefault(o => o.Inn == kAgent.Inn);

            kAgent.OrgId = organization.OrgId;
            kAgent.Address = organization.Address;
        }

        private void SetBoxId(bool forLogin)
		{
            if (forLogin)
            {
                if (string.IsNullOrEmpty(_actualBoxId))
                {
                    string BoxId = null;

                    if (_config != null)
                    {
                        if (_config.EdoBoxId != null)
                        {
                            _actualBoxId = _config.EdoBoxId;
                            return;
                        }
                    }
                    if (BoxId == null)
                    {
                        OrganizationList MyOrganizations = CallApiSafe(new Func<OrganizationList>(() => _api.GetMyOrganizations(_authToken, false)));
                        _config.Save(_config, UtilitesLibrary.ConfigSet.Config.ConfFileName);
                    }
                    _actualBoxId = BoxId;
                }
            }
            else if(!string.IsNullOrEmpty(_orgCertInn))
            {
                OrganizationList myOrganizations = CallApiSafe(new Func<OrganizationList>(() => _api.GetMyOrganizations(_authToken, false)));

                var organization = myOrganizations?.Organizations?
                    .FirstOrDefault(k => k.Inn == _orgCertInn && k.IsRoaming == false);

                _actualBoxId = organization?
                    .Boxes?
                    .FirstOrDefault()?
                    .BoxId;
            }
		}

		/// <summary>
		/// Получить токен аутентификации
		/// </summary>
		public bool Authenticate(bool byLogin = false, X509Certificate2 cert = null, string orgCertInn = null)
		{
            if (!byLogin)
            {
                _orgCertInn = orgCertInn;

                if (cert != null)
                    _certificate = cert;
                else
                    _certificate = new X509Certificate2(File.ReadAllBytes(_config.CertFullPath));

                _cache = new EdoTokenCache().Load(_certificate.Thumbprint);
            }
            else
                _cache = new EdoTokenCache().Load( TokenFileName );

			if (_cache != null && !IsTokenExpired)
			{
                SetBoxId(byLogin);

				return true;
			}
			if (_cache == null || IsTokenExpired)
			{
				string authToken;

                if(byLogin)
                    authToken = (string)CallApiSafe(new Func<object>(() => _api.Authenticate(_config.EdoUserName, _config.EdoUserPassword)));
                else
                    authToken = (string)CallApiSafe(new Func<object>(() => _api.Authenticate(_certificate.RawData)));

                if (string.IsNullOrEmpty( authToken ))
					return false;

                if (byLogin)
                {
                    _cache = new EdoTokenCache(authToken, $"User {_config.EdoUserName}", _cache?.PartyId ?? "");
                    _cache.Save(_cache, TokenFileName);
                }
                else
                {
                    _cache = new EdoTokenCache(authToken, $"Certificate, Serial Number {_certificate.SerialNumber}", _cache?.PartyId ?? "");
                    _cache.Save(_cache, _certificate.Thumbprint);
                }

                SetBoxId(byLogin);

				return true;
			}
			return false;			
		}

		/// <summary>
		/// Истёк ли токен аутентификации
		/// </summary>
		public bool IsTokenExpired => _cache?.TokenExpirationDate < DateTime.Now;
		private static volatile Edo _instance;
		private static readonly object syncRoot = new object();

		private Edo()
		{
			var hClient = new HttpClient( _config.EdoApiUrl );
			// если edo хочет ходить через прокси - пусть будет так
			if (_config.ProxyEnabled)
			{
				hClient.SetProxyUri( "http://"+_config.ProxyAddress );
				hClient.UseSystemProxy = false;
				hClient.SetProxyCredentials( new NetworkCredential(
					_config.ProxyUserName,
					_config.ProxyUserPassword
				) );
			}
			_api = new DiadocHttpApi( _config.EdoApiClientId, hClient, new WinApiCrypt() );	
		}

		public static Edo GetInstance()
		{
			if (_instance == null)
			{
				lock (syncRoot)
				{
					if (_instance == null)
					{
						_instance = new Edo();
					}
				}
			}
			return _instance;
		}

		private TOut CallApiSafe<TOut>(Func<TOut> CallingDelegate) where TOut : new()
		{
			_log.Log( "SafeCall: " + CallingDelegate.Method.Name );

			TOut ret;
			int tries = 15;
			Exception ex = null;

			while (tries-- >= 0)
			{
				try
				{
					ret = CallingDelegate.Invoke();
					return ret;
				}
                catch (WebException webEx)
                {
                    _log.Error(webEx);
                    ex = webEx;
                }
				catch (Exception e)
				{
					_log.Error( e );
					ex = e;
				}
			}

			if (ex != null)
				throw ex;

			throw new Exception( "метод не получилось вызвать более 15 раз" );
		}

	}
}
