using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Models;
using Lahanku.Services;

namespace Lahanku.ViewModels
{
    public partial class LandDetailViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private readonly ILandService _landService;

        [ObservableProperty]
        private Land _land;

        [ObservableProperty]
        private ObservableCollection<IrrigationLog> _irrigationLogs = new();

        [ObservableProperty]
        private string _selectedTab = "Penyiraman";

        [ObservableProperty]
        private bool _hasLogs;

        public LandDetailViewModel(MainViewModel main, ILandService landService, Land land)
        {
            _main = main;
            _landService = landService;
            _land = land;

            _ = LoadLogsAsync();
        }

        [RelayCommand]
        public async Task LoadLogsAsync()
        {
            var logs = await _landService.GetIrrigationLogsAsync(Land.Id);
            IrrigationLogs.Clear();
            foreach (var log in logs)
            {
                IrrigationLogs.Add(log);
            }

            HasLogs = IrrigationLogs.Count > 0;
        }

        [RelayCommand]
        private void BackToDashboard()
        {
            _main.NavigateToDashboard();
        }

        [RelayCommand]
        private void OpenAddIrrigationModal()
        {
            _main.OpenModal(new IrrigationModalViewModel(_main, _landService, this, Land));
        }

        [RelayCommand]
        private void SelectTab(string tabName)
        {
            SelectedTab = tabName;
        }
    }
}
