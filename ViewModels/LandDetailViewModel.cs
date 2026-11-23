using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Models;
using Lahanku.Services;
using System.Windows.Input;

namespace Lahanku.ViewModels
{
    public partial class LandDetailViewModel : ViewModelBase, IDisposable, IKeyboardShortcutHandler
    {
        private readonly MainViewModel _main;
        private readonly ILandService _landService;
        private readonly IWeatherService _weatherService;
        private readonly IIrrigationAdvisorService _advisorService;
        private readonly IDailyRecommendationService _dailyRecommendations;
        private readonly IDailyPlanService _dailyPlan;
        private readonly DispatcherTimer _weatherAutoRefreshTimer;

        [ObservableProperty]
        private Land _land;

        [ObservableProperty]
        private string _selectedTab = "Penyiraman";

        [ObservableProperty]
        private bool _hasLogs;

        [ObservableProperty]
        private bool _isLoadingLogs = true;

        // Visual Weather & Climate-Smart State
        [ObservableProperty]
        private Weather? _currentWeather;

        [ObservableProperty]
        private Recommendation? _todayRecommendation;

        [ObservableProperty]
        private bool _isLoadingWeather = true;

        [ObservableProperty]
        private string _weatherIcon = "partly";

        [ObservableProperty]
        private string _weatherTempText = "-- °C";

        [ObservableProperty]
        private string _weatherHumidityText = "-- %";

        [ObservableProperty]
        private string _weatherPrecipText = "-- mm";

        // current humidity vs the land's ideal range: "ok", "low", "high", or "" if there's no data
        [ObservableProperty]
        private string _humidityStatus = string.Empty;

        [ObservableProperty]
        private string _humidityStatusText = string.Empty;

        public string HumidityIdealText => Land.HasIdealMoisture ? $"Ideal {Land.MoistureRangeText}" : string.Empty;

        [ObservableProperty]
        private string _weatherConditionText = "Memuat data satelit cuaca...";

        [ObservableProperty]
        private string _adviceStatus = "Menghitung...";

        [ObservableProperty]
        private string _adviceText = "Menghubungkan ke satelit Open-Meteo...";

        // "tunda", "ekstra", or "normal" → picks the recommendation icon & color
        [ObservableProperty]
        private string _todayKind = "normal";

        [ObservableProperty]
        private string _baselineVolumeText = "-- L";

        [ObservableProperty]
        private string _recommendedVolumeText = "-- L";

        // history table rows: real logs + postponed days, with evaluation
        [ObservableProperty]
        private ObservableCollection<HistoryRow> _historyRows = new();

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
            _dailyRecommendations = new DailyRecommendationService(landService);
            _dailyPlan = new DailyPlanService(_weatherService, _advisorService, _dailyRecommendations);

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
                // weather + forecast + recommendation + save the daily record, all at once
                var plan = await _dailyPlan.CalculateAsync(Land, DateTime.Now);
                CurrentWeather = plan.Weather;

                if (CurrentWeather != null)
                {
                    var (_, icon) = _weatherService.GetWmoDescription(CurrentWeather.WeatherCode);
                    WeatherIcon = icon;

                    WeatherTempText = $"{CurrentWeather.Temperature.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)} °C";
                    WeatherHumidityText = $"{CurrentWeather.Humidity:0}%";
                    WeatherPrecipText = $"{CurrentWeather.Rainfall.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)} mm";
                    WeatherConditionText = CurrentWeather.Condition;
                    UpdateHumidityStatus(CurrentWeather.Humidity);
                }
                else
                {
                    WeatherIcon = "offline";
                    WeatherConditionText = "Data satelit offline (menggunakan estimasi)";
                }

                TodayRecommendation = plan.Recommendation;

                AdviceStatus = TodayRecommendation.Title;
                AdviceText = string.IsNullOrEmpty(TodayRecommendation.TimingAdvice)
                    ? TodayRecommendation.Reason
                    : $"{TodayRecommendation.Reason} {TodayRecommendation.TimingAdvice}";
                TodayKind = plan.Kind;
                BaselineVolumeText = Liters(TodayRecommendation.BaselineVolume);
                RecommendedVolumeText = Liters(TodayRecommendation.RecommendedVolume);
                CanApplyAdvice = TodayRecommendation.RecommendedVolume > 0;

