using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Models;
using Lahanku.Services;
using System.Windows.Input;

namespace Lahanku.ViewModels
{
    /// <summary>
    /// ViewModel untuk dialog tambah dan edit lahan.
    /// Memuat katalog varietas tanaman secara dinamis dari Supabase (SRP & Loose Coupling).
    /// </summary>
    public partial class LandModalViewModel : ViewModelBase, IKeyboardShortcutHandler
    {
        private readonly MainViewModel _main;
        private readonly ILandService _landService;
        private readonly DashboardViewModel _dashboard;
        private readonly Land? _originalLand;
        private readonly IGeocodingService _geocodingService;
        private List<Tanaman> _availableTanaman = new();

        public bool IsEditMode { get; }

        public string ModalSubtitle => IsEditMode ? "PEMBARUAN INFORMASI" : "FORMULIR REGISTRASI";

        public string ModalTitle => IsEditMode ? $"Edit {_originalLand?.Name ?? "Lahan"}" : "Lahan Baru";

        public string SubmitButtonText => IsEditMode ? "Simpan" : "Simpan Lahan";

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _locationDescription = string.Empty;

        [ObservableProperty]
        private string _latitudeText = string.Empty;

        [ObservableProperty]
        private string _longitudeText = string.Empty;

        partial void OnLatitudeTextChanged(string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && (value.Contains("maps") || value.Contains("http") || value.Contains("@")))
            {
                _ = TryParseGoogleMapsInputAsync(value);
            }
        }

        private async Task TryParseGoogleMapsInputAsync(string input)
        {
            var coords = await _geocodingService.ParseGoogleMapsUrlAsync(input);
            if (coords.HasValue)
            {
                LatitudeText = coords.Value.lat.ToString("0.00000", CultureInfo.InvariantCulture);
                LongitudeText = coords.Value.lon.ToString("0.00000", CultureInfo.InvariantCulture);

                var geo = await _geocodingService.ReverseGeocodeAsync(coords.Value.lat, coords.Value.lon);
                if (geo != null && !string.IsNullOrWhiteSpace(geo.ShortLocationText))
                {
                    LocationDescription = geo.ShortLocationText;
                }
                _main.ShowToast("Berhasil mendeteksi koordinat dari tautan!", "Tautan Google Maps");
            }
        }

        [ObservableProperty]
        private string _areaValueText = string.Empty;

        [ObservableProperty]
        private string _selectedAreaUnit = "Hektar (ha)";

        public ObservableCollection<string> AreaUnitOptions { get; } = new()
        {
            "Hektar (ha)",
            "Meter² (m²)"
        };

        public string AreaPlaceholder => SelectedAreaUnit?.Contains("m²") == true ? "Contoh: 500" : "Contoh: 0.5";

        /// <summary>
        /// Alias untuk kompatibilitas properti lama
        /// </summary>
        public string AreaHectaresText
        {
            get => AreaValueText;
            set => AreaValueText = value;
        }

        partial void OnSelectedAreaUnitChanged(string? oldValue, string newValue)
        {
            OnPropertyChanged(nameof(AreaPlaceholder));
            if (!string.IsNullOrWhiteSpace(AreaValueText) && 
                double.TryParse(AreaValueText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double val) && 
                val > 0 && oldValue != null)
            {
                if (oldValue.Contains("ha") && newValue.Contains("m²"))
                {
                    // Ha -> m²
                    AreaValueText = (val * 10000.0).ToString("0.##", CultureInfo.InvariantCulture);
                }
                else if (oldValue.Contains("m²") && newValue.Contains("ha"))
                {
                    // m² -> Ha
                    AreaValueText = (val / 10000.0).ToString("0.####", CultureInfo.InvariantCulture);
                }
            }
        }

        [ObservableProperty]
        private string _selectedCropType = string.Empty;

