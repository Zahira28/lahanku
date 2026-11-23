using System;
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
    /// Main Dashboard ViewModel with loading indicator, error handling, and retry banner (SRP).
    /// </summary>
    public partial class DashboardViewModel : ViewModelBase, IKeyboardShortcutHandler
    {
        private readonly MainViewModel _main;
        private readonly ILandService _landService;
        private readonly IAuthService _authService;
        private readonly IErrorHandler _errorHandler;

        [ObservableProperty]
        private ObservableCollection<Land> _lands = new();

        [ObservableProperty]
        private string _totalLands = "0";

        [ObservableProperty]
        private string _totalArea = "0";

        [ObservableProperty]
        private string _totalVarieties = "0";

        [ObservableProperty]
        private bool _hasLands;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string _greetingText = "Selamat Siang!";

        [ObservableProperty]
        private string _selectedNav = "Ringkasan";

        [ObservableProperty]
        private bool _isSettingsView;

        // today's watering plan, one card per land
        [ObservableProperty]
        private ObservableCollection<LandPlan> _todayPlans = new();

        [ObservableProperty]
        private bool _isLoadingPlans;

        // summary card: water saved over the last 7 days
        [ObservableProperty]
        private string _savedText = "-";

        [ObservableProperty]
        private string _savedNote = "Belum ada data";

        [ObservableProperty]
        private string _totalAreaUnit = "ha";

        public string TodayText => DateTime.Today.ToString("dddd, d MMMM yyyy", CultureInfo.GetCultureInfo("id-ID"));

        private readonly IDailyPlanService _dailyPlan;
        private readonly IDailyRecommendationService _dailyRecommendations;

        public string CurrentUserName => _authService.CurrentUser?.DisplayName ?? "Petani";

        public string CurrentUserUsername => _authService.CurrentUser?.Handle ?? "@petani";

        public DashboardViewModel(MainViewModel main, ILandService landService, IAuthService authService, IErrorHandler? errorHandler = null)
        {
            _main = main;
            _landService = landService;
            _authService = authService;
            _errorHandler = errorHandler ?? new ErrorHandler();
            _dailyRecommendations = new DailyRecommendationService(landService);
            _dailyPlan = new DailyPlanService(new WeatherService(), new IrrigationAdvisorService(), _dailyRecommendations);

            UpdateGreeting();
            _ = LoadLandsAsync();
        }

        private void UpdateGreeting()
        {
            var hour = DateTime.Now.Hour;
            if (hour >= 4 && hour < 11)
            {
                GreetingText = "Selamat Pagi!";
            }
            else if (hour >= 11 && hour < 15)
            {
                GreetingText = "Selamat Siang!";
            }
            else if (hour >= 15 && hour < 18)
            {
                GreetingText = "Selamat Sore!";
            }
            else
            {
                GreetingText = "Selamat Malam!";
            }
        }

        [RelayCommand]
        public async Task LoadLandsAsync()
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;

            try
            {
                var currentUserId = _authService.CurrentUser?.Id;
                var landsList = await _landService.GetLandsAsync(currentUserId);
                Lands.Clear();
                foreach (var item in landsList)
                {
                    Lands.Add(item);
                }

                HasLands = Lands.Count > 0;

                if (HasLands)
                {
                    TotalLands = Lands.Count.ToString();

                    // small gardens read better in m²
                    double totalHa = Lands.Sum(l => l.AreaHectares);
                    var id = CultureInfo.GetCultureInfo("id-ID");
                    if (totalHa < 0.1)
                    {
                        TotalArea = (totalHa * 10000).ToString("#,0", id);
                        TotalAreaUnit = "m²";
                    }
                    else
                    {
                        TotalArea = totalHa.ToString("0.##", id);
                        TotalAreaUnit = "ha";
                    }

                    var distinctCrops = Lands
                        .Select(l => l.CropId?.ToString() ?? l.CropType?.Trim().ToLowerInvariant())
                        .Where(key => !string.IsNullOrEmpty(key))
                        .Distinct()
                        .Count();

                    TotalVarieties = distinctCrops.ToString();
                    _ = LoadPlansAsync();
                }
                else
                {
                    TotalLands = "0";
                    TotalArea = "0";
                    TotalVarieties = "0";
                    TodayPlans.Clear();
                    SavedText = "-";
                    SavedNote = "Belum ada data";
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = "Gagal memuat data dari Supabase. Silakan periksa koneksi internet Anda.";
                _errorHandler.HandleError(ex, ErrorMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // weather + recommendation per land (in parallel), then the weekly summary line
        private async Task LoadPlansAsync()
        {
            IsLoadingPlans = true;
            try
            {
                var lands = Lands.ToList();
                var plans = await Task.WhenAll(lands.Select(l => _dailyPlan.CalculateAsync(l, DateTime.Now)));
                TodayPlans.Clear();
                foreach (var plan in plans)
                {
                    TodayPlans.Add(plan);
                }

                ApplySummary(await BuildWeeklySummaryAsync(lands));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] Failed to load plans: {ex}");
            }
            finally
            {
                IsLoadingPlans = false;
            }
        }

        private void ApplySummary(WaterSummary summary)
        {
            var id = CultureInfo.GetCultureInfo("id-ID");
            if (!summary.HasData)
            {
                SavedText = "-";
                SavedNote = "Belum ada data";
                return;
            }

            SavedText = Math.Round(summary.Saved, 0).ToString("#,0", id);
            SavedNote = summary.Saved >= 0
                ? $"{summary.SavedPercent.ToString("0", id)}% dari penyiraman rutin"
                : "Lebih banyak dari penyiraman rutin";
        }

        private async Task<WaterSummary> BuildWeeklySummaryAsync(System.Collections.Generic.List<Land> lands)
        {
            var from = DateTime.Today.AddDays(-6);
            var perLand = await Task.WhenAll(lands.Select(async l =>
            {
                var records = await _dailyRecommendations.GetHistoryAsync(l.Id, from, DateTime.Today);
                var logs = await _landService.GetIrrigationLogsAsync(l.Id);
                return (records, logs);
            }));

            return WaterSummary.Calculate(
                perLand.SelectMany(p => p.records),
                perLand.SelectMany(p => p.logs));
        }

        [RelayCommand]
        private void RecordWatering(LandPlan? plan)
        {
            if (plan == null)
            {
                return;
            }

            _main.OpenModal(new IrrigationModalViewModel(_main, _landService, LoadPlansAsync, plan.Land, plan.Recommendation.RecommendedVolume));
        }

        [RelayCommand]
        private void OpenAddLandModal()
        {
            _main.OpenModal(new LandModalViewModel(_main, _landService, this));
        }

        [RelayCommand]
        private void OpenEditLandModal(Land? land)
        {
            if (land != null)
            {
                _main.OpenModal(new LandModalViewModel(_main, _landService, this, land));
            }
        }

        [RelayCommand]
        private void OpenDeleteLandModal(Land? land)
        {
            if (land != null)
            {
                _main.OpenModal(new DeleteConfirmationViewModel(_main, _landService, this, land));
            }
        }

        [RelayCommand]
        private void SelectLand(Land? land)
        {
            if (land != null)
            {
                _main.NavigateToLandDetail(land);
            }
        }

        [RelayCommand]
        private void SelectNav(string nav)
        {
            SelectedNav = nav;
            IsSettingsView = nav == "Pengaturan";
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            await _authService.LogoutAsync();
            _main.NavigateToLogin();
        }

        // Ctrl+N add land, F5 reload
        public bool HandleKey(Key key, ModifierKeys modifiers) => key switch
        {
            Key.N when modifiers == ModifierKeys.Control => OpenAddLandModalCommand.TryRun(),
            Key.F5 => LoadLandsCommand.TryRun(),
            _ => false
        };
    }
}
