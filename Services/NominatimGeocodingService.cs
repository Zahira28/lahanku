using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lahanku.Services
{
    public class NominatimGeocodingService : IGeocodingService
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        static NominatimGeocodingService()
        {
            // OpenStreetMap Nominatim mewajibkan User-Agent yang jelas agar tidak diblokir
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("LahanKu-PrecisionAgriculture/1.0 (desktop-app)");
                _httpClient.DefaultRequestHeaders.Add("Accept-Language", "id,en;q=0.8");
            }
        }

        public async Task<GeoLocationResult?> ReverseGeocodeAsync(double latitude, double longitude)
        {
            try
            {
                var latStr = latitude.ToString("0.000000", CultureInfo.InvariantCulture);
                var lonStr = longitude.ToString("0.000000", CultureInfo.InvariantCulture);
                var url = $"https://nominatim.openstreetmap.org/reverse?lat={latStr}&lon={lonStr}&format=jsonv2&addressdetails=1";

                using var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var result = new GeoLocationResult
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    DisplayName = root.TryGetProperty("display_name", out var dn) ? dn.GetString() ?? string.Empty : string.Empty
                };

                if (root.TryGetProperty("address", out var address))
                {
                    result.Village = GetStringProperty(address, "village", "suburb", "neighbourhood", "quarter", "hamlet");
                    result.District = GetStringProperty(address, "city_district", "county", "municipality", "subdistrict");
                    result.City = GetStringProperty(address, "city", "regency", "town");
                    result.Province = GetStringProperty(address, "state");

                    result.ShortLocationText = BuildShortAddress(result.Village, result.District, result.City, result.Province);
                }
                else
                {
                    result.ShortLocationText = result.DisplayName;
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Geocoding] ReverseGeocode error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<GeoLocationResult>> SearchPlaceAsync(string query)
        {
            var list = new List<GeoLocationResult>();
            if (string.IsNullOrWhiteSpace(query))
                return list;

            try
            {
                var escaped = Uri.EscapeDataString(query.Trim());
                var url = $"https://nominatim.openstreetmap.org/search?q={escaped}&format=jsonv2&addressdetails=1&countrycodes=id&limit=5";

                using var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return list;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (!item.TryGetProperty("lat", out var latProp) || !item.TryGetProperty("lon", out var lonProp))
                        continue;

                    if (!double.TryParse(latProp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) ||
                        !double.TryParse(lonProp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
                        continue;

                    var res = new GeoLocationResult
                    {
                        Latitude = lat,
                        Longitude = lon,
                        DisplayName = item.TryGetProperty("display_name", out var dn) ? dn.GetString() ?? string.Empty : string.Empty
                    };

                    if (item.TryGetProperty("address", out var address))
                    {
                        res.Village = GetStringProperty(address, "village", "suburb", "neighbourhood", "quarter");
                        res.District = GetStringProperty(address, "city_district", "county", "municipality");
                        res.City = GetStringProperty(address, "city", "regency", "town");
                        res.Province = GetStringProperty(address, "state");
                        res.ShortLocationText = BuildShortAddress(res.Village, res.District, res.City, res.Province);
                    }
                    else
                    {
                        res.ShortLocationText = res.DisplayName;
                    }

                    list.Add(res);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Geocoding] SearchPlace error: {ex.Message}");
            }

            return list;
        }

        public async Task<(double lat, double lon)?> ParseGoogleMapsUrlAsync(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var trimmed = input.Trim();

            // 1. Cek koordinat mentah langsung (misal "-7.9826, 112.6308")
            var rawMatch = Regex.Match(trimmed, @"^(-?\d{1,2}(?:\.\d+)?)[,\s]+(-?\d{1,3}(?:\.\d+)?)$");
            if (rawMatch.Success &&
                double.TryParse(rawMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double rawLat) &&
                double.TryParse(rawMatch.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double rawLon))
            {
                return (rawLat, rawLon);
            }

            // 2. Jika link pendek (maps.app.goo.gl atau goo.gl/maps), resolve URL aslinya terlebih dahulu
            var targetUrl = trimmed;
            if (trimmed.Contains("maps.app.goo.gl") || trimmed.Contains("goo.gl/maps"))
            {
                try
                {
                    using var handler = new HttpClientHandler { AllowAutoRedirect = false };
                    using var redirectClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
                    using var headRes = await redirectClient.GetAsync(trimmed);

                    if (headRes.Headers.Location != null)
                    {
                        targetUrl = headRes.Headers.Location.ToString();
                    }
                }
                catch
                {
                    // Fallback jika redirect gagal
                }
            }

            // 3. Ekstrak pola koordinat dari URL Google Maps:
            // Pola A: @-7.9826,112.6308
            var atMatch = Regex.Match(targetUrl, @"@(-?\d+\.\d+),(-?\d+\.\d+)");
            if (atMatch.Success &&
                double.TryParse(atMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double atLat) &&
                double.TryParse(atMatch.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double atLon))
            {
                return (atLat, atLon);
            }

            // Pola B: ?q=-7.9826,112.6308 atau &ll=-7.9826,112.6308
            var qMatch = Regex.Match(targetUrl, @"[?&](?:q|ll)=(-?\d+\.\d+),(-?\d+\.\d+)");
            if (qMatch.Success &&
                double.TryParse(qMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double qLat) &&
                double.TryParse(qMatch.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double qLon))
            {
                return (qLat, qLon);
            }

            // Pola C: /place/(-?\d+\.\d+),(-?\d+\.\d+)
            var placeMatch = Regex.Match(targetUrl, @"/place/(-?\d+\.\d+),(-?\d+\.\d+)");
            if (placeMatch.Success &&
                double.TryParse(placeMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double pLat) &&
                double.TryParse(placeMatch.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double pLon))
            {
                return (pLat, pLon);
            }

            return null;
        }

        private static string? GetStringProperty(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    var val = prop.GetString();
                    if (!string.IsNullOrWhiteSpace(val))
                        return val.Trim();
                }
            }
            return null;
        }

        private static string BuildShortAddress(string? village, string? district, string? city, string? province)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(village))
            {
                var v = village.Trim();
                if (!v.StartsWith("Desa", StringComparison.OrdinalIgnoreCase) && 
                    !v.StartsWith("Kel.", StringComparison.OrdinalIgnoreCase) &&
                    !v.StartsWith("Kelurahan", StringComparison.OrdinalIgnoreCase))
                {
                    v = $"Kel. {v}";
                }
                parts.Add(v);
            }

            if (!string.IsNullOrWhiteSpace(district))
            {
                var d = district.Trim();
                if (!d.StartsWith("Kec.", StringComparison.OrdinalIgnoreCase) &&
                    !d.StartsWith("Kecamatan", StringComparison.OrdinalIgnoreCase))
                {
                    d = $"Kec. {d}";
                }
                parts.Add(d);
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                parts.Add(city.Trim());
            }
            else if (!string.IsNullOrWhiteSpace(province))
            {
                parts.Add(province.Trim());
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "Lokasi Terpilih";
        }
    }
}
