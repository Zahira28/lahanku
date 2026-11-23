using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Supabase;

namespace Lahanku.Helpers
{
    /// <summary>
    /// Menyediakan konfigurasi terpusat dan manajemen lifecycle Supabase Client (Singleton).
    /// </summary>
    public static class SupabaseConfig
    {
        private static Supabase.Client? _client;
        private static readonly object _lock = new();

        public static string Url { get; private set; } = "https://icdevgpvqnghnbieuvnw.supabase.co";
        public static string AnonKey { get; private set; } = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImljZGV2Z3B2cW5naG5iaWV1dm53Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODkyODcyMjgsImV4cCI6MjEwNDg2MzIyOH0.H6gKq9Hpuyn_em35IPRcJN9qlWNjqJ3uD5JqVpDd3mc";

        static SupabaseConfig()
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
                    if (doc.RootElement.TryGetProperty("Supabase", out var supabaseSection))
                    {
                        if (supabaseSection.TryGetProperty("Url", out var urlProp) && !string.IsNullOrWhiteSpace(urlProp.GetString()))
                        {
                            Url = urlProp.GetString()!;
                        }
                        if (supabaseSection.TryGetProperty("AnonKey", out var keyProp) && !string.IsNullOrWhiteSpace(keyProp.GetString()))
                        {
                            AnonKey = keyProp.GetString()!;
                        }
                    }
                }
            }
            catch
            {
                // Fallback otomatis menggunakan kredensial default
            }
        }

        public static async Task<Supabase.Client> GetClientAsync()
        {
            if (_client != null)
            {
                return _client;
            }

            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            };

            var client = new Supabase.Client(Url, AnonKey, options);
            await client.InitializeAsync();
            _client = client;
            return _client;
        }
    }
}
