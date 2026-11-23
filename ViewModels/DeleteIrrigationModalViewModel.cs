using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Models;
using Lahanku.Services;
using System.Windows.Input;

namespace Lahanku.ViewModels
{
    public partial class DeleteIrrigationModalViewModel : ViewModelBase, IKeyboardShortcutHandler
    {
        private readonly MainViewModel _main;
        private readonly ILandService _landService;
        private readonly System.Func<Task> _onDeleted;

        public IrrigationLog TargetLog { get; }
        public string FormattedDate => TargetLog.FormattedDate;
        public string FormattedVolume => TargetLog.FormattedVolume;
        public string Description => $"Apakah Anda yakin ingin menghapus catatan penyiraman tanggal {FormattedDate} ({FormattedVolume})? Tindakan ini tidak dapat dibatalkan.";

        [ObservableProperty]
        private bool _isDeleting;

        public DeleteIrrigationModalViewModel(MainViewModel main, ILandService landService, System.Func<Task> onDeleted, IrrigationLog targetLog)
        {
            _main = main;
            _landService = landService;
            _onDeleted = onDeleted;
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
                    await _onDeleted();
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

        public bool HandleKey(Key key, ModifierKeys modifiers) =>
            key == Key.Enter && modifiers == ModifierKeys.None && ConfirmDeleteCommand.TryRun();
    }
}
