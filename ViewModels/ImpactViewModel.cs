using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Models;
using Lahanku.Services;

namespace Lahanku.ViewModels
{
    // one row of the per-land table
    public record LandImpactRow(string Name, string AreaText, string BaselineText, string UsedText, string SavedText, bool IsOver, string OnTargetText);

    // Impact page: water saved compared with routine watering, for 7 days / 30 days / everything.
    public partial class ImpactViewModel : ViewModelBase, IKeyboardShortcutHandler
    {
        private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

        private readonly MainViewModel _main;
        private readonly ILandService _landService;
        private readonly IAuthService _authService;
        private readonly IDailyRecommendationService _records;

        // everything is loaded once; switching period only re-filters in memory
        private List<Land> _allLands = new();
        private List<DailyRecommendation> _allRecords = new();
        private List<IrrigationLog> _allLogs = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPeriod7), nameof(IsPeriod30), nameof(IsPeriodAll))]
        private string _period = "7";

        public bool IsPeriod7 => Period == "7";
        public bool IsPeriod30 => Period == "30";
        public bool IsPeriodAll => Period == "semua";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowContent), nameof(ShowEmpty))]
        private bool _isLoading = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowContent), nameof(ShowEmpty))]
        private bool _hasData;

        public bool ShowContent => !IsLoading && HasData;
        public bool ShowEmpty => !IsLoading && !HasData;

        [ObservableProperty] private string _rangeText = string.Empty;
        [ObservableProperty] private string _savedText = "0";
        [ObservableProperty] private string _savedNote = string.Empty;
        [ObservableProperty] private string _postponedDaysText = "0";
        [ObservableProperty] private string _postponedNote = string.Empty;
        [ObservableProperty] private string _adjustmentText = "0";
        [ObservableProperty] private string _adjustmentNote = string.Empty;
        [ObservableProperty] private string _onTargetText = "0";
        [ObservableProperty] private string _onTargetNote = string.Empty;

        [ObservableProperty] private IReadOnlyList<DailyPoint> _daily = Array.Empty<DailyPoint>();
        [ObservableProperty] private double _referenceValue;

        public ObservableCollection<LandImpactRow> LandRows { get; } = new();

        public ImpactViewModel(MainViewModel main, ILandService landService, IAuthService authService)
        {
            _main = main;
            _landService = landService;
            _authService = authService;
            _records = new DailyRecommendationService(landService);
            _ = LoadAsync();
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                _allLands = await _landService.GetLandsAsync(_authService.CurrentUser?.Id);
                var perLand = await Task.WhenAll(_allLands.Select(async land =>
                {
                    var records = await _records.GetHistoryAsync(land.Id, new DateTime(2020, 1, 1), DateTime.Today);
                    var logs = await _landService.GetIrrigationLogsAsync(land.Id);
                    return (records, logs);
                }));

                _allRecords = perLand.SelectMany(p => p.records).ToList();
                _allLogs = perLand.SelectMany(p => p.logs).ToList();
                Apply();
            }
            catch (Exception ex)
            {
                _allRecords.Clear();
                _allLogs.Clear();
                Apply();
                _main.ErrorHandler.HandleError(ex, "Gagal memuat data dampak. Silakan periksa koneksi internet Anda.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void SelectPeriod(string period)
        {
            Period = period;
            Apply();
        }

        private void Apply()
        {
            DateTime from = Period switch
            {
                "7" => DateTime.Today.AddDays(-6),
                "30" => DateTime.Today.AddDays(-29),
                _ => DateTime.MinValue
            };

            var records = _allRecords.Where(r => r.Date.Date >= from && r.Date.Date <= DateTime.Today).ToList();
            var report = ImpactCalculator.Calculate(_allLands, records, _allLogs);
            HasData = report.HasData;

            var start = from == DateTime.MinValue && report.Daily.Count > 0 ? report.Daily[0].Date : from;
            RangeText = start == DateTime.MinValue
                ? string.Empty
                : $"{start.ToString("d MMMM yyyy", Id)} – {DateTime.Today.ToString("d MMMM yyyy", Id)}";

            SavedText = Number(report.Saved);
            SavedNote = report.Saved >= 0
                ? $"{report.SavedPercent.ToString("0", Id)}% lebih rendah dari penyiraman rutin ({Number(report.Baseline)} L)"
                : $"Pemakaian melebihi penyiraman rutin ({Number(report.Baseline)} L)";

            PostponedDaysText = report.PostponedDays.ToString(Id);
            PostponedNote = report.PostponedDays > 0 ? $"menghemat {Number(report.PostponedSaved)} L" : "Belum ada hari yang ditunda";

            AdjustmentText = Number(report.AdjustmentSaved);
            AdjustmentNote = report.AdjustmentSaved >= 0 ? "dari suhu dan kelembapan" : "lebih banyak dari takaran rutin";

            OnTargetText = report.OnTargetPercent.ToString("0", Id);
            OnTargetNote = $"{report.OnTargetCount} dari {report.RecordCount} catatan harian";

            Daily = report.Daily;
            ReferenceValue = Math.Round(report.AverageDailyBaseline);

            LandRows.Clear();
            foreach (var l in report.PerLand)
            {
                LandRows.Add(new LandImpactRow(
                    l.Land.Name,
                    l.Land.AreaText,
                    $"{Number(l.Baseline)} L",
                    $"{Number(l.Used)} L",
                    $"{Number(l.Saved)} L",
                    l.Saved < 0,
                    $"{l.OnTargetPercent.ToString("0", Id)}%"));
            }
        }

        // id-ID thousands, real minus sign for negatives
        private static string Number(double value)
        {
            var rounded = Math.Round(value);
            return rounded < 0 ? $"−{(-rounded).ToString("#,0", Id)}" : rounded.ToString("#,0", Id);
        }

        // F5 reload, 1/2/3 switch period
        public bool HandleKey(Key key, ModifierKeys modifiers)
        {
            if (modifiers != ModifierKeys.None)
            {
                return false;
            }

            switch (key)
            {
                case Key.F5:
                    return LoadCommand.TryRun();
                case Key.D1:
                    SelectPeriod("7");
                    return true;
                case Key.D2:
                    SelectPeriod("30");
                    return true;
                case Key.D3:
                    SelectPeriod("semua");
                    return true;
                case Key.Escape:
                    _main.NavigateToDashboard();
                    return true;
                default:
                    return false;
            }
        }
    }
}
