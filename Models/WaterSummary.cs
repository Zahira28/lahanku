using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Lahanku.Models
{
    // total water vs routine watering. Only days with a recommendation record are counted.
    public record WaterSummary(double Baseline, double Used)
    {
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("id-ID");

        public double Saved => Baseline - Used;
        public double SavedPercent => Baseline > 0 ? Saved / Baseline * 100 : 0;
        public bool HasData => Baseline > 0;

        public static WaterSummary Calculate(IEnumerable<DailyRecommendation> records, IEnumerable<IrrigationLog> logs)
        {
            var list = records.ToList();
            var recordedDays = list.Select(c => (c.LandId, c.Date.Date)).ToHashSet();

            double baseline = list.Sum(c => c.BaselineVolume);
            double used = logs
                .Where(l => recordedDays.Contains((l.LandId, l.Date.Date)))
                .Sum(l => l.VolumeLiters);

            return new WaterSummary(baseline, used);
        }

        // one-line summary under the Dashboard plan cards
        public string ToWeeklySentence()
        {
            if (!HasData)
            {
                return string.Empty;
            }

            string start = $"Pemakaian air minggu ini {Liters(Used)}";
            double saved = Math.Round(Saved, 0);

            if (saved > 0)
            {
                return $"{start}, lebih hemat {Liters(saved)} ({SavedPercent.ToString("0", Culture)}%) dibandingkan penyiraman rutin.";
            }

            if (saved < 0)
            {
                return $"{start}, {Liters(-saved)} lebih banyak dibandingkan penyiraman rutin.";
            }

            return $"{start}, sama dengan penyiraman rutin.";
        }

        private static string Liters(double value) => value.ToString("#,0", Culture) + " L";
    }
}
