using System;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Models;
using Lahanku.Services;

namespace Lahanku.ViewModels
{
    public partial class IrrigationModalViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private readonly ILandService _landService;
        private readonly LandDetailViewModel _detailViewModel;

        public Land TargetLand { get; }
        public string ModalTitle => $"Siram {TargetLand.Name}";

        [ObservableProperty]
        private DateTime _date = DateTime.Now;

        [ObservableProperty]
        private string _volumeLitersText = "250";

        [ObservableProperty]
        private string _notes = "Penyiraman pagi hari, kondisi tanah agak kering di lereng barat.";

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _isSaving;

        public IrrigationModalViewModel(MainViewModel main, ILandService landService, LandDetailViewModel detailViewModel, Land targetLand)
        {
            _main = main;
            _landService = landService;
            _detailViewModel = detailViewModel;
            TargetLand = targetLand;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            ErrorMessage = null;

            if (!double.TryParse(VolumeLitersText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double volume) || volume <= 0)
            {
                ErrorMessage = "Volume air harus berupa angka positif.";
                return;
            }

            IsSaving = true;
            try
            {
                var newLog = new IrrigationLog
                {
                    LandId = TargetLand.Id,
                    Date = Date,
                    VolumeLiters = volume,
                    Notes = Notes?.Trim() ?? string.Empty
                };

                await _landService.AddIrrigationLogAsync(TargetLand.Id, newLog);
                await _detailViewModel.LoadLogsAsync();

                _main.CloseModal();
                _main.ShowToast("Penyiraman berhasil dicatat!");
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _main.CloseModal();
        }
    }
}
