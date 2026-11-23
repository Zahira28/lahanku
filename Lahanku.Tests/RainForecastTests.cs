using Lahanku.Models;
using Xunit;

namespace Lahanku.Tests
{
    public class RainForecastTests
    {
        private static readonly string[] Hours = { "2026-09-14T13:00", "2026-09-14T14:00", "2026-09-14T15:00", "2026-09-14T16:00" };

        [Fact]
        public void Sums_rain_and_takes_highest_probability()
        {
            var p = RainForecast.FromHourly(Hours, new double?[] { 0, 0.2, 5, 3 }, new double?[] { 10, 30, 75, 60 });
            Assert.Equal(8.2, p.TotalMm, 3);
            Assert.Equal(75, p.MaxProbability);
        }

        [Fact]
        public void Start_hour_is_first_hour_with_half_mm_and_50_percent()
        {
            // 14:00 rain 0.2 mm is ignored; 15:00 rain 5 mm at 75% → starts at 15
            var p = RainForecast.FromHourly(Hours, new double?[] { 0, 0.2, 5, 3 }, new double?[] { 10, 30, 75, 60 });
            Assert.Equal(15, p.StartHour);
        }

        [Fact]
        public void No_rain_means_no_start_hour()
        {
            var p = RainForecast.FromHourly(Hours, new double?[] { 0, 0, 0, 0 }, new double?[] { 5, 5, 5, 5 });
            Assert.Equal(0, p.TotalMm);
            Assert.Null(p.StartHour);
        }

        [Fact]
        public void Null_api_values_count_as_zero()
        {
            var p = RainForecast.FromHourly(Hours, new double?[] { null, 1, null, null }, new double?[] { null, null, null, null });
            Assert.Equal(1, p.TotalMm, 3);
            Assert.Equal(0, p.MaxProbability);
        }
    }
}
