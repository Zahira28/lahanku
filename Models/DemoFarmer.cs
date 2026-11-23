using System;

namespace Lahanku.Models
{
    // what the simulated farmer did on one day
    public record DemoDay(bool AppOpened, double? WateredLiters, int Hour, int Minute);

    // A believable (not perfect) farmer for the demo account. Seeded, so the demo data is the same every run.
    public static class DemoFarmer
    {
        public static DemoDay Simulate(WateringDecision decision, double recommended, double baseline, int seed)
        {
            var rng = new Random(seed);
            bool morning = rng.NextDouble() < 0.8;
            int hour = morning ? rng.Next(6, 9) : rng.Next(16, 18);
            int minute = rng.Next(0, 12) * 5;

            // some days the app isn't opened at all
            if (rng.NextDouble() < 0.08)
            {
                return new DemoDay(false, null, hour, minute);
            }

            if (decision == WateringDecision.Postpone)
            {
                // usually trusts the rain forecast, sometimes waters anyway
                return rng.NextDouble() < 0.85
                    ? new DemoDay(true, null, hour, minute)
                    : new DemoDay(true, RoundToFive(baseline * Between(rng, 0.7, 1.0)), hour, minute);
            }

            double roll = rng.NextDouble();
            double factor = roll < 0.75 ? Between(rng, 0.9, 1.1)
                : roll < 0.9 ? Between(rng, 1.25, 1.4)
                : Between(rng, 0.7, 0.8);
            return new DemoDay(true, RoundToFive(recommended * factor), hour, minute);
        }

        private static double Between(Random rng, double min, double max) => min + rng.NextDouble() * (max - min);

        private static double RoundToFive(double liters) => Math.Max(5, Math.Round(liters / 5) * 5);
    }
}
