namespace Lahanku.Helpers
{
    /// <summary>
    /// Menyediakan konfigurasi terpusat untuk layanan Peta & CARTO Basemaps.
    /// Key dibaca dari environment variable atau appsettings.json (lihat <see cref="AppSettings"/>).
    /// Key bersifat opsional: tanpa key, basemap CARTO publik tetap dapat dimuat.
    /// </summary>
    public static class MapConfig
    {
        public const string CartoApiKeyEnvVar = "LAHANKU_CARTO_API_KEY";

        public static string CartoApiKey { get; } = ResolveCartoKey();

        private static string ResolveCartoKey()
        {
            var key = AppSettings.Get("Maps", "CartoApiKey", CartoApiKeyEnvVar)
                      ?? AppSettings.Get("Carto", "ApiKey"); // kompatibilitas format lama

            return key == null || key.Contains("YOUR_", System.StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : key;
        }
    }
}
