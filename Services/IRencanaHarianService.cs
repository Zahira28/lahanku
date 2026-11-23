using System;
using System.Threading.Tasks;
using Lahanku.Models;

namespace Lahanku.Services
{
    public interface IRencanaHarianService
    {
        // ambil cuaca + prakiraan, hitung rekomendasi, simpan catatan harian
        Task<RencanaLahan> HitungAsync(Land land, DateTime waktu);
    }
}
