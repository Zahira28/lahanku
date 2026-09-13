using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lahanku.Services
{
    public class GeoLocationResult
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? Village { get; set; }
        public string? District { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }

        /// <summary>
        /// Ringkasan lokasi ramah pengguna (contoh: "Kel. Dinoyo, Kec. Lowokwaru, Kota Malang")
        /// </summary>
        public string ShortLocationText { get; set; } = string.Empty;
    }

    public interface IGeocodingService
    {
        Task<GeoLocationResult?> ReverseGeocodeAsync(double latitude, double longitude);
        Task<List<GeoLocationResult>> SearchPlaceAsync(string query);
        Task<(double lat, double lon)?> ParseGoogleMapsUrlAsync(string url);
    }
}
