using Lahanku.Models;
using Xunit;

namespace Lahanku.Tests
{
    public class WaterSummaryTests
    {
        private static readonly DateTime Monday = new(2026, 11, 16);
        private static readonly DateTime Tuesday = new(2026, 11, 17);

        private static DailyRecommendation MakeRecord(long land, DateTime date, double baseline, string decision = "siram") =>
            new() { LandId = land, Date = date, BaselineVolume = baseline, Decision = decision };

        private static IrrigationLog Log(long land, DateTime date, double liters) =>
            new() { LandId = land, Date = date.AddHours(7), VolumeLiters = liters };

        [Fact]
        public void Days_without_record_are_ignored()
        {
            var r = WaterSummary.Calculate(
                new[] { MakeRecord(1, Monday, 480) },
                new[] { Log(1, Monday, 460), Log(1, Tuesday, 500) });

            Assert.Equal(480, r.Baseline);
            Assert.Equal(460, r.Used);
            Assert.Equal(20, r.Saved);
        }

        [Fact]
        public void Postponed_day_without_watering_saves_everything()
        {
            var r = WaterSummary.Calculate(new[] { MakeRecord(1, Monday, 480, "tunda") }, Array.Empty<IrrigationLog>());
            Assert.Equal(480, r.Saved);
            Assert.Equal(100, r.SavedPercent, 3);
        }

        [Fact]
        public void Logs_are_matched_by_land_and_date()
        {
            // land 2 has no record that day → its log isn't counted
            var r = WaterSummary.Calculate(
                new[] { MakeRecord(1, Monday, 480) },
                new[] { Log(1, Monday, 400), Log(2, Monday, 300) });

            Assert.Equal(400, r.Used);
        }

        [Fact]
        public void Multiple_waterings_per_day_are_summed()
        {
            var r = WaterSummary.Calculate(
                new[] { MakeRecord(1, Monday, 480) },
                new[] { Log(1, Monday, 200), Log(1, Monday, 300) });

            Assert.Equal(500, r.Used);
            Assert.Equal(-20, r.Saved);
        }

        [Fact]
        public void No_data_means_zero_percent()
        {
            var r = WaterSummary.Calculate(Array.Empty<DailyRecommendation>(), Array.Empty<IrrigationLog>());
            Assert.Equal(0, r.SavedPercent);
            Assert.False(r.HasData);
        }
    }
}
