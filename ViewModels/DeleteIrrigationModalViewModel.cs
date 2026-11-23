using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Models;
using Lahanku.Services;

namespace Lahanku.ViewModels
{
    public partial class DeleteIrrigationModalViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private readonly ILandService _landService;
        private readonly LandDetailViewModel _detailViewModel;

        public IrrigationLog TargetLog { get; }
        public string FormattedDate => TargetLog.FormattedDate;
        public string FormattedVolume => TargetLog.FormattedVolume;
        public string Description => $"Apakah Anda yakin ingin menghapus catatan penyiraman tanggal {FormattedDate} ({FormattedVolume})? Tindakan ini tidak dapat dibatalkan.";

        [ObservableProperty]
        private bool _isDeleting;

        public DeleteIrrigationModalViewModel(MainViewModel main, ILandService landService, LandDetailViewModel detailViewModel, IrrigationLog targetLog)
        {
            _main = main;
            _landService = landService;
            _detailViewModel = detailViewModel;
            TargetLog = targetLog;
        }

        [RelayCommand]
        private async Task ConfirmDeleteAsync()
        {
            IsDeleting = true;
            try
            {
                bool success = await _landService.DeleteIrrigationLogAsync(TargetLog.Id);
                if (success)
                {
                    await _detailViewModel.LoadLogsAsync();
                    _main.CloseModal();
                    _main.ShowToast("Catatan penyiraman berhasil dihapus!");
                }
                else
                {
                    _main.ShowErrorToast("Gagal menghapus catatan penyiraman.", "Kesalahan Database");
                }
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
