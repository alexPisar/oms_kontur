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
        }

        public bool IsMainFilial => _idFilial == 1;

        public RelayCommand CheckConnectCommand => new RelayCommand((o) => { CheckConnect(); });
        public RelayCommand ConnectCommand => new RelayCommand((o) => { Connect(); });
        public RelayCommand ChangeCounteragentCommand => new RelayCommand((o) => { ChangeCounteragent(); });
        public RelayCommand TransferToFilialsCommand => new RelayCommand((o) => { TransferToFilials(); });

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

            var refEdoCounteragent = _abt.RefEdoCounteragents.FirstOrDefault(c => c.IdCustomerBuyer == SelectedCustomer.Id && c.IdCustomerSeller == this.IdSellerCustomer);

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

                var refEdoCounteragent = _abt.RefEdoCounteragents.FirstOrDefault(c => c.IdCustomerBuyer == SelectedCustomer.Id && c.IdCustomerSeller == this.IdSellerCustomer);

                if(refEdoCounteragent != null)
                {
                    if (refEdoCounteragent.IdFnsBuyer.ToUpper() != FnsId.Trim().ToUpper())
                    {
                        refEdoCounteragent.IdFnsBuyer = FnsId.Trim();
                        refEdoCounteragent.InsertDatetime = DateTime.Now;
                        refEdoCounteragent.InsertUser = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser;
                    }
                }
                else
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
                        IsConnected = checkConnect == Diadoc.Api.Proto.CounteragentStatus.IsMyCounteragent ? 1 : 0
                    };

                    _abt.RefEdoCounteragents.Add(refEdoCounteragent);
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

                var refEdoCounteragent = _abt.RefEdoCounteragents.FirstOrDefault(c => c.IdCustomerBuyer == SelectedCustomer.Id && c.IdCustomerSeller == this.IdSellerCustomer);

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
                                var filialEdoCounteragent = abtDbContext.RefEdoCounteragents.FirstOrDefault(c => c.IdCustomerBuyer == SelectedCustomer.Id && c.IdCustomerSeller == this.IdSellerCustomer);

                                if(filialEdoCounteragent != null)
                                {
                                    filialEdoCounteragent.ConnectStatus = refEdoCounteragent.ConnectStatus;
                                    filialEdoCounteragent.IsConnected = refEdoCounteragent.IsConnected;
                                    filialEdoCounteragent.IdFnsBuyer = refEdoCounteragent.IdFnsBuyer;
                                    filialEdoCounteragent.IdFilial = refEdoCounteragent.IdFilial;
                                }
                                else
                                {
                                    abtDbContext.RefEdoCounteragents.Add(new RefEdoCounteragent
                                    {
                                        IdCustomerSeller = refEdoCounteragent.IdCustomerSeller,
                                        IdCustomerBuyer = refEdoCounteragent.IdCustomerBuyer,
                                        IdFnsBuyer = refEdoCounteragent.IdFnsBuyer,
                                        ConnectStatus = refEdoCounteragent.ConnectStatus,
                                        InsertDatetime = refEdoCounteragent.InsertDatetime,
                                        InsertUser = refEdoCounteragent.InsertUser,
                                        IdFilial = refEdoCounteragent.IdFilial,
                                        IsConnected = refEdoCounteragent.IsConnected
                                    });
                                }

                                abtDbContext.SaveChanges();
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
