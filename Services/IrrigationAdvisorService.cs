using System;
using System.Globalization;
using Lahanku.Models;

namespace Lahanku.Services
{
    // Calculate the daily watering recommendation from the crop's water need (mm/day),
    // current weather, and the 24h rain forecast. Pure function, easy to test.
    public class IrrigationAdvisorService : IIrrigationAdvisorService
    {
        private const double DefaultNeedMm = 4.5;
        private const double DefaultAreaM2 = 100;
        private const double RainThresholdMm = 3.0;
        private const double ProbabilityThreshold = 60;
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        public Recommendation Calculate(Land land, Weather? weather, RainForecast? forecast, DateTime now)
        {
            double areaM2 = land.AreaHectares > 0 ? land.AreaHectares * 10000 : DefaultAreaM2;
            double needMm = land.Crop?.DailyWaterNeedMm ?? DefaultNeedMm;
            double baseline = Math.Round(needMm * areaM2, 0);
            double? forecastRain = forecast?.TotalMm;
            double? probability = forecast?.MaxProbability;

            if (weather == null)
            {
                return new Recommendation(WateringDecision.Water, baseline, baseline, areaM2,
                    "Siram dengan takaran standar",
                    "Data cuaca tidak tersedia, gunakan takaran standar.",
                    TimingAdvice(now), forecastRain, probability) { Brief = "Data cuaca tidak tersedia" };
            }

            if (weather.Rainfall >= RainThresholdMm)
            {
                return Postpone(baseline, areaM2,
                    $"Sedang turun hujan sekitar {weather.Rainfall.ToString("0.#", Culture)} mm. Kelembapan tanah mencukupi sehingga penyiraman tidak diperlukan.",
                    forecastRain, probability) with { Brief = $"Sedang hujan ±{Mm(weather.Rainfall)} mm" };
            }

            if (forecast != null && forecast.TotalMm >= RainThresholdMm && forecast.MaxProbability >= ProbabilityThreshold)
            {
                string hour = forecast.StartHour.HasValue ? $" sekitar pukul {forecast.StartHour.Value:00}.00" : string.Empty;
                return Postpone(baseline, areaM2,
                    $"Hujan sekitar {forecast.TotalMm.ToString("0.#", Culture)} mm diperkirakan turun{hour} (peluang {forecast.MaxProbability:0}%). Kelembapan tanah diperkirakan mencukupi sehingga penyiraman tidak diperlukan.",
                    forecastRain, probability) with
                {
                    Brief = forecast.StartHour.HasValue
                        ? $"Hujan ±{Mm(forecast.TotalMm)} mm pukul {forecast.StartHour.Value:00}.00"
                        : $"Hujan ±{Mm(forecast.TotalMm)} mm hari ini"
                };
            }

            double tempFactor = weather.Temperature switch
            {
                >= 33.0 => 1.30,
                >= 30.0 => 1.15,
                <= 22.0 => 0.85,
                _ => 1.0
            };

            double dryLimit = land.HasIdealMoisture ? land.IdealMoistureMin!.Value : 45.0;
            double wetLimit = land.HasIdealMoisture ? land.IdealMoistureMax!.Value : 80.0;
            double humidityFactor = weather.Humidity <= dryLimit ? 1.20
                : weather.Humidity >= wetLimit ? 0.85
                : 1.0;

            // light rain in the forecast reduces the volume, weighted by its probability
            double effectiveRain = weather.Rainfall + (forecast == null ? 0 : forecast.TotalMm * forecast.MaxProbability / 100.0);
            double netMm = Math.Max(0.5, needMm * tempFactor * humidityFactor - 0.8 * effectiveRain);
            double volume = Math.Round(netMm * areaM2, 0);

            string tempText = weather.Temperature.ToString("0.#", Culture);
            if (weather.Temperature >= 32.0 && weather.Humidity < dryLimit)
            {
                return new Recommendation(WateringDecision.WaterExtra, volume, baseline, areaM2,
                    "Tambah volume penyiraman",
                    $"Suhu {tempText}°C dan kelembapan {weather.Humidity:0}% berada di bawah batas ideal, sehingga air lebih cepat menguap.",
                    TimingAdvice(now), forecastRain, probability)
                { Brief = $"{tempText}°C, kelembapan {weather.Humidity:0}%. {ShortTiming(now)}" };
            }

            return new Recommendation(WateringDecision.Water, volume, baseline, areaM2,
                "Siram sesuai kebutuhan normal",
                $"Kondisi cuaca {weather.Condition.ToLowerInvariant()} dengan suhu {tempText}°C dan kelembapan {weather.Humidity:0}%.",
                TimingAdvice(now), forecastRain, probability)
            { Brief = $"{weather.Condition}, {tempText}°C. {ShortTiming(now)}" };
        }

        public DailyRecommendation CalculateRecommendation(Land land, Weather? weather)
        {
            var r = Calculate(land, weather, null, DateTime.Now);
            return new DailyRecommendation
            {
                LandId = land.Id,
                WeatherId = weather?.WeatherId,
                RecommendedVolume = r.RecommendedVolume,
                StatusLabel = r.Title,
                Advice = string.IsNullOrEmpty(r.TimingAdvice) ? r.Reason : $"{r.Reason} {r.TimingAdvice}",
                CreatedAt = DateTime.UtcNow
            };
        }

        private static Recommendation Postpone(double baseline, double areaM2, string reason, double? forecastRain, double? probability) =>
            new(WateringDecision.Postpone, 0, baseline, areaM2, "Penyiraman ditunda", reason, string.Empty, forecastRain, probability);

        private static string ShortTiming(DateTime now) =>
            now.Hour < 9 ? "Sebelum pukul 09.00" : "Setelah pukul 16.00";

        // brief uses Indonesian decimals (20,4) since it's shown as-is on the cards
        private static string Mm(double value) => value.ToString("0.#", BriefCulture);

        private static readonly CultureInfo BriefCulture = CultureInfo.GetCultureInfo("id-ID");

        private static string TimingAdvice(DateTime now) =>
            now.Hour < 9 ? "Lakukan sebelum pukul 09.00." : "Waktu terbaik setelah pukul 16.00.";
    }
}
