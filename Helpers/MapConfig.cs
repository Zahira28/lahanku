using System;
using System.IO;
using System.Text.Json;

namespace Lahanku.Helpers
{
    /// <summary>
    /// Menyediakan konfigurasi terpusat untuk layanan Peta & CARTO Basemaps.
    /// Membaca konfigurasi dari appsettings.json standar .NET,
    /// dengan fallback default agar aplikasi selalu siap pakai saat runtime.
    /// </summary>
    public static class MapConfig
    {
        public static string CartoApiKey { get; private set; } = "cb1_3j5x_1_292479c406cbc3c525637f87";

        static MapConfig()
        {
            LoadConfig();
        }

        private static void LoadConfig()
        {
            try
            {
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var configPath = Path.Combine(basePath, "appsettings.json");

                if (!File.Exists(configPath))
                {
                    configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                }

                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    using var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("Maps", out var mapsSection) &&
                        mapsSection.TryGetProperty("CartoApiKey", out var keyProp) &&
                        !string.IsNullOrWhiteSpace(keyProp.GetString()))
                    {
                        CartoApiKey = keyProp.GetString()!;
                        return;
                    }

                    if (doc.RootElement.TryGetProperty("Carto", out var cartoSection) &&
                        cartoSection.TryGetProperty("ApiKey", out var cartoKeyProp) &&
                        !string.IsNullOrWhiteSpace(cartoKeyProp.GetString()))
                    {
                        CartoApiKey = cartoKeyProp.GetString()!;
                        return;
                    }
                }
            }
            catch
            {
                // Fallback otomatis menggunakan default key yang aman
            }
        }
    }
}
