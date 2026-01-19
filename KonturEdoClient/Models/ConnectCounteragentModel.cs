using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using KonturEdoClient.Models.Implementations;

namespace KonturEdoClient.Models
{
    public class ConnectCounteragentModel : Base.ModelBase
    {
        private AbtDbContext _abt;
        private UtilitesLibrary.Logger.UtilityLog _log = UtilitesLibrary.Logger.UtilityLog.GetInstance();
        private decimal? _idFilial = null;
        private EdiProcessingUnit.UsersConfig _config;

        public ConnectCounteragentModel(EdiProcessingUnit.UsersConfig config, AbtDbContext abt)
        {
            _abt = abt;
            _config = config;
            RefCustomers = new ObservableCollection<RefCustomer>(abt.RefCustomers);

            var idFilialStr = _abt?.Database?.SqlQuery<string>("select const_value from ref_const where id = 0")?.FirstOrDefault();

            if (!string.IsNullOrEmpty(idFilialStr))
            {
                decimal idFilial;

                if (decimal.TryParse(idFilialStr, out idFilial))
                    _idFilial = idFilial;
            }

            Filials = null;
            IsDefault = true;
        }

        public bool IsMainFilial => _idFilial == 1;
        public bool IsDefault { get; set; }

        public RelayCommand CheckConnectCommand => new RelayCommand((o) => { CheckConnect(); });
        public RelayCommand ConnectCommand => new RelayCommand((o) => { Connect(); });
        public RelayCommand ChangeCounteragentCommand => new RelayCommand((o) => { ChangeCounteragent(); });
        public RelayCommand TransferToFilialsCommand => new RelayCommand((o) => { TransferToFilials(); });
        public RelayCommand ChangeConsigneeCommand => new RelayCommand((o) => { ChangeConsignee(); });

        public ObservableCollection<RefCustomer> RefCustomers { get; set; }
        public RefCustomer SelectedCustomer { get; set; }

        public List<EdiProcessingUnit.User> Filials { get; set; }

        public decimal IdSellerCustomer { get; set; }

        public string FnsId { get; set; }

