using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Models;
using Lahanku.Services;

namespace Lahanku.ViewModels
{
    public partial class LandDetailViewModel : ViewModelBase, IDisposable
    {
        private readonly MainViewModel _main;
        private readonly ILandService _landService;
        private readonly IWeatherService _weatherService;
        private readonly IIrrigationAdvisorService _advisorService;
        private readonly DispatcherTimer _weatherAutoRefreshTimer;

        [ObservableProperty]
        private Land _land;

        [ObservableProperty]
        private ObservableCollection<IrrigationLog> _irrigationLogs = new();

        [ObservableProperty]
        private string _selectedTab = "Penyiraman";

        [ObservableProperty]
        private bool _hasLogs;

        [ObservableProperty]
        private bool _isLoadingLogs = true;

        // Visual Weather & Climate-Smart State
        [ObservableProperty]
        private Cuaca? _currentWeather;

        [ObservableProperty]
        private EvaluasiIrigasi? _irrigationAdvice;

        [ObservableProperty]
        private bool _isLoadingWeather = true;

        [ObservableProperty]
        private string _weatherIcon = "⛅";

        [ObservableProperty]
        private string _weatherTempText = "-- °C";

        [ObservableProperty]
        private string _weatherHumidityText = "-- %";

        [ObservableProperty]
        private string _weatherPrecipText = "-- mm";

        [ObservableProperty]
        private string _weatherConditionText = "Memuat data satelit cuaca...";

        [ObservableProperty]
        private string _adviceStatus = "Menghitung...";

        [ObservableProperty]
        private string _adviceSaran = "Menghubungkan ke satelit Open-Meteo...";

        [ObservableProperty]
        private string _adviceVolumeText = "-- Liter";

        [ObservableProperty]
        private bool _canApplyAdvice;

        public LandDetailViewModel(
            MainViewModel main,
            ILandService landService,
            Land land,
            IWeatherService? weatherService = null,
            IIrrigationAdvisorService? advisorService = null)
        {
            _main = main;
            _landService = landService;
            _land = land;
            _weatherService = weatherService ?? new WeatherService();
            _advisorService = advisorService ?? new IrrigationAdvisorService();

            _weatherAutoRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5)
            };
            _weatherAutoRefreshTimer.Tick += OnWeatherAutoRefreshTick;
            _weatherAutoRefreshTimer.Start();

            _ = LoadLogsAsync();
            _ = LoadWeatherAndAdviceAsync();
        }

        private async void OnWeatherAutoRefreshTick(object? sender, EventArgs e)
        {
            await LoadWeatherAndAdviceAsync();
        }

        [RelayCommand]
        public async Task LoadWeatherAndAdviceAsync()
        {
            IsLoadingWeather = true;
            try
            {
                // Ambil data cuaca satelit real-time berdasarkan koordinat lahan
                CurrentWeather = await _weatherService.GetCurrentWeatherAsync(Land.Latitude, Land.Longitude, Land.Id);

                if (CurrentWeather != null)
                {
                    var (_, icon) = _weatherService.GetWmoDescription(0); // fallback
                    // Dapatkan icon cuaca spesifik
                    if (_weatherService is WeatherService ws)
                    {
                        var (_, wmoIcon) = ws.GetWmoDescription(CurrentWeather.KondisiCuaca.Contains("Hujan") ? 61 : CurrentWeather.KondisiCuaca.Contains("Mendung") ? 3 : 0);
                        WeatherIcon = wmoIcon;
                    }

                    WeatherTempText = $"{CurrentWeather.Suhu.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)} °C";
                    WeatherHumidityText = $"{CurrentWeather.Kelembapan:0}%";
                    WeatherPrecipText = $"{CurrentWeather.CurahHujan.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)} mm";
                    WeatherConditionText = CurrentWeather.KondisiCuaca;
                }
                else
                {
                    WeatherIcon = "🌐";
                    WeatherConditionText = "Data satelit offline (menggunakan estimasi)";
                }

                // Kalkulasi rekomendasi irigasi cerdas (SDG 13 - Climate Action)
                IrrigationAdvice = _advisorService.CalculateRecommendation(Land, CurrentWeather);
                if (IrrigationAdvice != null)
                {
                    AdviceStatus = IrrigationAdvice.StatusEfisiensi;
                    AdviceSaran = IrrigationAdvice.Saran ?? string.Empty;
                    AdviceVolumeText = $"{IrrigationAdvice.VolumeAnjuran:N0} Liter";
                    CanApplyAdvice = IrrigationAdvice.VolumeAnjuran > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LandDetail] Error loading weather: {ex.Message}");
                WeatherConditionText = "Gagal memuat cuaca.";
            }
            finally
            {
                IsLoadingWeather = false;
            }
        }

        [RelayCommand]
        public async Task LoadLogsAsync()
        {
            IsLoadingLogs = true;
            try
            {
                var logs = await _landService.GetIrrigationLogsAsync(Land.Id);
                IrrigationLogs.Clear();
                foreach (var log in logs)
                {
                    IrrigationLogs.Add(log);
                }

                HasLogs = IrrigationLogs.Count > 0;
            }
            finally
            {
                IsLoadingLogs = false;
            }
        }

        [RelayCommand]
        private void BackToDashboard()
        {
            Dispose();
            _main.NavigateToDashboard();
        }

        [RelayCommand]
        private void OpenAddIrrigationModal()
        {
            _main.OpenModal(new IrrigationModalViewModel(_main, _landService, this, Land));
        }

        [RelayCommand]
        private void OpenEditIrrigationModal(IrrigationLog? log)
        {
            if (log != null)
            {
                _main.OpenModal(new IrrigationModalViewModel(_main, _landService, this, Land, log));
            }
        }

        [RelayCommand]
        private void OpenDeleteIrrigationModal(IrrigationLog? log)
        {
            if (log != null)
            {
                _main.OpenModal(new DeleteIrrigationModalViewModel(_main, _landService, this, log));
            }
        }

        [RelayCommand]
        private void ApplyRecommendedIrrigation()
        {
            if (IrrigationAdvice != null && IrrigationAdvice.VolumeAnjuran > 0)
            {
                _main.OpenModal(new IrrigationModalViewModel(_main, _landService, this, Land, IrrigationAdvice.VolumeAnjuran));
            }
            else
            {
                _main.ShowWarningToast("Sistem menganjurkan menunda penyiraman karena hujan/tanah sudah cukup basah.", "Konservasi Air");
            }
        }

        [RelayCommand]
        private void RefreshWeather()
        {
            _weatherAutoRefreshTimer.Stop();
            _weatherAutoRefreshTimer.Start();
            _ = LoadWeatherAndAdviceAsync();
            _main.ShowSuccessToast("Data satelit cuaca diperbarui.", "Sinkronisasi Cuaca");
        }

        [RelayCommand]
        private void SelectTab(string tabName)
        {
            SelectedTab = tabName;
        }

        public void Dispose()
        {
            _weatherAutoRefreshTimer.Stop();
            _weatherAutoRefreshTimer.Tick -= OnWeatherAutoRefreshTick;
        }
    }
}
