using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Lahanku.Models
{
    // one row in the watering history table: a real log, or a postponed day without watering
    public class HistoryRow
    {
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("id-ID");

        public DateTime Date { get; init; }
        public string DateText { get; init; } = string.Empty;
        public string VolumeText { get; init; } = string.Empty;
        public string RecommendationText { get; init; } = "-";
        public EvaluationResult? Evaluation { get; init; }
        public string NotesText { get; init; } = string.Empty;
        public bool NotesEmpty { get; init; }
        public IrrigationLog? Log { get; init; }

        public bool CanEdit => Log != null;
        public string EvaluationText => Evaluation?.Text ?? "-";

        // "ok", "warn", "skip", or "none" → used for the evaluation text color
        public string EvaluationKind => Evaluation?.Category switch
        {
            EvaluationCategory.OnTarget => "ok",
            EvaluationCategory.IsPostponed => "skip",
            null => "none",
            _ => "warn"
        };

        public static List<HistoryRow> Build(IEnumerable<IrrigationLog> logs, IEnumerable<DailyRecommendation> records)
        {
            var recordsByDay = records
                .GroupBy(c => c.Date.Date)
                .ToDictionary(g => g.Key, g => g.First());

            var logList = logs.ToList();
            var totalByDay = logList
                .GroupBy(l => l.Date.Date)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.VolumeLiters));

            var rows = new List<HistoryRow>();

            foreach (var log in logList)
            {
                recordsByDay.TryGetValue(log.Date.Date, out var c);
                bool empty = string.IsNullOrWhiteSpace(log.Notes);
                rows.Add(new HistoryRow
                {
                    Date = log.Date,
                    DateText = log.Date.ToString("dd MMM yyyy, HH:mm", Culture),
                    VolumeText = FormatLiters(log.VolumeLiters),
                    RecommendationText = RecommendationLabel(c),
                    Evaluation = c == null ? null : Evaluate(c, totalByDay[log.Date.Date]),
                    NotesText = empty ? "Tidak ada catatan" : log.Notes!.Trim(),
                    NotesEmpty = empty,
                    Log = log
                });
            }

            // postponed days that really weren't watered are shown too
            foreach (var (day, c) in recordsByDay)
            {
                if (WateringDecisionExtensions.FromDb(c.Decision) != WateringDecision.Postpone || totalByDay.ContainsKey(day))
                {
                    continue;
                }

                rows.Add(new HistoryRow
                {
                    Date = day,
                    DateText = day.ToString("dd MMM yyyy", Culture),
                    VolumeText = "0 L",
                    RecommendationText = "Tunda",
                    Evaluation = Evaluate(c, 0),
                    NotesText = "Tidak ada catatan",
                    NotesEmpty = true
                });
            }

            return rows.OrderByDescending(r => r.Date).ToList();
        }

        private static EvaluationResult Evaluate(DailyRecommendation c, double dayTotal) =>
            WateringEvaluator.Evaluate(WateringDecisionExtensions.FromDb(c.Decision), c.RecommendedVolume, dayTotal);

        private static string RecommendationLabel(DailyRecommendation? c) =>
            c == null ? "-"
            : WateringDecisionExtensions.FromDb(c.Decision) == WateringDecision.Postpone ? "Tunda"
            : FormatLiters(c.RecommendedVolume);

        private static string FormatLiters(double value) => value.ToString("#,0.##", Culture) + " L";
    }
}
