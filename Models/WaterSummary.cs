using System.Collections.Generic;
using System.Linq;

namespace Lahanku.Models
{
    // total water vs routine watering. Only days with a recommendation record are counted.
    public record WaterSummary(double Baseline, double Used)
    {
        public double Saved => Baseline - Used;
        public double SavedPercent => Baseline > 0 ? Saved / Baseline * 100 : 0;
        public bool HasData => Baseline > 0;

        public static WaterSummary Calculate(IEnumerable<DailyRecommendation> records, IEnumerable<IrrigationLog> logs)
        {
            var list = records.ToList();
            var recordedDays = list.Select(c => (c.LandId, c.Date.Date)).ToHashSet();

            double baseline = list.Sum(c => c.BaselineVolume);
            double dipakai = logs
                .Where(l => recordedDays.Contains((l.LandId, l.Date.Date)))
                .Sum(l => l.VolumeLiters);

            return new WaterSummary(baseline, dipakai);
        }
    }
}
