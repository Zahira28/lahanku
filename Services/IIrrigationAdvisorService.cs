using System;
using Lahanku.Models;

namespace Lahanku.Services
{
    public interface IIrrigationAdvisorService
    {
        // rekomendasi hari ini dari cuaca sekarang + prakiraan 24 jam
        Rekomendasi Hitung(Land land, Cuaca? cuaca, PrakiraanHujan? prakiraan, DateTime waktu);

        // versi lama, dipertahankan sampai UI pindah ke Hitung
        EvaluasiIrigasi CalculateRecommendation(Land land, Cuaca? cuaca);
    }
}
