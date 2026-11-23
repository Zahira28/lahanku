using Lahanku.Models;
using Xunit;

namespace Lahanku.Tests
{
    public class BarisRiwayatTests
    {
        private static readonly DateTime Senin = new(2026, 11, 16);
        private static readonly DateTime Selasa = new(2026, 11, 17);

        private static EvaluasiIrigasi Catatan(DateTime tgl, double anjuran, string keputusan = "siram") =>
            new() { IdLahan = 1, Tanggal = tgl, VolumeAnjuran = anjuran, VolumeStandar = 480, Keputusan = keputusan };

        private static IrrigationLog Log(DateTime waktu, double liter, string? catatan = "Pagi") =>
            new() { LandId = 1, Date = waktu, VolumeLiters = liter, Notes = catatan };

        [Fact]
        public void Log_tanpa_catatan_rekomendasi_tidak_punya_evaluasi()
        {
            var rows = BarisRiwayat.Susun(new[] { Log(Senin.AddHours(7), 300) }, Array.Empty<EvaluasiIrigasi>());
            var r = Assert.Single(rows);
            Assert.Equal("-", r.RekomendasiText);
            Assert.Null(r.Evaluasi);
            Assert.Equal("-", r.EvaluasiText);
        }

        [Fact]
        public void Log_sesuai_rekomendasi()
        {
            var rows = BarisRiwayat.Susun(new[] { Log(Senin.AddHours(7), 460) }, new[] { Catatan(Senin, 450) });
            var r = Assert.Single(rows);
            Assert.Equal("450 L", r.RekomendasiText);
            Assert.Equal(KategoriEvaluasi.Sesuai, r.Evaluasi!.Kategori);
            Assert.Equal("ok", r.EvaluasiJenis);
        }

        [Fact]
        public void Evaluasi_dihitung_dari_total_sehari()
        {
            var rows = BarisRiwayat.Susun(
                new[] { Log(Senin.AddHours(7), 200), Log(Senin.AddHours(16), 400) },
                new[] { Catatan(Senin, 420) });

            Assert.Equal(2, rows.Count);
            Assert.All(rows, r => Assert.Equal("Melebihi 180 L", r.EvaluasiText));
            Assert.All(rows, r => Assert.Equal("warn", r.EvaluasiJenis));
        }

        [Fact]
        public void Hari_tunda_tanpa_log_jadi_baris_sendiri()
        {
            var rows = BarisRiwayat.Susun(Array.Empty<IrrigationLog>(), new[] { Catatan(Selasa, 0, "tunda") });
            var r = Assert.Single(rows);
            Assert.Null(r.Log);
            Assert.False(r.BisaDiubah);
            Assert.Equal("0 L", r.VolumeText);
            Assert.Equal("Tunda", r.RekomendasiText);
            Assert.Equal("Ditunda karena hujan", r.EvaluasiText);
            Assert.Equal("skip", r.EvaluasiJenis);
        }

        [Fact]
        public void Catatan_kosong_ditandai()
        {
            var rows = BarisRiwayat.Susun(new[] { Log(Senin.AddHours(7), 300, "  ") }, Array.Empty<EvaluasiIrigasi>());
            Assert.Equal("Tidak ada catatan", rows[0].CatatanText);
            Assert.True(rows[0].CatatanKosong);
        }

        [Fact]
        public void Urut_terbaru_dulu()
        {
            var rows = BarisRiwayat.Susun(
                new[] { Log(Senin.AddHours(7), 300), Log(Senin.AddHours(16), 100) },
                new[] { Catatan(Selasa, 0, "tunda") });

            Assert.Equal(Selasa, rows[0].Tanggal.Date);
            Assert.Equal(16, rows[1].Tanggal.Hour);
            Assert.Equal(7, rows[2].Tanggal.Hour);
        }
    }
}
