using System;
using System.Collections.ObjectModel;
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
        private readonly double? _suggestedVolume;

        public Land TargetLand { get; }
        public IrrigationLog? ExistingLog { get; }
        public bool IsEditMode => ExistingLog != null;
        public string ModalTitle => IsEditMode ? "Edit Catatan Penyiraman" : $"Siram {TargetLand.Name}";
        public string SubmitButtonText => IsEditMode ? "Simpan Perubahan" : "Simpan";

        [ObservableProperty]
        private DateTime _date = DateTime.Now;

        [ObservableProperty]
        private string _volumeValueText = string.Empty;

        [ObservableProperty]
        private string _selectedVolumeUnit = "Liter (L)";

        public ObservableCollection<string> VolumeUnitOptions { get; } = new()
        {
            "Liter (L)",
            "Mililiter (mL)"
        };

        public string VolumePlaceholder => SelectedVolumeUnit?.Contains("mL") == true ? "Contoh: 500" : "Contoh: 250";

        public string NotesPlaceholder => "Contoh: Penyiraman rutin pagi hari, kondisi tanah cukup lembap.";

        /// <summary>
        /// Alias properti untuk kompatibilitas
        /// </summary>
        public string VolumeLitersText
        {
            get => VolumeValueText;
            set => VolumeValueText = value;
        }

        partial void OnSelectedVolumeUnitChanged(string? oldValue, string newValue)
        {
            OnPropertyChanged(nameof(VolumePlaceholder));
            if (!string.IsNullOrWhiteSpace(VolumeValueText) &&
                double.TryParse(VolumeValueText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double val) &&
                val > 0 && oldValue != null)
            {
                if (oldValue.Contains("Liter") && newValue.Contains("mL"))
                {
                    // L -> mL (dikali 1000)
                    VolumeValueText = (val * 1000.0).ToString("0.##", CultureInfo.InvariantCulture);
                }
                else if (oldValue.Contains("mL") && newValue.Contains("Liter"))
                {
                    // mL -> L (dibagi 1000)
                    VolumeValueText = (val / 1000.0).ToString("0.###", CultureInfo.InvariantCulture);
                }
            }
        }

        [ObservableProperty]
        private string _notes = string.Empty;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _isSaving;

        public IrrigationModalViewModel(MainViewModel main, ILandService landService, LandDetailViewModel detailViewModel, Land targetLand, double? suggestedVolume = null)
        {
            _main = main;
            _landService = landService;
            _detailViewModel = detailViewModel;
            TargetLand = targetLand;
            _suggestedVolume = suggestedVolume;

            if (_suggestedVolume.HasValue && _suggestedVolume.Value > 0)
            {
                if (_suggestedVolume.Value < 1.0)
                {
                    SelectedVolumeUnit = "Mililiter (mL)";
                    VolumeValueText = (_suggestedVolume.Value * 1000.0).ToString("0.##", CultureInfo.InvariantCulture);
                }
                else
                {
                    SelectedVolumeUnit = "Liter (L)";
                    VolumeValueText = _suggestedVolume.Value.ToString("0.##", CultureInfo.InvariantCulture);
                }
                Notes = "Mengikuti rekomendasi takaran air cerdas.";
            }
            else
            {
                VolumeValueText = string.Empty;
                Notes = string.Empty;
                SelectedVolumeUnit = "Liter (L)";
            }
        }

        public IrrigationModalViewModel(MainViewModel main, ILandService landService, LandDetailViewModel detailViewModel, Land targetLand, IrrigationLog existingLog)
        {
            _main = main;
            _landService = landService;
            _detailViewModel = detailViewModel;
            TargetLand = targetLand;
            ExistingLog = existingLog;

            Date = existingLog.Date;
            if (existingLog.VolumeLiters < 1.0 && existingLog.VolumeLiters > 0)
            {
                SelectedVolumeUnit = "Mililiter (mL)";
                VolumeValueText = (existingLog.VolumeLiters * 1000.0).ToString("0.##", CultureInfo.InvariantCulture);
            }
            else
            {
                SelectedVolumeUnit = "Liter (L)";
                VolumeValueText = existingLog.VolumeLiters.ToString("0.##", CultureInfo.InvariantCulture);
            }
            Notes = existingLog.Notes ?? string.Empty;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(VolumeValueText) || 
                !double.TryParse(VolumeValueText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double rawVolume) || 
                rawVolume <= 0)
            {
                ErrorMessage = "Volume air wajib diisi dengan angka positif.";
                return;
            }

            double volumeInLiters = SelectedVolumeUnit.Contains("mL") ? (rawVolume / 1000.0) : rawVolume;

            IsSaving = true;
            try
            {
                if (IsEditMode && ExistingLog != null)
                {
                    ExistingLog.Date = Date;
                    ExistingLog.VolumeLiters = volumeInLiters;
                    ExistingLog.Notes = Notes?.Trim() ?? string.Empty;

                    bool success = await _landService.UpdateIrrigationLogAsync(ExistingLog);
                    if (success)
                    {
                        await _detailViewModel.LoadLogsAsync();
                        _main.CloseModal();
                        _main.ShowToast("Catatan penyiraman berhasil diperbarui!");
                    }
                    else
                    {
                        ErrorMessage = "Gagal memperbarui catatan penyiraman. Periksa koneksi Anda.";
                    }
                }
                else
                {
                    var newLog = new IrrigationLog
                    {
                        LandId = TargetLand.Id,
                        Date = Date,
                        VolumeLiters = volumeInLiters,
                        Notes = Notes?.Trim() ?? string.Empty
                    };

                    await _landService.AddIrrigationLogAsync(TargetLand.Id, newLog);
                    await _detailViewModel.LoadLogsAsync();

                    _main.CloseModal();
                    _main.ShowToast("Penyiraman berhasil dicatat!");
                }
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
