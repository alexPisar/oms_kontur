using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using System.Collections.ObjectModel;
using KonturEdoClient.Models.Implementations;

namespace KonturEdoClient.Models
{
    public class ChannelsListModel : Base.ListViewModel<RefEdoGoodChannel>
    {
        private AbtDbContext _abt;
        private decimal? _idFilial = null;
        private EdiProcessingUnit.UsersConfig _config;

        public bool AccountWithChoicesOfFilials => (Filials?.Count ?? 0) > 1;
        public List<EdiProcessingUnit.User> Filials { get; set; } = null;
        public bool PermissionChannelsSettings { get; set; }

        public ChannelsListModel(AbtDbContext abt, EdiProcessingUnit.UsersConfig config, decimal? idFilial, List<RefEdoGoodChannel> items)
        {
            _abt = abt;
            _config = config;
            ItemsList = new ObservableCollection<RefEdoGoodChannel>(items);
            _idFilial = idFilial;
        }

        public override void Refresh()
        {
            var items = new List<RefEdoGoodChannel>();
            if (AccountWithChoicesOfFilials)
            {
                var idFilials = _abt.Database.SqlQuery<decimal>("select Id from ref_filials where Links in('" + string.Join("', '", Filials.Select(f => f.SID)) + "')");

                items = _abt?.RefEdoGoodChannels?.Where(r => idFilials.Any(i => i == r.IdFilial))?.ToList() ?? new List<RefEdoGoodChannel>();
            }
            else
                items = _abt?.RefEdoGoodChannels?.Where(r => _idFilial == r.IdFilial || r.PermittedForOtherFilials == 1)?.ToList() ?? new List<RefEdoGoodChannel>();

            ItemsList = new ObservableCollection<RefEdoGoodChannel>(items);
            SelectedItem = null;
            UpdateProps();
        }

        public override void CreateNew()
        {
            var channelSettingsWindow = new ChannelSettingsWindow();

            if (!AccountWithChoicesOfFilials)
            {
                channelSettingsWindow.PermittedCheckBox.Visibility = System.Windows.Visibility.Collapsed;
                channelSettingsWindow.TransferSettingsCheckBox.Visibility = System.Windows.Visibility.Collapsed;
            }

            var channelSettingsModel = new ChannelSettingsModel(_abt, _config, _idFilial);
            channelSettingsModel.Filials = this.Filials;
            channelSettingsWindow.DataContext = channelSettingsModel;

            if (channelSettingsWindow.ShowDialog() == true)
                Refresh();
        }

        public override void Edit()
        {
            if(SelectedItem == null)
            {
                System.Windows.MessageBox.Show(
                        "Не выбрана сеть для изменения.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if ((!PermissionChannelsSettings) && (SelectedItem.UserName != UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser || SelectedItem.IdFilial != (_idFilial ?? 0)))
            {
                System.Windows.MessageBox.Show(
                        "Пользователь не имеет прав на изменение данной сети.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var channelSettingsWindow = new ChannelSettingsWindow();

            if (!AccountWithChoicesOfFilials)
            {
                channelSettingsWindow.PermittedCheckBox.Visibility = System.Windows.Visibility.Collapsed;
                channelSettingsWindow.TransferSettingsCheckBox.Visibility = System.Windows.Visibility.Collapsed;
            }

            var channelSettingsModel = new ChannelSettingsModel(_abt, _config, _idFilial, SelectedItem);
            channelSettingsModel.Filials = this.Filials;
            channelSettingsWindow.DataContext = channelSettingsModel;

            if (channelSettingsWindow.ShowDialog() == true)
                Refresh();
        }

        public override void Delete()
        {
            if (SelectedItem == null)
            {
                System.Windows.MessageBox.Show(
                        "Не выбрана сеть для удаления.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if((!PermissionChannelsSettings) && (SelectedItem.UserName != UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser || SelectedItem.IdFilial != (_idFilial ?? 0)))
            {
                System.Windows.MessageBox.Show(
                        "Пользователь не имеет прав на удаление данной сети.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (System.Windows.MessageBox.Show("Вы действительно хотите удалить сеть из сопоставлений?" +
                        $"\nДанную операцию нельзя будет отменить.", "Внимание",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                return;

            try
            {
                var item = _abt.RefEdoGoodChannels.FirstOrDefault(r => r.Id == SelectedItem.Id);

                if (item != null)
                {
                    if (item.EdoValuesPairs.Count > 0)
                    {
                        var valuePairs = item.EdoValuesPairs.ToArray();

                        for(int i = 0; i < item.EdoValuesPairs.Count; i++)
                            item.EdoValuesPairs.Remove(valuePairs[i]);
                    }

                    _abt.RefEdoGoodChannels.Remove(SelectedItem);
                    _abt.SaveChanges();
                }
                Refresh();
            }
            catch(Exception ex)
            {
                var errorWindow = new ErrorWindow(
                       "Произошла ошибка удаления.",
                       new List<string>(
                           new string[]
                           {
                                    ex.Message,
                                    ex.StackTrace
                           }
                           ));

                errorWindow.ShowDialog();
                _log.Log("ChannelsListModel.Delete Exception: " + _log.GetRecursiveInnerException(ex));
            }
        }

        private void UpdateProps()
        {
            OnPropertyChanged("ItemsList");
            OnPropertyChanged("SelectedItem");
        }
    }
}
