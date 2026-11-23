using Lahanku.Models;

namespace Lahanku.Services
{
    /// <summary>
    /// Abstraksi sistem penasihat irigasi cerdas Climate-Smart Agriculture (SDG 13).
    /// </summary>
    public interface IIrrigationAdvisorService
    {
        /// <summary>
        /// Menghitung volume anjuran penyiraman dan analisis efisiensi air
        /// berdasarkan standar tanaman FAO 56, luas lahan, dan cuaca real-time.
        /// </summary>
        EvaluasiIrigasi CalculateRecommendation(Land land, Cuaca? cuaca);
    }
}
