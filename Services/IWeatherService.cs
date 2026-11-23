using System.Threading.Tasks;
using Lahanku.Models;

namespace Lahanku.Services
{
    /// <summary>
    /// Abstraction for the satellite weather data service (DIP - Dependency Inversion Principle).
    /// </summary>
    public interface IWeatherService
    {
        /// <summary>
        /// Gets real-time weather data from Open-Meteo for the given coordinates.
        /// </summary>
        Task<Weather?> GetCurrentWeatherAsync(double latitude, double longitude, long? landId = null);

        // total rain for the next 24 hours, null if the request fails
        Task<RainForecast?> GetRainForecastAsync(double latitude, double longitude);

        /// <summary>
        /// Translates WMO codes into an Indonesian description and a weather icon key (see Styles/WeatherIcons.xaml).
        /// </summary>
        (string Condition, string IconKey) GetWmoDescription(int weatherCode);
    }
}
