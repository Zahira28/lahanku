using System;
using System.Collections.Generic;
using System.Linq;

namespace Lahanku.Models
{
    // one day on the impact chart (all lands summed)
    public record DailyPoint(DateTime Date, double Used, double Baseline, bool IsPostponed);

    public record LandImpact(Land Land, double Baseline, double Used, int OnTargetCount, int RecordCount)
    {
        public double Saved => Baseline - Used;
        public double OnTargetPercent => RecordCount > 0 ? OnTargetCount * 100.0 / RecordCount : 0;
    }

    public record ImpactReport(
        double Baseline,
        double Used,
        int PostponedDays,
        double PostponedSaved,
        double AdjustmentSaved,
        int OnTargetCount,
        int RecordCount,
        IReadOnlyList<DailyPoint> Daily,
        IReadOnlyList<LandImpact> PerLand)
    {
        public double Saved => Baseline - Used;
        public double SavedPercent => Baseline > 0 ? Saved / Baseline * 100 : 0;
        public double OnTargetPercent => RecordCount > 0 ? OnTargetCount * 100.0 / RecordCount : 0;
        public bool HasData => RecordCount > 0;

        // average routine watering per recorded day, used for the chart's reference line
        public double AverageDailyBaseline => Daily.Count > 0 ? Daily.Average(d => d.Baseline) : 0;
    }

    // Saved water = baseline − used, only on land-days that have a recommendation record.
    public static class ImpactCalculator
    {
        public static ImpactReport Calculate(IEnumerable<Land> lands, IEnumerable<DailyRecommendation> records, IEnumerable<IrrigationLog> logs)
        {
            var usedByLandDay = logs
                .GroupBy(l => (l.LandId, l.Date.Date))
                .ToDictionary(g => g.Key, g => g.Sum(l => l.VolumeLiters));

            // one row per land-day (the table has a unique constraint, but be safe)
            var rows = records
                .GroupBy(r => (r.LandId, r.Date.Date))
                .Select(g => g.First())
                .Select(r =>
                {
                    double used = usedByLandDay.TryGetValue((r.LandId, r.Date.Date), out var u) ? u : 0;
                    var decision = WateringDecisionExtensions.FromDb(r.Decision);
                    bool onTarget = WateringEvaluator.Evaluate(decision, r.RecommendedVolume, used).MatchesRecommendation;
                    return (Record: r, Used: used, Decision: decision, OnTarget: onTarget);
                })
                .ToList();

            var postponed = rows.Where(x => x.Decision == WateringDecision.Postpone).ToList();
            var others = rows.Where(x => x.Decision != WateringDecision.Postpone).ToList();

            var daily = rows
                .GroupBy(x => x.Record.Date.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DailyPoint(
                    g.Key,
                    g.Sum(x => x.Used),
                    g.Sum(x => x.Record.BaselineVolume),
                    g.All(x => x.Decision == WateringDecision.Postpone) && g.Sum(x => x.Used) <= 0))
                .ToList();

            var landById = lands.ToDictionary(l => l.Id);
            var perLand = rows
                .GroupBy(x => x.Record.LandId)
                .Where(g => landById.ContainsKey(g.Key))
                .Select(g => new LandImpact(
                    landById[g.Key],
                    g.Sum(x => x.Record.BaselineVolume),
                    g.Sum(x => x.Used),
                    g.Count(x => x.OnTarget),
                    g.Count()))
                .OrderBy(l => l.Land.Name)
                .ToList();

            return new ImpactReport(
                Baseline: rows.Sum(x => x.Record.BaselineVolume),
                Used: rows.Sum(x => x.Used),
                PostponedDays: postponed.Select(x => x.Record.Date.Date).Distinct().Count(),
                PostponedSaved: postponed.Sum(x => x.Record.BaselineVolume - x.Used),
                AdjustmentSaved: others.Sum(x => x.Record.BaselineVolume - x.Used),
                OnTargetCount: rows.Count(x => x.OnTarget),
                RecordCount: rows.Count,
                Daily: daily,
                PerLand: perLand);
        }
    }
}
