using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lahanku.Models
{
    public enum WateringDecision
    {
        Water,
        Postpone,
        WaterExtra
    }

    public static class WateringDecisionExtensions
    {
        public static string ToDb(this WateringDecision decision) => decision switch
        {
            WateringDecision.Postpone => "tunda",
            WateringDecision.WaterExtra => "siram_ekstra",
            _ => "siram"
        };

        public static WateringDecision FromDb(string? value) => value switch
        {
            "tunda" => WateringDecision.Postpone,
            "siram_ekstra" => WateringDecision.WaterExtra,
            _ => WateringDecision.Water
        };
    }

    // summary of the rain forecast for the next 24 hours
    public record RainForecast(double TotalMm, double MaxProbability, int? StartHour)
    {
        // summarize Open-Meteo hourly data (null counts as 0)
        public static RainForecast FromHourly(IReadOnlyList<string> times, IReadOnlyList<double?> rainMm, IReadOnlyList<double?> probability)
        {
            double total = 0, maxProbability = 0;
            int? startHour = null;
            int n = Math.Min(times.Count, Math.Min(rainMm.Count, probability.Count));

            for (int i = 0; i < n; i++)
            {
                double mm = rainMm[i] ?? 0;
                double p = probability[i] ?? 0;
                total += mm;
                maxProbability = Math.Max(maxProbability, p);

                if (startHour == null && mm >= 0.5 && p >= 50 &&
                    DateTime.TryParse(times[i], CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
                {
                    startHour = t.Hour;
                }
            }

            return new RainForecast(Math.Round(total, 1), maxProbability, startHour);
        }
    }

    public record Recommendation(
        WateringDecision Decision,
        double RecommendedVolume,
        double BaselineVolume,
        double AreaM2,
        string Title,
        string Reason,
        string TimingAdvice,
        double? ForecastRainMm,
        double? RainProbability)
    {
        // 1 mm of water over 1 m² = 1 liter
        public double RecommendedPerM2 => AreaM2 > 0 ? RecommendedVolume / AreaM2 : 0;

        // one-liner for compact cards, e.g. "Hujan ±20,4 mm pukul 12.00"
        public string Brief { get; init; } = string.Empty;
    }
}
