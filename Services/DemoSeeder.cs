using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Lahanku.Helpers;
using Lahanku.Models;

namespace Lahanku.Services
{
    // Fills the shared demo account with ~30 days of history:
    // real past weather from Open-Meteo → the same recommendation logic as the app → a simulated farmer.
    // Run with: dotnet run -- --seed-demo   (wipes and rebuilds the demo account's lands every time)
    public class DemoSeeder
    {
        private const int Days = 30;

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

        // crop keywords: most specific first, so "Cabai Rawit" doesn't end up as "Cabai Merah Besar"
        private record DemoLand(string Name, string Location, double Lat, double Lon, double AreaHa, params string[] CropKeywords);

        private static readonly DemoLand[] DemoLands =
        {
            new("Kebun Cabai Rawit", "Pekarangan belakang, Sleman", -7.7160, 110.3550, 0.006, "cabai rawit", "cabai"),
            new("Bedeng Sawi", "Halaman samping, Kota Yogyakarta", -7.7970, 110.3700, 0.0025, "sawi hijau", "sawi"),
            new("Pot Tomat", "Teras rumah, Bantul", -7.8880, 110.3300, 0.0008, "tomat"),
        };

        private readonly AuthService _auth = new();
        private readonly LandService _lands = new();
        private readonly WeatherService _weather = new();
        private readonly IrrigationAdvisorService _advisor = new();
        private readonly Action<string> _log;

        public DemoSeeder(Action<string> log)
        {
            _log = log;
        }

        public async Task<bool> RunAsync()
        {
            var user = await SignInAsync();
            if (user == null)
            {
                return false;
            }

            var client = await SupabaseConfig.GetClientAsync();
            var userId = user.Id;
            await client.From<User>().Where(u => u.Id == userId).Set(u => u.FullName!, DemoAccount.FullName).Update();

            await WipeAsync(user.Id);
            var crops = await _lands.GetCropsAsync();

            int landIndex = 0;
            foreach (var demo in DemoLands)
            {
                landIndex++;
                var crop = demo.CropKeywords
                    .Select(k => crops.FirstOrDefault(c => c.DisplayName.Contains(k, StringComparison.OrdinalIgnoreCase)))
                    .FirstOrDefault(c => c != null);
                var land = await _lands.AddLandAsync(new Land
                {
                    OwnerId = user.Id,
                    Name = demo.Name,
                    LocationDescription = demo.Location,
                    Latitude = demo.Lat,
                    Longitude = demo.Lon,
                    AreaHectares = demo.AreaHa,
                    CropId = crop?.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-Days - 1),
                });
                land.Crop = crop;
                _log($"Lahan \"{land.Name}\" dibuat{(crop == null ? " (tanpa tanaman, katalog tidak cocok)" : $" ({crop.Name})")}.");

                var history = await GetPastWeatherAsync(demo.Lat, demo.Lon);
                var records = new List<DailyRecommendation>();
                var logs = new List<IrrigationLog>();

                foreach (var day in history)
                {
                    var weather = new Weather
                    {
                        Temperature = day.TempMax,
                        Humidity = day.Humidity,
                        Rainfall = 0,
                        Condition = _weather.GetWmoDescription(day.Code).Condition,
                        WeatherCode = day.Code,
                    };
                    var forecast = new RainForecast(day.RainMm, day.Probability, day.RainMm >= 0.5 ? 13 : null);
                    var rec = _advisor.Calculate(land, weather, forecast, day.Date.AddHours(6));

                    int seed = landIndex * 1000 + day.Date.DayOfYear;
                    var done = DemoFarmer.Simulate(rec.Decision, rec.RecommendedVolume, rec.BaselineVolume, seed);
                    if (!done.AppOpened)
                    {
                        continue;
                    }

                    var record = new DailyRecommendation { LandId = land.Id, Date = day.Date, CreatedAt = DateTime.UtcNow };
                    DailyRecommendationService.Fill(record, rec);
                    records.Add(record);

                    if (done.WateredLiters.HasValue)
                    {
                        logs.Add(new IrrigationLog
                        {
                            LandId = land.Id,
                            Date = LocalTime.Ensure(day.Date.AddHours(done.Hour).AddMinutes(done.Minute)),
                            VolumeLiters = done.WateredLiters.Value,
                            Notes = Note(rec.Decision, done.WateredLiters.Value, rec.RecommendedVolume, seed),
                        });
                    }
                }

                if (records.Count > 0)
                {
                    await client.From<DailyRecommendation>().Insert(records);
                }
                if (logs.Count > 0)
                {
                    await client.From<IrrigationLog>().Insert(logs);
                }
                _log($"  {records.Count} catatan harian, {logs.Count} penyiraman.");
            }