                // today's record may have just been created → show it in the history without a skeleton flash
                await LoadHistoryAsync(showSkeleton: false);
            }
            catch (Exception ex)
            {
                // only shown in the weather panel, no toast on every 5-minute auto-refresh
                System.Diagnostics.Debug.WriteLine($"[LandDetail] Error loading weather: {ex.Message}");
                WeatherIcon = "offline";
                WeatherConditionText = "Gagal memuat cuaca.";
            }
            finally
            {
                IsLoadingWeather = false;
            }
        }

        private void UpdateHumidityStatus(double humidity)
        {
            if (!Land.HasIdealMoisture)
            {
                HumidityStatus = string.Empty;
                HumidityStatusText = string.Empty;
                return;
            }

            if (humidity < Land.IdealMoistureMin)
            {
                HumidityStatus = "low";
                HumidityStatusText = "Terlalu kering";
            }
            else if (humidity > Land.IdealMoistureMax)
            {
                HumidityStatus = "high";
                HumidityStatusText = "Terlalu lembap";
            }
            else
            {
                HumidityStatus = "ok";
                HumidityStatusText = "Sesuai";
            }
        }

        [RelayCommand]
        public Task LoadLogsAsync() => LoadHistoryAsync(showSkeleton: true);

        private async Task LoadHistoryAsync(bool showSkeleton)
        {
            if (showSkeleton)
            {
                IsLoadingLogs = true;
            }

            try
            {
                var logsTask = _landService.GetIrrigationLogsAsync(Land.Id);
                var recordsTask = _dailyRecommendations.GetHistoryAsync(Land.Id, DateTime.Today.AddDays(-HistoryDays), DateTime.Today);
                await Task.WhenAll(logsTask, recordsTask);

                var rows = HistoryRow.Build(logsTask.Result, recordsTask.Result);
                HistoryRows.Clear();
                foreach (var row in rows)
                {
                    HistoryRows.Add(row);
                }

                HasLogs = HistoryRows.Count > 0;
            }
            catch (Exception ex)
            {
                _main.ErrorHandler.HandleError(ex, "Gagal memuat riwayat penyiraman. Periksa koneksi internet Anda.");
            }
            finally
            {
                IsLoadingLogs = false;
            }
        }

        private const int HistoryDays = 90;

        private static string Liters(double value) =>
            value.ToString("#,0", System.Globalization.CultureInfo.GetCultureInfo("id-ID")) + " L";

        public string CurrentUserName => _main.AuthService.CurrentUser?.DisplayName ?? "Petani";

        public string CurrentUserUsername => _main.AuthService.CurrentUser?.Handle ?? "@petani";

        [RelayCommand]
        private void BackToDashboard()
        {
            Dispose();
            _main.NavigateToDashboard();
        }

        [RelayCommand]
        private void OpenSettings()
        {
            Dispose();
            _main.NavigateToDashboard();
            if (_main.CurrentView is DashboardViewModel dashboard)
            {
                dashboard.SelectNavCommand.Execute("Pengaturan");
            }
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            Dispose();
            await _main.AuthService.LogoutAsync();
            _main.NavigateToLogin();
        }

        [RelayCommand]
        private void OpenAddIrrigationModal()
        {
            _main.OpenModal(new IrrigationModalViewModel(_main, _landService, LoadLogsAsync, Land));
        }

        [RelayCommand]
        private void OpenEditIrrigationModal(IrrigationLog? log)
        {
            if (log != null)
            {
                _main.OpenModal(new IrrigationModalViewModel(_main, _landService, LoadLogsAsync, Land, log));
            }
        }

        [RelayCommand]
        private void OpenDeleteIrrigationModal(IrrigationLog? log)
        {
            if (log != null)
            {
                _main.OpenModal(new DeleteIrrigationModalViewModel(_main, _landService, LoadLogsAsync, log));
            }
        }

        [RelayCommand]
        private void ApplyRecommendedIrrigation()
        {
            if (TodayRecommendation != null && TodayRecommendation.RecommendedVolume > 0)
            {
                _main.OpenModal(new IrrigationModalViewModel(_main, _landService, LoadLogsAsync, Land, TodayRecommendation.RecommendedVolume));
            }
            else
            {
                _main.ShowWarningToast("Sistem menganjurkan menunda penyiraman karena hujan/tanah sudah cukup basah.", "Konservasi Air");
            }
        }

        [RelayCommand]
        private async Task RefreshWeatherAsync()
        {
            _weatherAutoRefreshTimer.Stop();
            _weatherAutoRefreshTimer.Start();
            await LoadWeatherAndAdviceAsync();

            if (CurrentWeather != null)
            {
                _main.ShowSuccessToast("Data satelit cuaca diperbarui.", "Sinkronisasi Cuaca");
            }
            else
            {
                _main.ShowWarningToast("Data cuaca belum bisa diambil, anjuran pakai estimasi standar dulu.", "Cuaca Offline");
            }
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

        // Ctrl+N water land, F5 refresh, Esc / Alt+Left back to dashboard
        public bool HandleKey(Key key, ModifierKeys modifiers)
        {
            switch (key)
            {
                case Key.N when modifiers == ModifierKeys.Control:
                    return OpenAddIrrigationModalCommand.TryRun();
                case Key.F5:
                    RefreshWeatherCommand.TryRun();
                    LoadLogsCommand.TryRun();
                    return true;
                case Key.Escape:
                case Key.Left when modifiers == ModifierKeys.Alt:
                    return BackToDashboardCommand.TryRun();
                default:
                    return false;
            }
        }
    }
}
