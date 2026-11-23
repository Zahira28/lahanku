using Lahanku.Models;
using Xunit;

namespace Lahanku.Tests
{
    public class DemoFarmerTests
    {
        private static IEnumerable<DemoDay> Many(WateringDecision decision, double recommended, double baseline) =>
            Enumerable.Range(0, 400).Select(seed => DemoFarmer.Simulate(decision, recommended, baseline, seed));

        [Fact]
        public void Same_seed_gives_the_same_day()
        {
            var a = DemoFarmer.Simulate(WateringDecision.Water, 300, 300, 42);
            var b = DemoFarmer.Simulate(WateringDecision.Water, 300, 300, 42);
            Assert.Equal(a, b);
        }

        [Fact]
        public void Skipped_days_have_no_watering()
        {
            Assert.All(Many(WateringDecision.Water, 300, 300).Where(d => !d.AppOpened), d => Assert.Null(d.WateredLiters));
        }

        [Fact]
        public void Most_postponements_are_followed_but_not_all()
        {
            var opened = Many(WateringDecision.Postpone, 0, 300).Where(d => d.AppOpened).ToList();
            double followed = opened.Count(d => d.WateredLiters == null) / (double)opened.Count;
            Assert.InRange(followed, 0.75, 0.95);
        }

        [Fact]
        public void Most_waterings_are_close_to_the_recommendation()
        {
            var watered = Many(WateringDecision.Water, 300, 300).Where(d => d.WateredLiters.HasValue).ToList();
            double close = watered.Count(d => d.WateredLiters >= 240 && d.WateredLiters <= 360) / (double)watered.Count;
            Assert.InRange(close, 0.65, 0.9);
            Assert.Contains(watered, d => d.WateredLiters > 360);
        }

        [Fact]
        public void Volumes_are_rounded_to_five_liters_and_positive()
        {
            Assert.All(Many(WateringDecision.WaterExtra, 137, 120).Where(d => d.WateredLiters.HasValue), d =>
            {
                Assert.True(d.WateredLiters >= 5);
                Assert.Equal(0, d.WateredLiters!.Value % 5);
            });
        }

        [Fact]
        public void Watering_happens_in_the_morning_or_late_afternoon()
        {
            Assert.All(Many(WateringDecision.Water, 300, 300), d => Assert.True(d.Hour is >= 6 and <= 8 or >= 16 and <= 17));
        }
    }
}
