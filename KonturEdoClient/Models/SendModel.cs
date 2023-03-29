using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EdiProcessingUnit.Edo.Models;
using System.Security.Cryptography.X509Certificates;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using KonturEdoClient.Utils;

namespace KonturEdoClient.Models
{
    public class SendModel
    {
        private Kontragent _myOrganization;
        private X509Certificate2 _signerCertificate;
        private Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphens _currentDocument;
        private AbtDbContext _abt;
        private bool _isSendReceive = false;
        private HonestMark.DocumentProcessStatusesEnum _docStatus = HonestMark.DocumentProcessStatusesEnum.None;
        private UtilitesLibrary.Logger.UtilityLog _log = UtilitesLibrary.Logger.UtilityLog.GetInstance();
        private decimal? _currentIdDoc;
        private DataContextManagementUnit.DataAccess.DocJournalType _docType;
        private bool _authInHonestMark;
        private DocEdoProcessing _edoProcessing;

        public EventHandler BeforeSendEventHandler { get; set; }

        public System.Windows.Window Owner { get; set; }

        public bool IsReceivedMarkData { get; set; }

        public List<Kontragent> Organizations { get; set; }
        public Kontragent SelectedOrganization { get; set; }

        public DocComissionEdoProcessing ComissionDocument { get; set; }
        public DocEdoProcessing EdoProcessing {
            get {
                return _edoProcessing;
            }
        }

        public SendModel(AbtDbContext abt, Kontragent myOrganization, X509Certificate2 signerCertificate, 
            Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphens currentDocument, decimal? currentIdDoc,
            DataContextManagementUnit.DataAccess.DocJournalType docType, bool authInHonestMark)
        {
            _abt = abt;
            _myOrganization = myOrganization;
            _signerCertificate = signerCertificate;
            _currentDocument = currentDocument;
            _currentIdDoc = currentIdDoc;
            _docType = docType;
            _authInHonestMark = authInHonestMark;
            _edoProcessing = null;
        }

