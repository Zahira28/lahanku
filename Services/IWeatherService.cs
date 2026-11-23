using System.Threading.Tasks;
using Lahanku.Models;

namespace Lahanku.Services
{
    /// <summary>
    /// Abstraksi layanan data satelit cuaca (DIP - Dependency Inversion Principle).
    /// </summary>
    public interface IWeatherService
    {
        /// <summary>
        /// Mengambil data cuaca real-time dari koordinat satelit Open-Meteo.
        /// </summary>
        Task<Cuaca?> GetCurrentWeatherAsync(double latitude, double longitude, long? landId = null);

        /// <summary>
        /// Menerjemahkan kode WMO ke deskripsi bahasa Indonesia dan key ikon cuaca (lihat Styles/WeatherIcons.xaml).
        /// </summary>
        (string Condition, string IconKey) GetWmoDescription(int weatherCode);
    }
}
