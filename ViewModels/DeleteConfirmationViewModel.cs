using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Models;
using Lahanku.Services;
using System.Windows.Input;

namespace Lahanku.ViewModels
{
    public partial class DeleteConfirmationViewModel : ViewModelBase, IKeyboardShortcutHandler
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
                bool success = await _landService.DeleteLandAsync(TargetLand.Id);
                if (!success)
                {
                    _main.ShowErrorToast("Lahan gagal dihapus. Periksa koneksi internet lalu coba lagi.", "Gagal Menghapus");
                    return;
                }

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

        public bool HandleKey(Key key, ModifierKeys modifiers) =>
            key == Key.Enter && modifiers == ModifierKeys.None && ConfirmDeleteCommand.TryRun();
    }
}
