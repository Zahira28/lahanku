using System.Collections.Generic;
using System.Threading.Tasks;
using Supabase;

namespace Lahanku.Helpers
{
    /// <summary>
    /// Menyediakan konfigurasi terpusat dan manajemen lifecycle Supabase Client (Singleton).
    /// Kredensial dibaca dari environment variable atau appsettings.json (lihat <see cref="AppSettings"/>).
    /// </summary>
    public static class SupabaseConfig
    {
        public const string UrlEnvVar = "LAHANKU_SUPABASE_URL";
        public const string AnonKeyEnvVar = "LAHANKU_SUPABASE_ANON_KEY";

        private static Supabase.Client? _client;

        public static string? Url { get; } = AppSettings.Get("Supabase", "Url", UrlEnvVar);
        public static string? AnonKey { get; } = AppSettings.Get("Supabase", "AnonKey", AnonKeyEnvVar);

        /// <summary>
        /// True jika URL dan AnonKey tersedia dan bukan placeholder dari appsettings.example.json.
        /// </summary>
        public static bool IsConfigured => GetMissingSettings().Count == 0;

        public static IReadOnlyList<string> GetMissingSettings()
        {
            var missing = new List<string>();
            if (IsMissing(Url)) missing.Add($"Supabase:Url (atau env {UrlEnvVar})");
            if (IsMissing(AnonKey)) missing.Add($"Supabase:AnonKey (atau env {AnonKeyEnvVar})");
            return missing;
        }

        public static async Task<Supabase.Client> GetClientAsync()
        {
            if (_client != null)
            {
                return _client;
            }

            if (!IsConfigured)
            {
                throw new System.InvalidOperationException(
                    "Konfigurasi Supabase belum diisi: " + string.Join(", ", GetMissingSettings()));
            }

            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            };

            var client = new Supabase.Client(Url!, AnonKey!, options);
            await client.InitializeAsync();
            _client = client;
            return _client;
        }

        private static bool IsMissing(string? value) =>
            string.IsNullOrWhiteSpace(value) || value.Contains("YOUR_", System.StringComparison.OrdinalIgnoreCase);
    }
}