            _log($"Selesai. Masuk dengan username \"{DemoAccount.Username}\" dan password \"{DemoAccount.Password}\".");
            return true;
        }

        private async Task<User?> SignInAsync()
        {
            var (user, error) = await _auth.LoginAsync(DemoAccount.Username, DemoAccount.Password);
            if (user != null)
            {
                _log("Masuk ke akun demo.");
                return user;
            }

            _log($"Login gagal ({error}). Mencoba membuat akun demo...");
            var (ok, message) = await _auth.RegisterAsync(DemoAccount.Username, DemoAccount.Password);
            if (!ok)
            {
                _log($"Gagal membuat akun demo: {message}");
                return null;
            }

            _log("Akun demo dibuat.");
            return _auth.CurrentUser;
        }

        // remove the demo account's old lands (and their logs and records) so every run starts clean
        private async Task WipeAsync(Guid ownerId)
        {
            var client = await SupabaseConfig.GetClientAsync();
            var old = await _lands.GetLandsAsync(ownerId);
            foreach (var land in old)
            {
                long id = land.Id; // the query builder wants a plain local, not land.Id
                await client.From<DailyRecommendation>().Where(r => r.LandId == id).Delete();
                await client.From<IrrigationLog>().Where(l => l.LandId == id).Delete();
                await client.From<Land>().Where(l => l.Id == id).Delete();
            }
            if (old.Count > 0)
            {
                _log($"{old.Count} lahan lama dihapus.");
            }
        }

        private record PastDay(DateTime Date, double TempMax, double Humidity, double RainMm, double Probability, int Code);

        // the last 30 full days (today is left for the app to fill live)
        private static async Task<List<PastDay>> GetPastWeatherAsync(double lat, double lon)
        {
            var inv = CultureInfo.InvariantCulture;
            string url = "https://api.open-meteo.com/v1/forecast" +
                $"?latitude={lat.ToString(inv)}&longitude={lon.ToString(inv)}" +
                "&daily=temperature_2m_max,relative_humidity_2m_mean,precipitation_sum,precipitation_probability_max,weather_code" +
                $"&past_days={Days}&forecast_days=1&timezone=Asia%2FJakarta";

            using var doc = JsonDocument.Parse(await Http.GetStringAsync(url));
            var daily = doc.RootElement.GetProperty("daily");
            var days = new List<PastDay>();

            for (int i = 0; i < daily.GetProperty("time").GetArrayLength(); i++)
            {
                var date = DateTime.SpecifyKind(DateTime.Parse(daily.GetProperty("time")[i].GetString()!, inv), DateTimeKind.Local);
                if (date >= DateTime.Today)
                {
                    continue;
                }

                double rain = Number(daily, "precipitation_sum", i) ?? 0;
                // past days often have no probability; derive one from the rain that actually fell
                double probability = Number(daily, "precipitation_probability_max", i) ?? (rain >= 3 ? 80 : rain >= 0.5 ? 45 : 10);

                days.Add(new PastDay(
                    date,
                    Number(daily, "temperature_2m_max", i) ?? 30,
                    Number(daily, "relative_humidity_2m_mean", i) ?? 75,
                    rain,
                    probability,
                    (int)(Number(daily, "weather_code", i) ?? 2)));
            }
            return days;
        }

        private static double? Number(JsonElement daily, string name, int i)
        {
            var value = daily.GetProperty(name)[i];
            return value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;
        }

        // a few short notes so the history table doesn't look generated
        private static string Note(WateringDecision decision, double watered, double recommended, int seed)
        {
            if (decision == WateringDecision.Postpone)
            {
                return "Hujan tidak jadi turun di kebun.";
            }

            var notes = new[] { "", "", "", "Tanah terlihat kering.", "Pakai selang.", "Pakai gembor.", "", "Daun agak layu siang tadi." };
            if (watered > recommended * 1.2)
            {
                return "Sekalian membersihkan daun.";
            }
            return notes[seed % notes.Length];
        }
    }
}
