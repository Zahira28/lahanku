using System;
using Lahanku.Models;

namespace Lahanku.Services
{
    public interface IIrrigationAdvisorService
    {
        // today's recommendation from current weather + 24h forecast
        Recommendation Calculate(Land land, Weather? weather, RainForecast? forecast, DateTime now);

        // old version, kept until the UI moves to Calculate
        DailyRecommendation CalculateRecommendation(Land land, Weather? weather);
    }
}