        // kelembapan udara ideal (%), otomatis keisi dari katalog waktu tanaman dipilih
        [ObservableProperty]
        private string _minMoistureText = string.Empty;

        [ObservableProperty]
        private string _maxMoistureText = string.Empty;

        [ObservableProperty]
        private string _cropSearchQuery = string.Empty;

        [ObservableProperty]
        private bool _isCropDropdownOpen;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _isSaving;

        [ObservableProperty]
        private bool _isLoadingCrops = true;

        public ObservableCollection<string> CropOptions { get; } = new();
        public ObservableCollection<string> FilteredCropOptions { get; } = new();

        public string FilteredCropCountText => $"Menampilkan {FilteredCropOptions.Count} dari {CropOptions.Count} tanaman";
        public bool HasNoFilteredCrops => FilteredCropOptions.Count == 0;

        public LandModalViewModel(MainViewModel main, ILandService landService, DashboardViewModel dashboard, Land? landToEdit = null, IGeocodingService? geocodingService = null)
        {
            _main = main;
            _landService = landService;
            _dashboard = dashboard;
            _originalLand = landToEdit;
            _geocodingService = geocodingService ?? new NominatimGeocodingService();

            IsEditMode = landToEdit != null;

            if (IsEditMode && landToEdit != null)
            {
                Name = landToEdit.Name;
                LocationDescription = landToEdit.LocationDescription;
                LatitudeText = landToEdit.Latitude.ToString("0.0000", CultureInfo.InvariantCulture);
                LongitudeText = landToEdit.Longitude.ToString("0.0000", CultureInfo.InvariantCulture);
                SelectedCropType = landToEdit.CropType;
                MinMoistureText = FormatMoisture(landToEdit.IdealMoistureMin);
                MaxMoistureText = FormatMoisture(landToEdit.IdealMoistureMax);

                if (landToEdit.AreaHectares < 0.1 && landToEdit.AreaHectares > 0)
                {
                    SelectedAreaUnit = "Meter² (m²)";
                    AreaValueText = (landToEdit.AreaHectares * 10000.0).ToString("0.##", CultureInfo.InvariantCulture);
                }
                else
                {
                    SelectedAreaUnit = "Hektar (ha)";
                    AreaValueText = landToEdit.AreaHectares.ToString("0.##", CultureInfo.InvariantCulture);
                }
            }
            else
            {
                Name = string.Empty;
                LocationDescription = string.Empty;
                LatitudeText = string.Empty;
                LongitudeText = string.Empty;
                AreaValueText = string.Empty;
                SelectedAreaUnit = "Hektar (ha)";
                SelectedCropType = string.Empty;
            }

            _ = LoadCropsAsync();
        }

