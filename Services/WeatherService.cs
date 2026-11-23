using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Lahanku.Helpers;
using Lahanku.Models;
using static Supabase.Postgrest.Constants;

namespace Lahanku.Services
{
    /// <summary>
    /// Real-time satellite weather service using the Open-Meteo API (SRP & DIP).
    /// </summary>
    public class WeatherService : IWeatherService
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public async Task<Weather?> GetCurrentWeatherAsync(double latitude, double longitude, long? landId = null)
        {
            try
            {
                var latStr = latitude.ToString(CultureInfo.InvariantCulture);
                var lonStr = longitude.ToString(CultureInfo.InvariantCulture);
                var url = $"https://api.open-meteo.com/v1/forecast?latitude={latStr}&longitude={lonStr}&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,rain,weather_code,wind_speed_10m&timezone=auto";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var meteoData = JsonSerializer.Deserialize<OpenMeteoResponse>(json);

                if (meteoData?.Current == null)
                {
                    return null;
                }

                var current = meteoData.Current;
                var (kondisi, _) = GetWmoDescription(current.WeatherCode);

                var weather = new Weather
                {
                    LandId = landId,
                    Temperature = current.Temperature2m,
                    Humidity = current.RelativeHumidity2m,
                    Rainfall = current.Precipitation,
                    Condition = kondisi,
                    WeatherCode = current.WeatherCode,
                    CheckedAt = DateTime.UtcNow
                };

                _ = SaveSnapshotToDatabaseAsync(weather);

                return weather;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WeatherService] Error fetching weather: {ex.Message}");
                return null;
            }
        }

        public async Task<RainForecast?> GetRainForecastAsync(double latitude, double longitude)
        {
            try
            {
                var lat = latitude.ToString(CultureInfo.InvariantCulture);
                var lon = longitude.ToString(CultureInfo.InvariantCulture);
                var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&hourly=precipitation,precipitation_probability&forecast_hours=24&timezone=auto";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var data = JsonSerializer.Deserialize<OpenMeteoResponse>(await response.Content.ReadAsStringAsync());
                var hourly = data?.Hourly;
                if (hourly == null || hourly.Time.Count == 0)
                {
                    return null;
                }

                return RainForecast.FromHourly(hourly.Time, hourly.Precipitation, hourly.PrecipitationProbability);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WeatherService] Gagal ambil prakiraan: {ex.Message}");
                return null;
            }
        }

        // save at most 1 snapshot per land per hour, so the weather table doesn't bloat
        private static readonly TimeSpan SnapshotInterval = TimeSpan.FromHours(1);
        private static readonly ConcurrentDictionary<long, DateTime> _lastSnapshotUtc = new();

        private async Task SaveSnapshotToDatabaseAsync(Weather weather)
        {
            if (weather.LandId is not long landId)
            {
                return;
            }

            if (_lastSnapshotUtc.TryGetValue(landId, out var cached) && DateTime.UtcNow - cached < SnapshotInterval)
            {
                return;
            }

            try
            {
                var client = await SupabaseConfig.GetClientAsync();

                // check the DB too, because the cache above is lost every time the app closes
                var latest = await client
                    .From<Weather>()
                    .Where(c => c.LandId == landId)
                    .Order(c => c.CheckedAt, Ordering.Descending)
                    .Limit(1)
                    .Get();

                var lastSaved = latest.Models.FirstOrDefault()?.CheckedAt.ToUniversalTime();
                if (lastSaved.HasValue && DateTime.UtcNow - lastSaved.Value < SnapshotInterval)
                {
                    _lastSnapshotUtc[landId] = lastSaved.Value;
                    return;
                }

                await client.From<Weather>().Insert(weather);
                _lastSnapshotUtc[landId] = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                // failing to save the history is fine, the weather still shows
                System.Diagnostics.Debug.WriteLine($"[WeatherService] Gagal simpan snapshot: {ex.Message}");
            }
        }

        public (string Condition, string IconKey) GetWmoDescription(int weatherCode) => weatherCode switch
        {
            0 => ("Cerah", "sun"),
            1 => ("Sebagian Cerah", "partly"),
            2 => ("Sebagian Berawan", "partly"),
            3 => ("Mendung / Berawan", "cloudy"),
            45 or 48 => ("Berkabut", "fog"),
            51 or 53 or 55 => ("Gerimis", "drizzle"),
            56 or 57 => ("Gerimis Dingin", "drizzle"),
            61 => ("Hujan Ringan", "rain"),
            63 => ("Hujan Sedang", "rain"),
            65 => ("Hujan Lebat", "rain"),
            80 or 81 => ("Hujan Lokal", "rain"),
            82 => ("Hujan Sangat Lebat", "storm"),
            95 or 96 or 99 => ("Badai Petir", "storm"),
            _ => ("Cerah Berawan", "partly")
        };
    }
}
