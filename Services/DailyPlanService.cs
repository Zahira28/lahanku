using System;
using System.Threading.Tasks;
using Lahanku.Models;

namespace Lahanku.Services
{
    // single entry point for daily recommendations, used by Dashboard and Land Detail
    public class DailyPlanService : IDailyPlanService
    {
        private readonly IWeatherService _weather;
        private readonly IIrrigationAdvisorService _advisor;
        private readonly IDailyRecommendationService _recommendations;

        public DailyPlanService(IWeatherService weather, IIrrigationAdvisorService advisor, IDailyRecommendationService recommendation)
        {
            _weather = weather;
            _advisor = advisor;
            _recommendations = recommendation;
        }

        public async Task<LandPlan> CalculateAsync(Land land, DateTime now)
        {
            // both weather requests run in parallel
            var weatherTask = _weather.GetCurrentWeatherAsync(land.Latitude, land.Longitude, land.Id);
            var forecastTask = _weather.GetRainForecastAsync(land.Latitude, land.Longitude);
            await Task.WhenAll(weatherTask, forecastTask);

            var recommendation = _advisor.Calculate(land, weatherTask.Result, forecastTask.Result, now);
            await _recommendations.SaveForDateAsync(land, recommendation, now);

            return new LandPlan(land, weatherTask.Result, recommendation);
        }
    }
}
