using System;
using System.IO;
using System.Text.Json;

namespace Lahanku.Helpers
{
    // Read config from environment variables first, then fall back to appsettings.json.
    public static class AppSettings
    {
        public const string FileName = "appsettings.json";

        private static readonly JsonDocument? _document = LoadDocument();

        public static string? LoadedFilePath { get; private set; }

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

            return LoadEmbedded();
        }

        // no file next to the .exe: use the copy baked in at build time (single-file exe)
        private static JsonDocument? LoadEmbedded()
        {
            try
            {
                using var stream = typeof(AppSettings).Assembly.GetManifestResourceStream(EmbeddedName);
                if (stream == null)
                {
                    return null;
                }
                var document = JsonDocument.Parse(stream);
                LoadedFilePath = "(embedded)";
                return document;
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppSettings] embedded config is not valid JSON: {ex.Message}");
                return null;
            }
        }

        private const string EmbeddedName = "Lahanku.appsettings.json";
    }
}