        private async Task<Diadoc.Api.Proto.CounteragentStatus?> GetConnectStatus()
        {
            if(SelectedCustomer == null)
            {
                System.Windows.MessageBox.Show("Ошибка! Не выбрано юр. лицо.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return null;
            }

            RefEdoCounteragent refEdoCounteragent = null;

            if(string.IsNullOrEmpty(FnsId))
                refEdoCounteragent = _abt.RefEdoCounteragents.FirstOrDefault(c => c.IdCustomerBuyer == SelectedCustomer.Id && c.IdCustomerSeller == this.IdSellerCustomer && c.IsDefault == 1);
            else
                refEdoCounteragent = _abt.RefEdoCounteragents.FirstOrDefault(c => c.IdCustomerBuyer == SelectedCustomer.Id && c.IdCustomerSeller == this.IdSellerCustomer
                && c.IdFnsBuyer.ToUpper() == FnsId.Trim().ToUpper());

            if(refEdoCounteragent?.IdFilial == 1 && _idFilial != 1)
            {
                System.Windows.MessageBox.Show("Ошибка! Данные контрагента были заданы головным филиалом.\nОбратитесь к головному филиалу для уточнения данных по контрагенту.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return null;
            }

            if (string.IsNullOrEmpty(FnsId) && refEdoCounteragent != null)
            {
                FnsId = refEdoCounteragent.IdFnsBuyer;
                OnPropertyChanged("FnsId");
            }

            if (string.IsNullOrEmpty(FnsId))
            {
                System.Windows.MessageBox.Show("Ошибка! Не указан идентификатор ЭДО контрагента.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return null;
            }

            var counteragents = await EdiProcessingUnit.Edo.Edo.GetInstance().GetKontragentsAsync(EdiProcessingUnit.Edo.Edo.GetInstance().ActualBoxIdGuid, null, null);

            Diadoc.Api.Proto.CounteragentStatus? currentStatus = null;
            if (!counteragents.Exists(c => c?.Organization?.FnsParticipantId?.ToUpper() == FnsId.Trim().ToUpper()))
            {
                currentStatus = Diadoc.Api.Proto.CounteragentStatus.NotInCounteragentList;
            }
            else
            {
                var counteragent = counteragents.FirstOrDefault(c => c?.Organization?.FnsParticipantId?.ToUpper() == FnsId.Trim().ToUpper());

                if(SelectedCustomer.Inn != counteragent?.Organization?.Inn)
                {
                    System.Windows.MessageBox.Show("Ошибка! ИНН контрагента не соответствует ИНН юр. лица.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return null;
                }
                else
                    currentStatus = counteragent?.CurrentStatus;
            }

            if (refEdoCounteragent != null)
            {
                _abt.Entry(refEdoCounteragent)?.Reload();
                if (refEdoCounteragent.IdFnsBuyer.ToUpper() == FnsId.Trim().ToUpper() && currentStatus != null)
                {
                    refEdoCounteragent.ConnectStatus = (int)currentStatus.Value;
                    refEdoCounteragent.IsConnected = currentStatus == Diadoc.Api.Proto.CounteragentStatus.IsMyCounteragent ? 1 : 0;
                    _abt.SaveChanges();
                }
            }

            return currentStatus;
        }

        public async void Connect()
        {
            var loadWindow = new LoadWindow();
            var checkConnect = await GetConnectStatus();

            if (checkConnect == null)
                return;

            try
            {
                var loadModel = new LoadModel();
                loadWindow.DataContext = loadModel;

                loadWindow.Show();

                if (checkConnect == Diadoc.Api.Proto.CounteragentStatus.NotInCounteragentList)
                {
                    var orgObj = await EdiProcessingUnit.Edo.Edo.GetInstance().GetOrganizationByFnsIdAsync(FnsId.Trim());

                    var counteragentBoxId = orgObj.Key;
                    var organization = orgObj.Value;

                    var res = await EdiProcessingUnit.Edo.Edo.GetInstance().AcquireCounteragentAsync(counteragentBoxId, organization.OrgId, organization.Inn, "Приглашаем вас к обмену документами.");
                }
                else
                {
                    var message = GetConnectStringResult(checkConnect.Value);

                    System.Windows.MessageBox.Show(message, "Подключение", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }

                if (IsDefault)
                {
                    var otherEdoIdsForOrganization = _abt.RefEdoCounteragents.Where(r => r.IdCustomerBuyer == SelectedCustomer.Id && r.IdCustomerSeller == this.IdSellerCustomer &&
                    r.IdFnsBuyer.ToUpper() != FnsId.Trim().ToUpper())?.ToList() ?? new List<RefEdoCounteragent>();

                    foreach (var otherEdoIdForOrganization in otherEdoIdsForOrganization)
                        otherEdoIdForOrganization.IsDefault = 0;
                }

                var refEdoCounteragent = _abt.RefEdoCounteragents.FirstOrDefault(c => c.IdCustomerBuyer == SelectedCustomer.Id && c.IdCustomerSeller == this.IdSellerCustomer &&
                c.IdFnsBuyer.ToUpper() == FnsId.Trim().ToUpper());

                if(refEdoCounteragent == null)
                {
                    refEdoCounteragent = new RefEdoCounteragent
                    {
                        IdCustomerSeller = this.IdSellerCustomer,
                        IdCustomerBuyer = SelectedCustomer.Id,
                        IdFnsBuyer = FnsId.Trim(),
                        ConnectStatus = (int)checkConnect.Value,
                        InsertDatetime = DateTime.Now,
                        InsertUser = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser,
                        IdFilial = _idFilial,
                        IsConnected = checkConnect == Diadoc.Api.Proto.CounteragentStatus.IsMyCounteragent ? 1 : 0,
                        IsDefault = this.IsDefault ? 1 : 0
                    };

                    _abt.RefEdoCounteragents.Add(refEdoCounteragent);
                }
                else
                {
                    refEdoCounteragent.IsDefault = this.IsDefault ? 1 : 0;
                }

                _abt.SaveChanges();

                loadWindow.SetSuccessFullLoad(loadModel, "Данные контрагента занесены.");
            }
            catch (Exception ex)
            {
                loadWindow.Close();
                _log.Log($"ConnectCounteragentModel.Connect Exception: {_log.GetRecursiveInnerException(ex)}");

                var errorWindow = new ErrorWindow(
                            "Произошла ошибка подключения контрагентов.",
                            new List<string>(
                                new string[]
                                {
                                    ex.Message,
                                    ex.StackTrace
                                }
                                ));

                errorWindow.ShowDialog();
            }
        }

        public async void CheckConnect()
        {
            var loadWindow = new LoadWindow();

            var loadModel = new LoadModel();
            loadWindow.DataContext = loadModel;

            loadWindow.Show();

            try
            {
                Diadoc.Api.Proto.CounteragentStatus? checkConnect = null;
                try
                {
                    checkConnect = await GetConnectStatus();
                }
                finally
                {
                    loadWindow.Close();
                }

                if (checkConnect == null)
                    return;

                var message = GetConnectStringResult(checkConnect.Value);
                System.Windows.MessageBox.Show(message, "Проверка статуса", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                loadWindow.Close();
                _log.Log($"ConnectCounteragentModel.CheckConnect Exception: {_log.GetRecursiveInnerException(ex)}");

                var errorWindow = new ErrorWindow(
                            "Произошла ошибка проверки статуса подключения контрагентов.",
                            new List<string>(
                                new string[]
                                {
                                    ex.Message,
                                    ex.StackTrace
                                }
                                ));

                errorWindow.ShowDialog();
            }
        }

        public void ChangeCounteragent()
        {
            if (SelectedCustomer == null)
            {
                System.Windows.MessageBox.Show("Ошибка! Не выбрано юр. лицо.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                var changeCounteragentsWindow = new CounteragentsWindow();
                var listModel = new Base.ListViewModel<EdiProcessingUnit.Edo.Models.Kontragent>();
                changeCounteragentsWindow.DataContext = listModel;

                var items = EdiProcessingUnit.Edo.Edo.GetInstance().GetKontragentsByInnKpp(SelectedCustomer.Inn, SelectedCustomer.Kpp) ?? new List<EdiProcessingUnit.Edo.Models.Kontragent>();
                listModel.ItemsList = new ObservableCollection<EdiProcessingUnit.Edo.Models.Kontragent>(items);

                if (changeCounteragentsWindow.ShowDialog() == true)
                {
                    FnsId = listModel.SelectedItem?.FnsParticipantId;
                    OnPropertyChanged("FnsId");
                }
            }
            catch(Exception ex)
            {
                _log.Log($"ConnectCounteragentModel.ChangeCounteragent Exception: {_log.GetRecursiveInnerException(ex)}");

                var errorWindow = new ErrorWindow(
                            "Произошла ошибка.",
                            new List<string>(
                                new string[]
                                {
                                    ex.Message,
                                    ex.StackTrace
                                }
                                ));

                errorWindow.ShowDialog();
            }
        }

        public async void TransferToFilials()
        {
            if (SelectedCustomer == null)
            {
                System.Windows.MessageBox.Show("Ошибка! Не выбрано юр. лицо.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var loadWindow = new LoadWindow();
            try
            {
                var loadModel = new LoadModel();
                loadWindow.DataContext = loadModel;

                RefEdoCounteragent refEdoCounteragent = null;

                if(string.IsNullOrEmpty(FnsId))
                    refEdoCounteragent = _abt.RefEdoCounteragents.FirstOrDefault(c => c.IdCustomerBuyer == SelectedCustomer.Id && c.IdCustomerSeller == this.IdSellerCustomer && c.IsDefault == 1);
                else
                    refEdoCounteragent = _abt.RefEdoCounteragents.FirstOrDefault(c => c.IdCustomerBuyer == SelectedCustomer.Id && c.IdCustomerSeller == this.IdSellerCustomer
                    && c.IdFnsBuyer.ToUpper() == FnsId.Trim().ToUpper());

                if (refEdoCounteragent == null)
                {
                    System.Windows.MessageBox.Show("Ошибка! Данный контрагент не был ранее занесён для подключения.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(FnsId))
                {
                    FnsId = refEdoCounteragent.IdFnsBuyer;
                    OnPropertyChanged("FnsId");
                }

                if (FnsId.Trim().ToUpper() != refEdoCounteragent.IdFnsBuyer.ToUpper())
                {
                    System.Windows.MessageBox.Show("Ошибка! Идентификатор ЭДО не соответствует подключаемому.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                if (_idFilial != 1)
                {
                    System.Windows.MessageBox.Show("Ошибка! Данный филиал не является головным.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                loadWindow.Show();
                Exception exception = null;
                await Task.Run(() =>
                {
                    try
                    {
                        foreach (var filial in Filials ?? new List<EdiProcessingUnit.User>())
                        {
                            if (filial.SID == UtilitesLibrary.ConfigSet.Config.GetInstance().AbtDataBaseSid)
                                continue;

                            using(var abtDbContext = new AbtDbContext(_config.GetConnectionStringByUser(filial), true))
                            {
                                RefCustomer filialBuyerCustomer = null;
                                var selectedCustomerId = SelectedCustomer.Id;
                                var selectedCustomerInn = SelectedCustomer.Inn;

                                filialBuyerCustomer = abtDbContext.RefCustomers?.FirstOrDefault(r => r.Id == selectedCustomerId && r.Inn == selectedCustomerInn);

                                if(filialBuyerCustomer == null)
                                    filialBuyerCustomer = abtDbContext.RefCustomers?.FirstOrDefault(r => r.Inn == selectedCustomerInn);

                                if (filialBuyerCustomer != null)
                                {
                                    var filialEdoCounteragents = abtDbContext.RefEdoCounteragents.Where(c => c.IdCustomerBuyer == filialBuyerCustomer.Id && c.IdCustomerSeller == this.IdSellerCustomer).ToList();

                                    if (refEdoCounteragent.IsDefault == 1)
                                    {
                                        foreach (var f in filialEdoCounteragents)
                                            f.IsDefault = 0;
                                    }

                                    var filialEdoCounteragent = filialEdoCounteragents.FirstOrDefault(c => c.IdFnsBuyer.ToUpper() == FnsId.Trim().ToUpper());

                                    if(filialEdoCounteragent != null)
                                    {
                                        filialEdoCounteragent.ConnectStatus = refEdoCounteragent.ConnectStatus;
                                        filialEdoCounteragent.IsConnected = refEdoCounteragent.IsConnected;
                                        filialEdoCounteragent.IdFnsBuyer = refEdoCounteragent.IdFnsBuyer;
                                        filialEdoCounteragent.IdFilial = refEdoCounteragent.IdFilial;
                                        filialEdoCounteragent.IsDefault = refEdoCounteragent.IsDefault == 1 ? 1 : 0;
                                    }
                                    else
                                    {
                                        abtDbContext.RefEdoCounteragents.Add(new RefEdoCounteragent
                                        {
                                            IdCustomerSeller = refEdoCounteragent.IdCustomerSeller,
                                            IdCustomerBuyer = filialBuyerCustomer.Id,
                                            IdFnsBuyer = refEdoCounteragent.IdFnsBuyer,
                                            ConnectStatus = refEdoCounteragent.ConnectStatus,
                                            InsertDatetime = refEdoCounteragent.InsertDatetime,
                                            InsertUser = refEdoCounteragent.InsertUser,
                                            IdFilial = refEdoCounteragent.IdFilial,
                                            IsConnected = refEdoCounteragent.IsConnected,
                                            IsDefault = refEdoCounteragent.IsDefault == 1 ? 1 : 0
                                        });
                                    }

                                    abtDbContext.SaveChanges();
                                }
                            }
                        }
                    }
                    catch (Exception excep)
                    {
                        exception = excep;
                    }
                });

                if (exception != null)
                {
                    loadWindow.Close();
                    throw exception;
                }
                else
                    loadWindow.SetSuccessFullLoad(loadModel);
            }
            catch(Exception ex)
            {
                _log.Log($"ConnectCounteragentModel.TransferToFilials Exception: {_log.GetRecursiveInnerException(ex)}");

                var errorWindow = new ErrorWindow(
                            "Произошла ошибка переноса на филиалы.",
                            new List<string>(
                                new string[]
                                {
                                    ex.Message,
                                    ex.StackTrace
                                }
                                ));

                errorWindow.ShowDialog();
            }
        }

        public void ChangeConsignee()
        {
            if (SelectedCustomer == null)
            {
                System.Windows.MessageBox.Show("Ошибка! Не выбрано юр. лицо.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                RefEdoCounteragent refEdoCounteragent = null;

                if (string.IsNullOrEmpty(FnsId))
                    refEdoCounteragent = _abt.RefEdoCounteragents.FirstOrDefault(c => c.IdCustomerBuyer == SelectedCustomer.Id && c.IdCustomerSeller == this.IdSellerCustomer && c.IsDefault == 1);
                else
                    refEdoCounteragent = _abt.RefEdoCounteragents.FirstOrDefault(c => c.IdCustomerBuyer == SelectedCustomer.Id && c.IdCustomerSeller == this.IdSellerCustomer
                    && c.IdFnsBuyer.ToUpper() == FnsId.Trim().ToUpper());

                if (refEdoCounteragent == null)
                {
                    System.Windows.MessageBox.Show("Ошибка! Данный контрагент не был ранее занесён для подключения.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(FnsId))
                {
                    FnsId = refEdoCounteragent.IdFnsBuyer;
                    OnPropertyChanged("FnsId");
                }

                var counteragentConsigneesModel = new CounteragentConsigneesModel(refEdoCounteragent, _abt);
                counteragentConsigneesModel.Refresh();

                var counteragentConsigneesWindow = new CounteragentConsigneesWindow();
                counteragentConsigneesWindow.DataContext = counteragentConsigneesModel;
                counteragentConsigneesWindow.ShowDialog();
            }
            catch(Exception ex)
            {
                _log.Log($"ConnectCounteragentModel.ChangeConsignee Exception: {_log.GetRecursiveInnerException(ex)}");

                var errorWindow = new ErrorWindow(
                            "Произошла ошибка выбора точек доставки.",
                            new List<string>(
                                new string[]
                                {
                                    ex.Message,
                                    ex.StackTrace
                                }
                                ));

                errorWindow.ShowDialog();
            }
        }

        private string GetConnectStringResult(Diadoc.Api.Proto.CounteragentStatus status)
        {
            if (status == Diadoc.Api.Proto.CounteragentStatus.NotInCounteragentList)
                return "Организации нет в списке контрагентов.";
            else if (status == Diadoc.Api.Proto.CounteragentStatus.IsMyCounteragent)
                return "Организация подключена к обмену.";
            else if (status == Diadoc.Api.Proto.CounteragentStatus.InvitesMe)
                return "Контрагент прислал приглашение на обмен, нужно принять.";
            else if (status == Diadoc.Api.Proto.CounteragentStatus.IsInvitedByMe)
                return "Контрагенту отправлено приглашение на обмен, пусть принимает.";
            else if (status == Diadoc.Api.Proto.CounteragentStatus.RejectsMe)
                return "Контрагент отклонил приглашение на обмен.";
            else if (status == Diadoc.Api.Proto.CounteragentStatus.IsRejectedByMe)
                return "Контрагенту было отказано в обмене, приглашение отклонено.";
            else
                return "Неизвестное значение статуса";
        }
    }
}
