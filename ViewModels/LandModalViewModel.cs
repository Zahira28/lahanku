using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Models;
using Lahanku.Services;

namespace Lahanku.ViewModels
{
    public partial class LandModalViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private readonly ILandService _landService;
        private readonly DashboardViewModel _dashboard;
        private readonly Land? _originalLand;

        public bool IsEditMode { get; }

        public string ModalSubtitle => IsEditMode ? "PEMBARUAN INFORMASI" : "FORMULIR REGISTRASI";

        public string ModalTitle => IsEditMode ? $"Edit {_originalLand?.Name ?? "Lahan"}" : "Lahan Baru";

        public string SubmitButtonText => IsEditMode ? "Simpan" : "Simpan Lahan";

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _locationDescription = string.Empty;

        [ObservableProperty]
        private string _latitudeText = "-6.2088";

        [ObservableProperty]
        private string _longitudeText = "106.8456";

        [ObservableProperty]
        private string _areaHectaresText = "0.5";

        [ObservableProperty]
        private string _selectedCropType = "Padi Pandan Wangi, Jagung Manis";

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _isSaving;

        public List<string> CropOptions { get; } = new()
        {
            "Padi Pandan Wangi, Jagung Manis",
            "Padi Pandan Wangi",
            "Jagung Manis",
            "Cabai Rawit Merah",
            "Bawang Merah Brebes",
            "Tomat Cherry & Selada",
            "Kedelai Anjasmoro"
        };

        public LandModalViewModel(MainViewModel main, ILandService landService, DashboardViewModel dashboard, Land? landToEdit = null)
        {
            _main = main;
            _landService = landService;
            _dashboard = dashboard;
            _originalLand = landToEdit;

            IsEditMode = landToEdit != null;

            if (IsEditMode && landToEdit != null)
            {
                Name = landToEdit.Name;
                LocationDescription = landToEdit.LocationDescription;
                LatitudeText = landToEdit.Latitude.ToString("0.0000", CultureInfo.InvariantCulture);
                LongitudeText = landToEdit.Longitude.ToString("0.0000", CultureInfo.InvariantCulture);
                AreaHectaresText = landToEdit.AreaHectares.ToString("0.##", CultureInfo.InvariantCulture);
                SelectedCropType = landToEdit.CropType;

                if (!CropOptions.Contains(SelectedCropType) && !string.IsNullOrWhiteSpace(SelectedCropType))
                {
                    CropOptions.Insert(0, SelectedCropType);
                }
            }
            else
            {
                Name = string.Empty;
                LocationDescription = string.Empty;
                LatitudeText = "-6.2088";
                LongitudeText = "106.8456";
                AreaHectaresText = "0.5";
                SelectedCropType = CropOptions[0];
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Nama lahan wajib diisi.";
                return;
            }

            if (!double.TryParse(LatitudeText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
            {
                ErrorMessage = "Format Latitude tidak valid.";
                return;
            }

            if (!double.TryParse(LongitudeText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
            {
                ErrorMessage = "Format Longitude tidak valid.";
                return;
            }

            if (!double.TryParse(AreaHectaresText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double area) || area <= 0)
            {
                ErrorMessage = "Luas lahan harus berupa angka positif.";
                return;
            }

            IsSaving = true;
            try
            {
                if (IsEditMode && _originalLand != null)
                {
                    _originalLand.Name = Name.Trim();
                    _originalLand.LocationDescription = LocationDescription.Trim();
                    _originalLand.Latitude = lat;
                    _originalLand.Longitude = lon;
                    _originalLand.AreaHectares = area;
                    _originalLand.CropType = SelectedCropType;

                    await _landService.UpdateLandAsync(_originalLand);
                    await _dashboard.LoadLandsAsync();

                    _main.CloseModal();
                    _main.ShowToast("Data berhasil diperbarui!");
                }
                else
                {
                    var newLand = new Land
                    {
                        Name = Name.Trim(),
                        LocationDescription = string.IsNullOrWhiteSpace(LocationDescription) ? Name.Trim() : LocationDescription.Trim(),
                        Latitude = lat,
                        Longitude = lon,
                        AreaHectares = area,
                        CropType = SelectedCropType
                    };

                    await _landService.AddLandAsync(newLand);
                    await _dashboard.LoadLandsAsync();

                    _main.CloseModal();
                    _main.ShowToast("Data berhasil ditambahkan!");
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
