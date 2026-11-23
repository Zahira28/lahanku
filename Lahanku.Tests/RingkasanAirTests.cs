using Lahanku.Models;
using Xunit;

namespace Lahanku.Tests
{
    public class RingkasanAirTests
    {
        private static readonly DateTime Senin = new(2026, 11, 16);
        private static readonly DateTime Selasa = new(2026, 11, 17);

        private static EvaluasiIrigasi Catatan(long lahan, DateTime tgl, double standar, string keputusan = "siram") =>
            new() { IdLahan = lahan, Tanggal = tgl, VolumeStandar = standar, Keputusan = keputusan };

        private static IrrigationLog Log(long lahan, DateTime tgl, double liter) =>
            new() { LandId = lahan, Date = tgl.AddHours(7), VolumeLiters = liter };

        [Fact]
        public void Hari_tanpa_catatan_tidak_dihitung()
        {
            var r = RingkasanAir.Hitung(
                new[] { Catatan(1, Senin, 480) },
                new[] { Log(1, Senin, 460), Log(1, Selasa, 500) });

            Assert.Equal(480, r.Standar);
            Assert.Equal(460, r.Dipakai);
            Assert.Equal(20, r.Dihemat);
        }

        [Fact]
        public void Hari_tunda_tanpa_siram_hemat_penuh()
        {
            var r = RingkasanAir.Hitung(new[] { Catatan(1, Senin, 480, "tunda") }, Array.Empty<IrrigationLog>());
            Assert.Equal(480, r.Dihemat);
            Assert.Equal(100, r.PersenHemat, 3);
        }

        [Fact]
        public void Log_dicocokkan_per_lahan_dan_tanggal()
        {
            // lahan 2 nggak punya catatan di hari itu → lognya nggak ikut
            var r = RingkasanAir.Hitung(
                new[] { Catatan(1, Senin, 480) },
                new[] { Log(1, Senin, 400), Log(2, Senin, 300) });

            Assert.Equal(400, r.Dipakai);
        }

        [Fact]
        public void Beberapa_penyiraman_sehari_dijumlah()
        {
            var r = RingkasanAir.Hitung(
                new[] { Catatan(1, Senin, 480) },
                new[] { Log(1, Senin, 200), Log(1, Senin, 300) });

            Assert.Equal(500, r.Dipakai);
            Assert.Equal(-20, r.Dihemat);
        }

        [Fact]
        public void Tanpa_data_persen_nol()
        {
            var r = RingkasanAir.Hitung(Array.Empty<EvaluasiIrigasi>(), Array.Empty<IrrigationLog>());
            Assert.Equal(0, r.PersenHemat);
            Assert.False(r.AdaData);
        }
    }
}
