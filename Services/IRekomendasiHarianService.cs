using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lahanku.Models;

namespace Lahanku.Services
{
    public interface IRekomendasiHarianService
    {
        // simpan/perbarui catatan rekomendasi lahan untuk tanggal itu; null kalau gagal
        Task<EvaluasiIrigasi?> SimpanAsync(Land land, Rekomendasi rekomendasi, DateTime tanggal);

        // catatan harian satu lahan dalam rentang tanggal (urut terbaru dulu); kosong kalau gagal
        Task<List<EvaluasiIrigasi>> GetRiwayatAsync(long landId, DateTime dari, DateTime sampai);
    }
}
