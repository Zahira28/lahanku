using System;
using System.Threading.Tasks;
using Lahanku.Models;

namespace Lahanku.Services
{
    public interface IRekomendasiHarianService
    {
        // simpan/perbarui catatan rekomendasi lahan untuk tanggal itu; null kalau gagal
        Task<EvaluasiIrigasi?> SimpanAsync(Land land, Rekomendasi rekomendasi, DateTime tanggal);
    }
}
