using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Abt;

namespace KonturEdoClient.Models
{
    public class ChannelSettingsModel : Base.ModelBase
    {
        protected UtilitesLibrary.Logger.UtilityLog _log = UtilitesLibrary.Logger.UtilityLog.GetInstance();
        private AbtDbContext _abt;
        private decimal? _idFilial = null;
        private EdiProcessingUnit.UsersConfig _config;
        private bool _isCreate;

        public bool IsCreate => _isCreate;

        public List<RefChannel> Channels { get; set; }
        public RefChannel SelectedChannel { get; set; }

        public List<WebService.Models.RefEdiChannel> EdiChannels { get; set; }
        public WebService.Models.RefEdiChannel SelectedEdiChannel { get; set; }

        public bool IsPermittedForOtherFilials
        {
            get {
                if (Item == null)
                    return false;

                return Item.PermittedForOtherFilials != 0;
            }
            set {
                if(value)
                    Item.PermittedForOtherFilials = 1;
                else
                    Item.PermittedForOtherFilials = 0;

                OnPropertyChanged("IsPermittedForOtherFilials");
            }
        }

        public bool TransferSettingsToFilials { get; set; }

        public bool AccountWithChoicesOfFilials => (Filials?.Count ?? 0) > 1;

        public List<EdiProcessingUnit.User> Filials { get; set; } = null;

        public RefEdoGoodChannel Item { get; set; }

        public string MainWindowText => _isCreate ? "Добавление сети" : "Изменение сети";

        public ChannelSettingsModel(AbtDbContext abt, EdiProcessingUnit.UsersConfig config, decimal? idFilial, RefEdoGoodChannel item = null)
        {
            _abt = abt;
            _config = config;
            _idFilial = idFilial;
            Channels = _abt?.Database?.SqlQuery<RefChannel>("select Id, Name from REF_CHANNELS")?
                .ToList() ?? new List<RefChannel>();

            if(item == null)
            {
                Item = new RefEdoGoodChannel
                {
                    Id = Guid.NewGuid().ToString(),
                    CreateDateTime = DateTime.Now,
                    UserName = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser,
                    PermittedForOtherFilials = 0
                };

                _isCreate = true;
            }
            else
                Item = item;

            if (WebService.Controllers.FinDbController.GetInstance().LoadedConfig)
                EdiChannels = WebService.Controllers.FinDbController.GetInstance().GetEdiChannels().ToList();
        }

        public void AddKeyValue(EdiProcessingUnit.Enums.DocEdoType docEdoType)
        {
            var refEdoUpdValuesWindow = new RefEdoUpdValuesWindow(Item);

            if (docEdoType == EdiProcessingUnit.Enums.DocEdoType.Upd)
                refEdoUpdValuesWindow.Item = new RefEdoUpdValues();
            else if(docEdoType == EdiProcessingUnit.Enums.DocEdoType.Ucd)
                refEdoUpdValuesWindow.Item = new RefEdoUcdValues();

            refEdoUpdValuesWindow.ShowDialog();
            OnPropertyChanged("Item.EdoValuesPairs");
            OnPropertyChanged("Item.EdoUcdValuesPairs");
        }

        public void EditKeyValue(object r)
        {
            var refEdoUpdValuesWindow = new RefEdoUpdValuesWindow(Item, r);
            refEdoUpdValuesWindow.ShowDialog();
            OnPropertyChanged("Item.EdoValuesPairs");
            OnPropertyChanged("Item.EdoUcdValuesPairs");
        }

