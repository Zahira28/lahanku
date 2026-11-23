namespace Lahanku.Helpers
{
    // CARTO key is optional, the basemap still loads without it.
    public static class MapConfig
    {
        public const string CartoApiKeyEnvVar = "LAHANKU_CARTO_API_KEY";

        public static string CartoApiKey { get; } = ResolveCartoKey();

        private static string ResolveCartoKey()
        {
            var key = AppSettings.Get("Maps", "CartoApiKey", CartoApiKeyEnvVar)
                      ?? AppSettings.Get("Carto", "ApiKey"); // format config lama

            return key == null || key.Contains("YOUR_", System.StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : key;
        }
    }
}