        public async void Send(bool isSign)
        {
            if(SelectedOrganization == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбран получатель.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            bool? sendDocument = false;
            bool isMarked = _currentDocument.Table.Item.Any(i => (i?.ItemIdentificationNumbers?.FirstOrDefault()?.Items?.Count() ?? 0) > 0);

            if (isMarked && !_authInHonestMark)
            {
                if (System.Windows.MessageBox.Show(
                    "Авторизация в Честном знаке не была успешной\nНевозможно проверить контрагента на регистрацию в Честном знаке.\nВсё равно хотите отправить документ?", "Ошибка", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                    return;
            }

            try
            {
                BeforeSendEventHandler?.Invoke(this, new EventArgs());
                sendDocument = true;
            }
            catch (System.Net.WebException webEx)
            {
                var errorWindow = new ErrorSendHonestMarkWindow($"Произошла ошибка на удалённом сервере: {webEx.Message}\n{webEx.StackTrace}");
                sendDocument = errorWindow.ShowDialog();
                _log.Log("Отправка в честный знак. WebException: " + _log.GetRecursiveInnerException(webEx));
            }
            catch (Exception ex)
            {
                var errorWindow = new ErrorSendHonestMarkWindow($"Произошла ошибка: {ex.Message}\n{ex.StackTrace}");
                sendDocument = errorWindow.ShowDialog();
                _log.Log("Отправка в честный знак. Exception: " + _log.GetRecursiveInnerException(ex));
            }

            if (sendDocument == true)
            {
                var loadContext = new LoadModel();
                var loadWindow = new LoadWindow();
                loadWindow.DataContext = loadContext;

                loadWindow.Owner = Owner;
                loadWindow.Show();
                Exception exception = null;

                await Task.Run(() =>
                {
                    try
                    {
                        if (isMarked && _authInHonestMark && !HonestMark.HonestMarkClient.GetInstance().IsOrgRegistered(SelectedOrganization.Inn))
                            throw new Exception("Данный участник не зарегистрирован в Честном знаке");

                        if (_docType == DataContextManagementUnit.DataAccess.DocJournalType.Translocation)
                        {
                            Diadoc.Api.DataXml.RussianAddress receiverAddress = SelectedOrganization?.Address?.RussianAddress != null ?
                            new Diadoc.Api.DataXml.RussianAddress
                            {
                                ZipCode = string.IsNullOrEmpty(SelectedOrganization.Address.RussianAddress.ZipCode) ? null : SelectedOrganization.Address.RussianAddress.ZipCode,
                                Region = SelectedOrganization.Address.RussianAddress.Region,
                                Street = string.IsNullOrEmpty(SelectedOrganization?.Address?.RussianAddress?.Street) ? null : SelectedOrganization.Address.RussianAddress.Street,
                                City = string.IsNullOrEmpty(SelectedOrganization?.Address?.RussianAddress?.City) ? null : SelectedOrganization.Address.RussianAddress.City,
                                Locality = string.IsNullOrEmpty(SelectedOrganization?.Address?.RussianAddress?.Locality) ? null : SelectedOrganization.Address.RussianAddress.Locality,
                                Territory = string.IsNullOrEmpty(SelectedOrganization?.Address?.RussianAddress?.Territory) ? null : SelectedOrganization.Address.RussianAddress.Territory,
                                Building = string.IsNullOrEmpty(SelectedOrganization?.Address?.RussianAddress?.Building) ? null : SelectedOrganization.Address.RussianAddress.Building
                            } : null;

                            _currentDocument.Buyers = new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationInfoWithHyphens[]
                            {
                                    new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationInfoWithHyphens
                                    {
                                        Item = new Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationDetailsWithHyphens
                                        {
                                            Inn = SelectedOrganization.Inn,
                                            Kpp = string.IsNullOrEmpty(SelectedOrganization.Kpp) ? null : SelectedOrganization.Kpp,
                                            OrgType = SelectedOrganization.Inn.Length == 12 ? Diadoc.Api.DataXml.OrganizationType.IndividualEntity : Diadoc.Api.DataXml.OrganizationType.LegalEntity,
                                            OrgName = SelectedOrganization.Name,
                                            Address = new Diadoc.Api.DataXml.Address
                                            {
                                                Item = receiverAddress
                                            }
                                        }
                                    }
                            };
                        }

                        var message = new XmlSignUtils().SignAndSend(isSign,
                            _signerCertificate, _myOrganization, SelectedOrganization, _currentDocument);

                        if (message != null)
                        {

                            loadContext.Text = "Сохранение в базе данных.";
                            var entity = message.Entities.FirstOrDefault(t => t.AttachmentType == Diadoc.Api.Proto.Events.AttachmentType.UniversalTransferDocument);

                            var fileNameLength = entity.FileName.LastIndexOf('.');

                            if (fileNameLength < 0)
                                fileNameLength = entity.FileName.Length;

                            var fileName = entity.FileName.Substring(0, fileNameLength);

                            var docProcessing = new DocEdoProcessing
                            {
                                Id = Guid.NewGuid().ToString(),
                                MessageId = message.MessageId,
                                EntityId = entity.EntityId,
                                FileName = fileName,
                                IsReprocessingStatus = 0,
                                IdDoc = _currentIdDoc,
                                DocDate = DateTime.Now,
                                UserName = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser,
                                ReceiverName = SelectedOrganization.Name,
                                ReceiverInn = SelectedOrganization.Inn,
                                DocType = (int)EdiProcessingUnit.Enums.DocEdoType.Upd
                            };

                            if(isMarked && ComissionDocument == null)
                            {
                                var comDoc = _abt.DocComissionEdoProcessings.FirstOrDefault(d => d.IdDoc == _currentIdDoc && 
                                (d.DocStatus == (int)HonestMark.DocEdoProcessingStatus.Processed || d.DocStatus == (int)HonestMark.DocEdoProcessingStatus.Sent));

                                if (comDoc == null)
                                    throw new Exception("Не найден комиссионный документ.");

                                ComissionDocument = comDoc;
                            }

                            if (ComissionDocument != null)
                            {
                                docProcessing.IdComissionDocument = ComissionDocument.Id;
                                docProcessing.ComissionDocument = ComissionDocument;
                                ComissionDocument.MainDocuments.Add(docProcessing);
                            }
                            else
                            {
                                _abt.DocEdoProcessings.Add(docProcessing);
                            }

                            _abt.SaveChanges();


                            if (isSign)
                            {
                                _edoProcessing = docProcessing;
                                loadWindow.SetSuccessFullLoad(loadContext, "Подписание и отправка завершены успешно.");
                            }
                            else
                                loadWindow.SetSuccessFullLoad(loadContext, "Отправка завершена успешно.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _edoProcessing = null;
                        exception = ex;
                    }
                });

                if (exception != null)
                {
                    loadWindow.Close();

                    var errorWindow = new ErrorWindow(
                                "Произошла ошибка отправки.",
                                new List<string>(
                                    new string[]
                                    {
                                    exception.Message,
                                    exception.StackTrace
                                    }
                                    ));

                    errorWindow.ShowDialog();
                    _log.Log($"Send Exception: {_log.GetRecursiveInnerException(exception)}");
                }
            }
        }
    }
}
