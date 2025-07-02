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
    public class SendModel : Base.ModelBase
    {
        private Kontragent _myOrganization;
        private X509Certificate2 _signerCertificate;
        private IEnumerable<KeyValuePair<decimal, Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument>> _currentDocuments;
        private AbtDbContext _abt;
        private bool _isSendReceive = false;
        private EdiProcessingUnit.HonestMark.DocumentProcessStatusesEnum _docStatus = EdiProcessingUnit.HonestMark.DocumentProcessStatusesEnum.None;
        private UtilitesLibrary.Logger.UtilityLog _log = UtilitesLibrary.Logger.UtilityLog.GetInstance();
        private DataContextManagementUnit.DataAccess.DocJournalType _docType;
        private bool _authInHonestMark;
        private List<DocEdoProcessing> _edoProcessings;

        public EventHandler BeforeSendEventHandler { get; set; }

        public System.Windows.Window Owner { get; set; }

        public bool IsReceivedMarkData { get; set; }
        public bool IsButtonsEnabled { get; set; }

        public List<Kontragent> Organizations { get; set; }
        public Kontragent SelectedOrganization { get; set; }

        public List<DocComissionEdoProcessing> ComissionDocuments { get; set; }
        public List<DocEdoProcessing> EdoProcessing {
            get {
                return _edoProcessings;
            }
        }

        public SendModel(AbtDbContext abt, Kontragent myOrganization, X509Certificate2 signerCertificate,
            IEnumerable<KeyValuePair<decimal, Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument>> currentDocuments,
            DataContextManagementUnit.DataAccess.DocJournalType docType, bool authInHonestMark)
        {
            _abt = abt;
            _myOrganization = myOrganization;
            _signerCertificate = signerCertificate;
            _currentDocuments = currentDocuments;
            _docType = docType;
            _authInHonestMark = authInHonestMark;
            _edoProcessings = new List<DocEdoProcessing>();
            ComissionDocuments = new List<DocComissionEdoProcessing>();
        }

        public void SetButtonsEnabled(bool isEnabled)
        {
            IsButtonsEnabled = isEnabled;
            OnPropertyChanged("IsButtonsEnabled");
        }

        public async void Send(bool isSign)
        {
            try
            {
                if(SelectedOrganization == null)
                {
                    System.Windows.MessageBox.Show(
                        "Не выбран получатель.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                bool? sendDocument = false;
                bool isMarked = _currentDocuments.Any(u => u.Value.Table.Item.Any(i => (i?.ItemIdentificationNumbers?.FirstOrDefault()?.Items?.Count() ?? 0) > 0));

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

                    if (isMarked && _authInHonestMark && !EdiProcessingUnit.HonestMark.HonestMarkClient.GetInstance().IsOrgRegistered(SelectedOrganization.Inn))
                    {
                        _log.Log("Проверка на регистрацию дала отрицательный результат.");

                        //if (System.Windows.MessageBox.Show("Возможно, контрагент не зарегистрирован в Честном знаке.\nВсё равно хотите отправить документ?", "Ошибка", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                        //    return;
                    }

                    await Task.Run(() =>
                    {
                        try
                        {
                            var currentDocuments = _currentDocuments.Where(c => c.Value != null).Select(c => c.Value).ToList();
                            if (_docType == DataContextManagementUnit.DataAccess.DocJournalType.Translocation)
                            {
                                Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.RussianAddressUtd970 receiverAddress = SelectedOrganization?.Address?.RussianAddress != null ?
                                    new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.RussianAddressUtd970
                                    {
                                        ZipCode = string.IsNullOrEmpty(SelectedOrganization.Address.RussianAddress.ZipCode) ? null : SelectedOrganization.Address.RussianAddress.ZipCode,
                                        Region = SelectedOrganization.Address.RussianAddress.Region,
                                        Street = string.IsNullOrEmpty(SelectedOrganization?.Address?.RussianAddress?.Street) ? null : SelectedOrganization.Address.RussianAddress.Street,
                                        City = string.IsNullOrEmpty(SelectedOrganization?.Address?.RussianAddress?.City) ? null : SelectedOrganization.Address.RussianAddress.City,
                                        Locality = string.IsNullOrEmpty(SelectedOrganization?.Address?.RussianAddress?.Locality) ? null : SelectedOrganization.Address.RussianAddress.Locality,
                                        Territory = string.IsNullOrEmpty(SelectedOrganization?.Address?.RussianAddress?.Territory) ? null : SelectedOrganization.Address.RussianAddress.Territory,
                                        Building = string.IsNullOrEmpty(SelectedOrganization?.Address?.RussianAddress?.Building) ? null : SelectedOrganization.Address.RussianAddress.Building
                                    } : null;

                                foreach (var curDoc in currentDocuments)
                                {
                                    curDoc.Buyers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970[]
                                    {
                                        new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970
                                        {
                                            Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationDetailsUtd970
                                            {
                                                Inn = SelectedOrganization.Inn,
                                                Kpp = string.IsNullOrEmpty(SelectedOrganization.Kpp) ? null : SelectedOrganization.Kpp,
                                                OrgType = SelectedOrganization.Inn.Length == 12 ? Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item1 : Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item2,
                                                OrgName = SelectedOrganization.Name,
                                                Address = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AddressUtd970
                                                {
                                                    Item = receiverAddress
                                                }
                                            }
                                        }
                                    };
                                }
                            }

                            var message = new XmlSignUtils().SignAndSend(isSign,
                                _signerCertificate, _myOrganization, SelectedOrganization,
                                currentDocuments.ToList<object>());

                            DocEdoProcessing docProcessing;
                            if (message != null)
                            {
                                _log.Log($"Сохранение в базе данных {_currentDocuments.Count()} документов, Id сообщения {message?.MessageId}");
                                loadContext.Text = "Сохранение в базе данных.";

                                foreach (var currentDocument in _currentDocuments)
                                {
                                    var documentNumber = currentDocument.Value.DocumentNumber;
                                    var entity = message.Entities.FirstOrDefault(t => t.AttachmentType == Diadoc.Api.Proto.Events.AttachmentType.UniversalTransferDocument &&
                                    t?.DocumentInfo?.DocumentNumber == documentNumber);

                                    var fileNameLength = entity.FileName.LastIndexOf('.');

                                    if (fileNameLength < 0)
                                        fileNameLength = entity.FileName.Length;

                                    var fileName = entity.FileName.Substring(0, fileNameLength);

                                    bool isDocMarked = false;

                                    if (isMarked)
                                        isDocMarked = currentDocument.Value.Table.Item.Any(i => (i?.ItemIdentificationNumbers?.FirstOrDefault()?.Items?.Count() ?? 0) > 0);

                                    docProcessing = new DocEdoProcessing
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        MessageId = message.MessageId,
                                        EntityId = entity.EntityId,
                                        FileName = fileName,
                                        IsReprocessingStatus = 0,
                                        IdDoc = currentDocument.Key,
                                        DocDate = DateTime.Now,
                                        UserName = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser,
                                        ReceiverName = SelectedOrganization.Name,
                                        ReceiverInn = SelectedOrganization.Inn,
                                        DocType = (int)EdiProcessingUnit.Enums.DocEdoType.Upd,
                                        HonestMarkStatus = isDocMarked ? (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Sent : (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.None
                                    };

                                    var comissionDocument = ComissionDocuments.FirstOrDefault(c => c.IdDoc == currentDocument.Key);

                                    if (comissionDocument == null && currentDocument.Value.Table.Item.Any(i => (i?.ItemIdentificationNumbers?.FirstOrDefault()?.Items?.Count() ?? 0) > 0))
                                    {
                                        var comDoc = _abt.DocComissionEdoProcessings.FirstOrDefault(d => d.IdDoc == currentDocument.Key &&
                                        (d.DocStatus == (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Processed || d.DocStatus == (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Sent));

                                        if (comDoc == null)
                                            throw new Exception("Не найден комиссионный документ.");

                                        comissionDocument = comDoc;
                                    }

                                    if (comissionDocument != null)
                                    {
                                        docProcessing.IdComissionDocument = comissionDocument.Id;
                                        docProcessing.ComissionDocument = comissionDocument;
                                        comissionDocument.MainDocuments.Add(docProcessing);

                                        if(!_abt.DocEdoProcessings.Any(d => d.Id == docProcessing.Id))
                                            _abt.DocEdoProcessings.Add(docProcessing);
                                    }
                                    else
                                    {
                                        _abt.DocEdoProcessings.Add(docProcessing);
                                    }

                                    _log.Log($"Документ {docProcessing.EntityId ?? string.Empty} добавлен в базу.");

                                    if(isSign)
                                        _edoProcessings.Add(docProcessing);
                                }

                                _abt.SaveChanges();
                                _log.Log($"SaveChanges: успешно.");


                                if (isSign)
                                {
                                    loadWindow.SetSuccessFullLoad(loadContext, "Подписание и отправка завершены успешно.");
                                }
                                else
                                    loadWindow.SetSuccessFullLoad(loadContext, "Отправка завершена успешно.");
                            }
                            else
                            {
                                _log.Log($"Не удалось идентифицировать отправленное сообщение.");
                            }
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
            finally
            {
                SetButtonsEnabled(true);
            }
        }
    }
}
