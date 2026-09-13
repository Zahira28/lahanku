using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Models;
using Lahanku.Services;

namespace Lahanku.ViewModels
{
    public partial class DeleteConfirmationViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private readonly ILandService _landService;
        private readonly DashboardViewModel _dashboard;

        public Land TargetLand { get; }
        public string LandName => TargetLand.Name;

        [ObservableProperty]
        private bool _isDeleting;

        public DeleteConfirmationViewModel(MainViewModel main, ILandService landService, DashboardViewModel dashboard, Land targetLand)
        {
            _main = main;
            _landService = landService;
            _dashboard = dashboard;
            TargetLand = targetLand;
        }

        [RelayCommand]
        private async Task ConfirmDeleteAsync()
        {
            IsDeleting = true;
            try
            {
                await _landService.DeleteLandAsync(TargetLand.Id);
                await _dashboard.LoadLandsAsync();

                _main.CloseModal();
                _main.ShowToast("Data berhasil dihapus!");
            }
            finally
            {
                IsDeleting = false;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _main.CloseModal();
        }
    }
}
