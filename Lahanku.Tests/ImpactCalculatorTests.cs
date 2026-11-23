using Lahanku.Models;
using Xunit;

namespace Lahanku.Tests
{
    public class ImpactCalculatorTests
    {
        private static readonly DateTime Day1 = new(2026, 11, 16);
        private static readonly DateTime Day2 = new(2026, 11, 17);

        private static readonly Land Land1 = new() { Id = 1, Name = "Brokoli" };
        private static readonly Land Land2 = new() { Id = 2, Name = "Cabai" };

        private static DailyRecommendation Record(long land, DateTime date, string decision, double recommended, double baseline) =>
            new() { LandId = land, Date = date, Decision = decision, RecommendedVolume = recommended, BaselineVolume = baseline };

        private static IrrigationLog Log(long land, DateTime date, double liters) =>
            new() { LandId = land, Date = date.AddHours(7), VolumeLiters = liters };

        // land1: day1 on target, day2 postponed (no watering); land2: day1 over
        private static ImpactReport Sample() => ImpactCalculator.Calculate(
            new[] { Land1, Land2 },
            new[]
            {
                Record(1, Day1, "siram", 450, 480),
                Record(1, Day2, "tunda", 0, 480),
                Record(2, Day1, "siram", 100, 100),
            },
            new[] { Log(1, Day1, 460), Log(2, Day1, 200) });

        [Fact]
        public void Totals_compare_used_with_baseline()
        {
            var r = Sample();
            Assert.Equal(1060, r.Baseline);
            Assert.Equal(660, r.Used);
            Assert.Equal(400, r.Saved);
        }

        [Fact]
        public void Savings_are_split_into_postponed_and_adjustment()
        {
            var r = Sample();
            Assert.Equal(1, r.PostponedDays);
            Assert.Equal(480, r.PostponedSaved);
            Assert.Equal(-80, r.AdjustmentSaved); // (480 − 460) + (100 − 200)
            Assert.Equal(r.Saved, r.PostponedSaved + r.AdjustmentSaved);
        }

        [Fact]
        public void Watering_on_a_postponed_day_is_not_counted_as_postponed_savings()
        {
            // brokoli followed the postponement, teknik watered anyway
            var r = ImpactCalculator.Calculate(
                new[] { Land1, Land2 },
                new[] { Record(1, Day1, "tunda", 0, 96), Record(2, Day1, "tunda", 0, 40) },
                new[] { Log(2, Day1, 250) });
            Assert.Equal(1, r.PostponedDays);
            Assert.Equal(96, r.PostponedSaved);
            Assert.Equal(-210, r.AdjustmentSaved);
            Assert.Equal(-114, r.Saved);
        }

        [Fact]
        public void On_target_counts_postponed_and_matching_days()
        {
            var r = Sample();
            Assert.Equal(2, r.OnTargetCount);
            Assert.Equal(3, r.RecordCount);
        }

        [Fact]
        public void Logs_on_days_without_record_are_ignored()
        {
            var r = ImpactCalculator.Calculate(
                new[] { Land1 },
                new[] { Record(1, Day1, "siram", 450, 480) },
                new[] { Log(1, Day1, 400), Log(1, Day2, 999) });
            Assert.Equal(400, r.Used);
        }

        [Fact]
        public void Daily_points_are_sorted_and_flag_postponed_days()
        {
            var r = Sample();
            Assert.Equal(2, r.Daily.Count);
            Assert.Equal(Day1, r.Daily[0].Date);
            Assert.Equal(580, r.Daily[0].Baseline);
            Assert.Equal(660, r.Daily[0].Used);
            Assert.False(r.Daily[0].IsPostponed);
            Assert.True(r.Daily[1].IsPostponed);
            Assert.Equal(0, r.Daily[1].Used);
        }

        [Fact]
        public void Per_land_rows_have_their_own_totals()
        {
            var r = Sample();
            var brokoli = r.PerLand.Single(p => p.Land.Id == 1);
            var cabai = r.PerLand.Single(p => p.Land.Id == 2);
            Assert.Equal(500, brokoli.Saved);
            Assert.Equal(100, brokoli.OnTargetPercent);
            Assert.Equal(-100, cabai.Saved);
            Assert.Equal(0, cabai.OnTargetPercent);
        }

        [Fact]
        public void Empty_input_gives_empty_report()
        {
            var r = ImpactCalculator.Calculate(Array.Empty<Land>(), Array.Empty<DailyRecommendation>(), Array.Empty<IrrigationLog>());
            Assert.False(r.HasData);
            Assert.Empty(r.Daily);
            Assert.Equal(0, r.SavedPercent);
        }
    }
}
