using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Lahanku.Helpers;
using Lahanku.Models;

namespace Lahanku.Services
{
    /// <summary>
    /// Layanan penarik data satelit cuaca real-time Open-Meteo API (SRP & DIP).
    /// </summary>
    public class WeatherService : IWeatherService
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public async Task<Cuaca?> GetCurrentWeatherAsync(double latitude, double longitude, long? landId = null)
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

                var cuaca = new Cuaca
                {
                    IdLahan = landId,
                    Suhu = current.Temperature2m,
                    Kelembapan = current.RelativeHumidity2m,
                    CurahHujan = current.Precipitation,
                    KondisiCuaca = kondisi,
                    WaktuCek = DateTime.UtcNow
                };

                // Sinkronkan snapshot cuaca ke Supabase di latar belakang jika terhubung
                _ = SaveSnapshotToDatabaseAsync(cuaca);

                return cuaca;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WeatherService] Error fetching weather: {ex.Message}");
                return null;
            }
        }

        private async Task SaveSnapshotToDatabaseAsync(Cuaca cuaca)
        {
            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                await client.From<Cuaca>().Insert(cuaca);
            }
            catch
            {
                // Pengambilan cuaca tetap berhasil meskipun simpan snapshot riwayat offline
            }
        }

        public (string Condition, string IconEmoji) GetWmoDescription(int weatherCode) => weatherCode switch
        {
            0 => ("Cerah", "☀️"),
            1 => ("Sebagian Cerah", "🌤️"),
            2 => ("Sebagian Berawan", "⛅"),
            3 => ("Mendung / Berawan", "☁️"),
            45 or 48 => ("Berkabut", "🌫️"),
            51 or 53 or 55 => ("Gerimis", "🌦️"),
            56 or 57 => ("Gerimis Dingin", "🌦️"),
            61 => ("Hujan Ringan", "🌧️"),
            63 => ("Hujan Sedang", "🌧️"),
            65 => ("Hujan Lebat", "🌧️"),
            80 or 81 => ("Hujan Lokal", "🌦️"),
            82 => ("Hujan Sangat Lebat", "⛈️"),
            95 or 96 or 99 => ("Badai Petir", "⚡"),
            _ => ("Cerah Berawan", "⛅")
        };
    }
}
