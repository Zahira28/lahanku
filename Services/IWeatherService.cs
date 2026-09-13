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
        /// Menerjemahkan kode WMO standar meteorologi dunia ke deskripsi bahasa Indonesia dan emoji visual.
        /// </summary>
        (string Condition, string IconEmoji) GetWmoDescription(int weatherCode);
    }
}
