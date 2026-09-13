using System;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Services;

namespace Lahanku.ViewModels
{
    public partial class MapPickerModalViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private readonly IGeocodingService _geocodingService;
        private readonly Action<double, double, string>? _onLocationSelected;

        [ObservableProperty]
        private double _selectedLatitude;

        [ObservableProperty]
        private double _selectedLongitude;

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private string _detectedAddressText = "Pilih atau klik titik pada peta untuk menentukan lokasi...";

        [ObservableProperty]
        private string _detailedAddressText = string.Empty;

        [ObservableProperty]
        private bool _isLoadingAddress;

        [ObservableProperty]
        private bool _isSearching;

        [ObservableProperty]
        private string? _errorMessage;

        public bool HasSelectedLocation => Math.Abs(SelectedLatitude) > 0.0001 || Math.Abs(SelectedLongitude) > 0.0001;

        public string CoordinatesDisplayText => HasSelectedLocation
            ? $"Lat: {SelectedLatitude.ToString("0.00000", CultureInfo.InvariantCulture)} | Long: {SelectedLongitude.ToString("0.00000", CultureInfo.InvariantCulture)}"
            : "Belum ada titik yang dipilih";

        public double InitialLatitude { get; }
        public double InitialLongitude { get; }
        public int InitialZoom { get; }

        public event Action<double, double, int>? MoveMapRequested;

        public MapPickerModalViewModel(
            MainViewModel main, 
            IGeocodingService geocodingService, 
            double? initialLat = null, 
            double? initialLon = null, 
            Action<double, double, string>? onLocationSelected = null)
        {
            _main = main;
            _geocodingService = geocodingService;
            _onLocationSelected = onLocationSelected;

            if (initialLat.HasValue && initialLon.HasValue && Math.Abs(initialLat.Value) > 0.0001)
            {
                InitialLatitude = initialLat.Value;
                InitialLongitude = initialLon.Value;
                InitialZoom = 15;
                SelectedLatitude = initialLat.Value;
                SelectedLongitude = initialLon.Value;
                _ = ReverseGeocodeCurrentAsync();
            }
            else
            {
                // Default titik tengah Indonesia / Jawa Timur
                InitialLatitude = -7.9826;
                InitialLongitude = 112.6308;
                InitialZoom = 11;
                SelectedLatitude = InitialLatitude;
                SelectedLongitude = InitialLongitude;
                _ = ReverseGeocodeCurrentAsync();
            }
        }

        public void UpdateSelectedCoordinatesFromMap(double lat, double lon)
        {
            SelectedLatitude = lat;
            SelectedLongitude = lon;
            OnPropertyChanged(nameof(CoordinatesDisplayText));
            OnPropertyChanged(nameof(HasSelectedLocation));

            _ = ReverseGeocodeCurrentAsync();
        }

        [RelayCommand]
        private async Task SearchLocationAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
                return;

            ErrorMessage = null;
            IsSearching = true;

            try
            {
                var input = SearchQuery.Trim();

                // 1. Cek apakah pengguna menempel link Google Maps atau koordinat langsung
                var parsedCoords = await _geocodingService.ParseGoogleMapsUrlAsync(input);
                if (parsedCoords.HasValue)
                {
                    SelectedLatitude = parsedCoords.Value.lat;
                    SelectedLongitude = parsedCoords.Value.lon;
                    OnPropertyChanged(nameof(CoordinatesDisplayText));
                    OnPropertyChanged(nameof(HasSelectedLocation));

                    MoveMapRequested?.Invoke(SelectedLatitude, SelectedLongitude, 16);
                    await ReverseGeocodeCurrentAsync();
                    return;
                }

                // 2. Jika bukan link, cari nama tempat / daerah
                var searchResults = await _geocodingService.SearchPlaceAsync(input);
                if (searchResults.Count > 0)
                {
                    var bestMatch = searchResults[0];
                    SelectedLatitude = bestMatch.Latitude;
                    SelectedLongitude = bestMatch.Longitude;
                    DetectedAddressText = bestMatch.ShortLocationText;
                    DetailedAddressText = bestMatch.DisplayName;

                    OnPropertyChanged(nameof(CoordinatesDisplayText));
                    OnPropertyChanged(nameof(HasSelectedLocation));

                    MoveMapRequested?.Invoke(SelectedLatitude, SelectedLongitude, 15);
                }
                else
                {
                    ErrorMessage = "Lokasi atau nama tempat tidak ditemukan. Silakan periksa ejaan atau klik langsung di peta.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Gagal mencari lokasi: {ex.Message}";
            }
            finally
            {
                IsSearching = false;
            }
        }

        private async Task ReverseGeocodeCurrentAsync()
        {
            IsLoadingAddress = true;
            try
            {
                var res = await _geocodingService.ReverseGeocodeAsync(SelectedLatitude, SelectedLongitude);
                if (res != null)
                {
                    DetectedAddressText = res.ShortLocationText;
                    DetailedAddressText = res.DisplayName;
                }
                else
                {
                    DetectedAddressText = $"Titik ({SelectedLatitude:0.0000}, {SelectedLongitude:0.0000})";
                    DetailedAddressText = string.Empty;
                }
            }
            catch
            {
                DetectedAddressText = $"Titik ({SelectedLatitude:0.0000}, {SelectedLongitude:0.0000})";
            }
            finally
            {
                IsLoadingAddress = false;
            }
        }

        [RelayCommand]
        private void ConfirmLocation()
        {
            _onLocationSelected?.Invoke(SelectedLatitude, SelectedLongitude, DetectedAddressText);
            _main.CloseModal();
        }

        [RelayCommand]
        private void Cancel()
        {
            _main.CloseModal();
        }
    }
}