        public void RemoveKeyValue(object r)
        {
            if (System.Windows.MessageBox.Show("Вы действительно хотите удалить данную пару ключ-значение?" +
                        $"\nДанную операцию нельзя будет отменить.", "Внимание",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                return;

            if (r as RefEdoUpdValues != null)
            {
                var refEdoUpdValue = r as RefEdoUpdValues;
                Item.EdoValuesPairs.Remove(refEdoUpdValue);
                OnPropertyChanged("Item.EdoValuesPairs");
            }
            else if(r as RefEdoUcdValues != null)
            {
                var refEdoUcdValue = r as RefEdoUcdValues;
                Item.EdoUcdValuesPairs.Remove(refEdoUcdValue);
                OnPropertyChanged("Item.EdoUcdValuesPairs");
            }
        }

        public bool Save()
        {
            if(SelectedChannel == null)
            {
                System.Windows.MessageBox.Show(
                        "Не выбрана сеть для соответствия.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }

            if (_abt?.RefEdoGoodChannels?.FirstOrDefault(r => r.IdChannel == SelectedChannel.Id && r.Id != Item.Id) != null)
            {
                System.Windows.MessageBox.Show(
                        "Данная сеть уже была ранее добавлена в базу.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }

            if (_idFilial == null)
            {
                System.Windows.MessageBox.Show(
                        "Не определён текущий филиал.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }

            try
            {
                Exception exception = null;
                var loadContext = new LoadModel();
                var loadWindow = new LoadWindow();
                loadWindow.DataContext = loadContext;

                loadWindow.Show();
                var task = Task.Run(() =>
                {
                    try
                    {
                        if (TransferSettingsToFilials && AccountWithChoicesOfFilials)
                        {
                            loadContext.Text = "Перенос на филиалы.";
                            foreach (var filial in Filials)
                            {
                                if (filial.SID == UtilitesLibrary.ConfigSet.Config.GetInstance().AbtDataBaseSid)
                                    continue;

                                using (var abtDbContext = new AbtDbContext(_config.GetConnectionStringByUser(filial), true))
                                {
                                    bool isCreate = false;
                                    var channel = abtDbContext?.RefEdoGoodChannels?.FirstOrDefault(r => r.IdChannel == SelectedChannel.Id);

                                    if (channel == null)
                                    {
                                        channel = new RefEdoGoodChannel
                                        {
                                            Id = Item.Id,
                                            CreateDateTime = DateTime.Now,
                                            IdChannel = SelectedChannel.Id,
                                            EdiGln = SelectedEdiChannel?.Gln
                                        };
                                        isCreate = true;
                                    }

                                    channel.UserName = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser;
                                    channel.PermittedForOtherFilials = Item.PermittedForOtherFilials;
                                    channel.Name = SelectedChannel.Name;
                                    channel.NumberUpdId = Item.NumberUpdId;
                                    channel.OrderNumberUpdId = Item.OrderNumberUpdId;
                                    channel.OrderDateUpdId = Item.OrderDateUpdId;
                                    channel.DetailBuyerCodeUpdId = Item.DetailBuyerCodeUpdId;
                                    channel.DetailBarCodeUpdId = Item.DetailBarCodeUpdId;
                                    channel.IdFilial = _idFilial.Value;
                                    channel.DocReturnNumberUcdId = Item.DocReturnNumberUcdId;
                                    channel.DocReturnDateUcdId = Item.DocReturnDateUcdId;
                                    channel.DetailPositionUpdId = Item.DetailPositionUpdId;
                                    channel.GlnShipToUpdId = Item.GlnShipToUpdId;

                                    foreach (var key in Item.EdoValuesPairs)
                                    {
                                        if (!channel.EdoValuesPairs.Exists(r => r.Key == key.Key))
                                        {
                                            var newKey = new RefEdoUpdValues
                                            {
                                                Key = key.Key,
                                                Value = key.Value,
                                                IdEdoGoodChannel = channel.Id,
                                                EdoGoodChannel = channel
                                            };

                                            channel.EdoValuesPairs.Add(newKey);
                                        }
                                    }

                                    foreach(var key in Item.EdoUcdValuesPairs)
                                    {
                                        if(!channel.EdoUcdValuesPairs.Exists(r => r.Key == key.Key))
                                        {
                                            var newKey = new RefEdoUcdValues
                                            {
                                                Key = key.Key,
                                                Value = key.Value,
                                                IdEdoGoodChannel = channel.Id,
                                                EdoGoodChannel = channel
                                            };

                                            channel.EdoUcdValuesPairs.Add(newKey);
                                        }
                                    }

                                    if (isCreate)
                                        abtDbContext.RefEdoGoodChannels.Add(channel);

                                    abtDbContext.SaveChanges();
                                }
                            }
                        }

                        loadContext.Text = "Сохранение в базе.";

                        if (_isCreate)
                        {
                            Item.IdFilial = _idFilial.Value;
                            Item.IdChannel = SelectedChannel.Id;
                            Item.EdiGln = SelectedEdiChannel?.Gln;
                            _abt.RefEdoGoodChannels.Add(Item);
                        }

                        Item.Name = SelectedChannel.Name;
                        _abt.SaveChanges();
                    }
                    catch(Exception ex)
                    {
                        exception = ex;
                    }
                });

                task.Wait();

                if (exception != null)
                {
                    loadWindow.Close();
                    throw exception;
                }
                else
                    loadWindow.SetSuccessFullLoad(loadContext);

                return true;
            }
            catch(Exception ex)
            {
                var errorWindow = new ErrorWindow(
                       "Произошла ошибка сохранения.",
                       new List<string>(
                           new string[]
                           {
                                    ex.Message,
                                    ex.StackTrace
                           }
                           ));

                errorWindow.ShowDialog();
                _log.Log("ChannelSettingsModel.Save Exception: " + _log.GetRecursiveInnerException(ex));

                return false;
            }
        }
    }
}