        [RelayCommand]
        private void OpenMapPicker()
        {
            double? initialLat = null;
            double? initialLon = null;

            if (double.TryParse(LatitudeText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(LongitudeText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
            {
                initialLat = lat;
                initialLon = lon;
            }

            var mapPickerVm = new MapPickerModalViewModel(
                _main,
                _geocodingService,
                initialLat,
                initialLon,
                (selectedLat, selectedLon, detectedAddress) =>
                {
                    LatitudeText = selectedLat.ToString("0.00000", CultureInfo.InvariantCulture);
                    LongitudeText = selectedLon.ToString("0.00000", CultureInfo.InvariantCulture);

                    if (!string.IsNullOrWhiteSpace(detectedAddress))
                    {
                        LocationDescription = detectedAddress;
                    }

                    _main.ShowToast("Lokasi lahan berhasil ditentukan dari peta!", "Titik Terpilih");
                });

            _main.OpenModal(mapPickerVm);
        }

        private async Task LoadCropsAsync()
        {
            IsLoadingCrops = true;
            try
            {
                _availableTanaman = await _landService.GetTanamanListAsync();

                CropOptions.Clear();
                foreach (var tanaman in _availableTanaman)
                {
                    CropOptions.Add(tanaman.DisplayName);
                }

                if (IsEditMode && !string.IsNullOrWhiteSpace(SelectedCropType))
                {
                    if (!CropOptions.Contains(SelectedCropType))
                    {
                        CropOptions.Insert(0, SelectedCropType);
                    }
                }

                UpdateFilteredCrops();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LandModal] Error loading crops: {ex.Message}");
            }
            finally
            {
                IsLoadingCrops = false;
            }
        }

        partial void OnCropSearchQueryChanged(string value)
        {
            UpdateFilteredCrops();
        }

        private void UpdateFilteredCrops()
        {
            FilteredCropOptions.Clear();
            var query = CropSearchQuery?.Trim().ToLowerInvariant() ?? string.Empty;

            var matches = string.IsNullOrEmpty(query)
                ? CropOptions
                : CropOptions.Where(c => c.ToLowerInvariant().Contains(query));

            foreach (var crop in matches)
            {
                FilteredCropOptions.Add(crop);
            }

            OnPropertyChanged(nameof(FilteredCropCountText));
            OnPropertyChanged(nameof(HasNoFilteredCrops));
        }

        [RelayCommand]
        private void ToggleCropDropdown()
        {
            IsCropDropdownOpen = !IsCropDropdownOpen;
            if (IsCropDropdownOpen)
            {
                CropSearchQuery = string.Empty;
                UpdateFilteredCrops();
            }
        }

        [RelayCommand]
        private void ClearCropSearch()
        {
            CropSearchQuery = string.Empty;
            UpdateFilteredCrops();
        }

        [RelayCommand]
        private void SelectCrop(string? crop)
        {
            if (!string.IsNullOrWhiteSpace(crop))
            {
                SelectedCropType = crop;

                var tanaman = _availableTanaman.FirstOrDefault(t => t.DisplayName == crop);
                if (tanaman != null && tanaman.KelembapanIdealMax > 0)
                {
                    MinMoistureText = FormatMoisture(tanaman.KelembapanIdealMin);
                    MaxMoistureText = FormatMoisture(tanaman.KelembapanIdealMax);
                }
            }
            IsCropDropdownOpen = false;
        }

        private static string FormatMoisture(double? value) =>
            value.HasValue ? value.Value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;

        // kosong dua-duanya = ikut katalog tanaman
        private bool TryParseMoisture(out double? min, out double? max)
        {
            min = null;
            max = null;

            bool minEmpty = string.IsNullOrWhiteSpace(MinMoistureText);
            bool maxEmpty = string.IsNullOrWhiteSpace(MaxMoistureText);
            if (minEmpty && maxEmpty)
            {
                return true;
            }

            if (minEmpty || maxEmpty)
            {
                ErrorMessage = "Isi kelembapan ideal minimal dan maksimal, atau kosongkan keduanya.";
                return false;
            }

            if (!double.TryParse(MinMoistureText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedMin) || parsedMin < 0 || parsedMin > 100)
            {
                ErrorMessage = "Kelembapan ideal minimal harus berupa angka antara 0% dan 100%.";
                return false;
            }

            if (!double.TryParse(MaxMoistureText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedMax) || parsedMax < 0 || parsedMax > 100)
            {
                ErrorMessage = "Kelembapan ideal maksimal harus berupa angka antara 0% dan 100%.";
                return false;
            }

            if (parsedMin > parsedMax)
            {
                ErrorMessage = "Kelembapan ideal minimal tidak boleh lebih besar dari kelembapan maksimal.";
                return false;
            }

            min = parsedMin;
            max = parsedMax;
            return true;
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

            if (string.IsNullOrWhiteSpace(LatitudeText) || !double.TryParse(LatitudeText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
            {
                ErrorMessage = "Koordinat Latitude wajib diisi (format angka).";
                return;
            }

            if (string.IsNullOrWhiteSpace(LongitudeText) || !double.TryParse(LongitudeText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
            {
                ErrorMessage = "Koordinat Longitude wajib diisi (format angka).";
                return;
            }

            if (string.IsNullOrWhiteSpace(AreaValueText) || !double.TryParse(AreaValueText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double rawArea) || rawArea <= 0)
            {
                ErrorMessage = "Luas lahan wajib diisi dengan angka positif.";
                return;
            }

            double areaInHectares = SelectedAreaUnit.Contains("m²") ? (rawArea / 10000.0) : rawArea;

            if (areaInHectares > 1)
            {
                ErrorMessage = "LahanKu ditujukan untuk kebun dengan luas maksimal 1 ha (10.000 m²).";
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedCropType))
            {
                ErrorMessage = "Silakan pilih jenis tanaman yang ditanam.";
                return;
            }

            if (!TryParseMoisture(out double? minMoisture, out double? maxMoisture))
            {
                return;
            }

            // Cari ID Tanaman yang cocok dari pilihan katalog
            var matchedTanaman = _availableTanaman.FirstOrDefault(t => 
                string.Equals(t.DisplayName, SelectedCropType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t.NamaTanaman, SelectedCropType, StringComparison.OrdinalIgnoreCase));

            IsSaving = true;
            try
            {
                var currentUserId = _main.AuthService.CurrentUser?.Id;

                if (IsEditMode && _originalLand != null)
                {
                    _originalLand.Name = Name.Trim();
                    _originalLand.LocationDescription = LocationDescription.Trim();
                    _originalLand.Latitude = lat;
                    _originalLand.Longitude = lon;
                    _originalLand.AreaHectares = areaInHectares;
                    _originalLand.MinMoisture = minMoisture;
                    _originalLand.MaxMoisture = maxMoisture;
                    _originalLand.TanamanId = matchedTanaman?.IdTanaman ?? _originalLand.TanamanId;
                    _originalLand.Tanaman = matchedTanaman ?? _originalLand.Tanaman;
                    _originalLand.CropType = SelectedCropType;

                    bool success = await _landService.UpdateLandAsync(_originalLand);
                    if (!success)
                    {
                        ErrorMessage = "Gagal menyimpan perubahan. Periksa koneksi internet lalu coba lagi.";
                        return;
                    }

                    await _dashboard.LoadLandsAsync();

                    _main.CloseModal();
                    _main.ShowToast("Data berhasil diperbarui!");
                }
                else
                {
                    var newLand = new Land
                    {
                        OwnerId = currentUserId,
                        TanamanId = matchedTanaman?.IdTanaman,
                        Tanaman = matchedTanaman,
                        Name = Name.Trim(),
                        LocationDescription = string.IsNullOrWhiteSpace(LocationDescription) ? Name.Trim() : LocationDescription.Trim(),
                        Latitude = lat,
                        Longitude = lon,
                        AreaHectares = areaInHectares,
                        MinMoisture = minMoisture,
                        MaxMoisture = maxMoisture,
                        CropType = SelectedCropType
                    };

                    await _landService.AddLandAsync(newLand);
                    await _dashboard.LoadLandsAsync();

                    _main.CloseModal();
                    _main.ShowToast("Data berhasil ditambahkan!");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Gagal menyimpan data: {ex.Message}";
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

        public bool HandleKey(Key key, ModifierKeys modifiers)
        {
            // kalau dropdown tanaman kebuka, Enter pilih hasil teratas dan Esc nutup dropdown-nya aja
            if (IsCropDropdownOpen)
            {
                if (key == Key.Enter)
                {
                    return SelectCropCommand.TryRun(FilteredCropOptions.FirstOrDefault());
                }

                if (key == Key.Escape)
                {
                    IsCropDropdownOpen = false;
                    return true;
                }
            }

            return key == Key.Enter && modifiers == ModifierKeys.None && SaveCommand.TryRun();
        }
    }
}
