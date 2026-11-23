using System;
using System.IO;
using System.Text.Json;

namespace Lahanku.Helpers
{
    /// <summary>
    /// Sumber konfigurasi terpusat aplikasi (SRP).
    /// Urutan prioritas: Environment Variable → appsettings.json.
    /// Tidak ada nilai rahasia yang di-hardcode di source code.
    /// </summary>
    public static class AppSettings
    {
        public const string FileName = "appsettings.json";

        private static readonly JsonDocument? _document = LoadDocument();

        /// <summary>
        /// Path appsettings.json yang ditemukan, atau null jika file tidak ada.
        /// </summary>
        public static string? LoadedFilePath { get; private set; }

        /// <summary>
        /// Mengambil nilai konfigurasi. Environment variable selalu menang atas isi file.
        /// </summary>
        public static string? Get(string section, string key, string? environmentVariable = null)
        {
            if (!string.IsNullOrWhiteSpace(environmentVariable))
            {
                var envValue = Environment.GetEnvironmentVariable(environmentVariable);
                if (!string.IsNullOrWhiteSpace(envValue))
                {
                    return envValue.Trim();
                }
            }

            if (_document != null &&
                _document.RootElement.TryGetProperty(section, out var sectionElement) &&
                sectionElement.ValueKind == JsonValueKind.Object &&
                sectionElement.TryGetProperty(key, out var valueElement) &&
                valueElement.ValueKind == JsonValueKind.String)
            {
                var value = valueElement.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return null;
        }

        private static JsonDocument? LoadDocument()
        {
            var candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName),
                Path.Combine(Directory.GetCurrentDirectory(), FileName)
            };

            foreach (var path in candidates)
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    var document = JsonDocument.Parse(File.ReadAllText(path));
                    LoadedFilePath = path;
                    return document;
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AppSettings] {path} bukan JSON valid: {ex.Message}");
                }
            }

            return null;
        }
    }
}
