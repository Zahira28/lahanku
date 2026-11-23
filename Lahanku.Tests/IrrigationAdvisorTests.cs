using Lahanku.Models;
using Lahanku.Services;
using Xunit;

namespace Lahanku.Tests
{
    public class IrrigationAdvisorTests
    {
        private readonly IrrigationAdvisorService _advisor = new();
        private static readonly DateTime Morning = new(2026, 9, 14, 7, 0, 0);
        private static readonly DateTime Afternoon = new(2026, 9, 14, 13, 0, 0);

        // 100 m², no crop → default need 4.5 mm → baseline 450 L
        private static Land MakeLand(double? min = null, double? max = null) =>
            new() { Name = "Tes", AreaHectares = 0.01, MinMoisture = min, MaxMoisture = max };

        private static Weather MakeWeather(double temp = 27, double rh = 60, double rain = 0) =>
            new() { Temperature = temp, Humidity = rh, Rainfall = rain, Condition = "Cerah" };

        [Fact]
        public void Offline_uses_baseline_volume()
        {
            var r = _advisor.Calculate(MakeLand(), null, null, Morning);
            Assert.Equal(WateringDecision.Water, r.Decision);
            Assert.Equal(450, r.RecommendedVolume);
            Assert.Equal(450, r.BaselineVolume);
            Assert.Contains("tidak tersedia", r.Reason);
        }

        [Fact]
        public void Current_rain_3mm_or_more_postpones()
        {
            var r = _advisor.Calculate(MakeLand(), MakeWeather(rain: 5), null, Morning);
            Assert.Equal(WateringDecision.Postpone, r.Decision);
            Assert.Equal(0, r.RecommendedVolume);
            Assert.Equal(450, r.BaselineVolume);
            Assert.Equal(string.Empty, r.TimingAdvice);
        }

        [Fact]
        public void Heavy_forecast_rain_with_high_probability_postpones()
        {
            var r = _advisor.Calculate(MakeLand(), MakeWeather(), new RainForecast(8, 75, 15), Morning);
            Assert.Equal(WateringDecision.Postpone, r.Decision);
            Assert.Contains("pukul 15.00", r.Reason);
            Assert.Contains("75%", r.Reason);
            Assert.Equal(8, r.ForecastRainMm);
            Assert.Equal(75, r.RainProbability);
        }

        [Fact]
        public void Low_probability_forecast_reduces_volume_without_postponing()
        {
            // 4,5 − 0,8 × (8 × 0,4) = 1,94 mm → 194 L
            var r = _advisor.Calculate(MakeLand(), MakeWeather(), new RainForecast(8, 40, null), Morning);
            Assert.Equal(WateringDecision.Water, r.Decision);
            Assert.Equal(194, r.RecommendedVolume);
        }

        [Fact]
        public void Hot_and_dry_means_extra_watering()
        {
            // 4,5 × 1,3 × 1,2 = 7,02 mm → 702 L
            var r = _advisor.Calculate(MakeLand(), MakeWeather(temp: 34, rh: 40), null, Morning);
            Assert.Equal(WateringDecision.WaterExtra, r.Decision);
            Assert.Equal(702, r.RecommendedVolume);
            Assert.Equal("Tambah volume penyiraman", r.Title);
        }

        [Fact]
        public void Dry_limit_follows_land_ideal_range()
        {
            // RH 55 is still above the default 45, but below the ideal minimum of 60
            var r = _advisor.Calculate(MakeLand(60, 80), MakeWeather(temp: 33, rh: 55), null, Morning);
            Assert.Equal(WateringDecision.WaterExtra, r.Decision);
        }

        [Fact]
        public void Volume_never_below_half_mm()
        {
            // 4.5 − 0.8 × (2.9 + 2.9) < 0.5 → rounded up to 0.5 mm = 50 L
            var r = _advisor.Calculate(MakeLand(), MakeWeather(rain: 2.9), new RainForecast(2.9, 100, null), Morning);
            Assert.Equal(WateringDecision.Water, r.Decision);
            Assert.Equal(50, r.RecommendedVolume);
        }

        [Fact]
        public void Timing_advice_follows_hour()
        {
            Assert.Contains("09.00", _advisor.Calculate(MakeLand(), MakeWeather(), null, Morning).TimingAdvice);
            Assert.Contains("16.00", _advisor.Calculate(MakeLand(), MakeWeather(), null, Afternoon).TimingAdvice);
        }

        [Fact]
        public void Recommended_per_m2_equals_mm()
        {
            var r = _advisor.Calculate(MakeLand(), MakeWeather(), null, Morning);
            Assert.Equal(4.5, r.RecommendedPerM2, 3);
        }

        [Fact]
        public void Brief_is_a_short_one_liner_per_decision()
        {
            Assert.Equal("Hujan ±8 mm pukul 15.00",
                _advisor.Calculate(MakeLand(), MakeWeather(), new RainForecast(8, 75, 15), Morning).Brief);
            Assert.Equal("Sedang hujan ±5 mm",
                _advisor.Calculate(MakeLand(), MakeWeather(rain: 5), null, Morning).Brief);
            Assert.Equal("34°C, kelembapan 40%. Sebelum pukul 09.00",
                _advisor.Calculate(MakeLand(), MakeWeather(temp: 34, rh: 40), null, Morning).Brief);
            Assert.Equal("Cerah, 27°C. Setelah pukul 16.00",
                _advisor.Calculate(MakeLand(), MakeWeather(), null, Afternoon).Brief);
            Assert.Equal("Data cuaca tidak tersedia",
                _advisor.Calculate(MakeLand(), null, null, Morning).Brief);
        }

        [Theory]
        [InlineData(WateringDecision.Water, "siram")]
        [InlineData(WateringDecision.Postpone, "tunda")]
        [InlineData(WateringDecision.WaterExtra, "siram_ekstra")]
        public void Decision_round_trips_to_db_text(WateringDecision k, string db)
        {
            Assert.Equal(db, k.ToDb());
            Assert.Equal(k, WateringDecisionExtensions.FromDb(db));
        }
    }
}
