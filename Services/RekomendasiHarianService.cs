using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lahanku.Helpers;
using Lahanku.Models;
using static Supabase.Postgrest.Constants;

namespace Lahanku.Services
{
    // 1 catatan rekomendasi per lahan per hari. Diperbarui tiap app dibuka,
    // tapi dibekukan begitu ada penyiraman yang dicatat hari itu.
    public class RekomendasiHarianService : IRekomendasiHarianService
    {
        private readonly ILandService _landService;

        public RekomendasiHarianService(ILandService landService)
        {
            _landService = landService;
        }

        public async Task<EvaluasiIrigasi?> SimpanAsync(Land land, Rekomendasi rekomendasi, DateTime tanggal)
        {
            var hari = tanggal.Date;
            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                var existing = (await client
                    .From<EvaluasiIrigasi>()
                    .Filter("id_lahan", Operator.Equals, land.Id.ToString())
                    .Filter("tanggal", Operator.Equals, hari.ToString("yyyy-MM-dd"))
                    .Get()).Models.FirstOrDefault();

                if (existing != null)
                {
                    // sudah ada penyiraman hari itu → rekomendasinya dibekukan
                    var logs = await _landService.GetIrrigationLogsAsync(land.Id);
                    if (logs.Any(l => l.Date.Date == hari))
                    {
                        return existing;
                    }

                    Isi(existing, rekomendasi);
                    await client.From<EvaluasiIrigasi>().Update(existing);
                    return existing;
                }

                var baru = new EvaluasiIrigasi { IdLahan = land.Id, Tanggal = hari, CreatedAt = DateTime.UtcNow };
                Isi(baru, rekomendasi);
                var response = await client.From<EvaluasiIrigasi>().Insert(baru);
                return response.Models.FirstOrDefault() ?? baru;
            }
            catch (Exception ex)
            {
                // gagal simpan nggak boleh ganggu tampilan rekomendasi
                System.Diagnostics.Debug.WriteLine($"[RekomendasiHarian] Gagal simpan: {ex}");
                return null;
            }
        }

        public async Task<List<EvaluasiIrigasi>> GetRiwayatAsync(long landId, DateTime dari, DateTime sampai)
        {
            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                var response = await client
                    .From<EvaluasiIrigasi>()
                    .Filter("id_lahan", Operator.Equals, landId.ToString())
                    .Filter("tanggal", Operator.GreaterThanOrEqual, dari.Date.ToString("yyyy-MM-dd"))
                    .Filter("tanggal", Operator.LessThanOrEqual, sampai.Date.ToString("yyyy-MM-dd"))
                    .Order("tanggal", Ordering.Descending)
                    .Get();
                return response.Models;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RekomendasiHarian] Gagal ambil riwayat: {ex}");
                return new List<EvaluasiIrigasi>();
            }
        }

        private static void Isi(EvaluasiIrigasi target, Rekomendasi r)
        {
            target.Keputusan = r.Keputusan.ToDb();
            target.VolumeAnjuran = r.VolumeAnjuran;
            target.VolumeStandar = r.VolumeStandar;
            target.HujanPrakiraanMm = r.HujanPrakiraanMm;
            target.PeluangHujan = r.PeluangHujan;
            target.StatusEfisiensi = r.Judul;
            target.Saran = string.IsNullOrEmpty(r.SaranWaktu) ? r.Alasan : $"{r.Alasan} {r.SaranWaktu}";
        }
    }
}
