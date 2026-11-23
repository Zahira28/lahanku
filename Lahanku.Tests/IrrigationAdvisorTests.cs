using Lahanku.Models;
using Lahanku.Services;
using Xunit;

namespace Lahanku.Tests
{
    public class IrrigationAdvisorTests
    {
        private readonly IrrigationAdvisorService _advisor = new();
        private static readonly DateTime Pagi = new(2026, 9, 14, 7, 0, 0);
        private static readonly DateTime Siang = new(2026, 9, 14, 13, 0, 0);

        // 100 m², tanpa tanaman → kebutuhan default 4,5 mm → patokan 450 L
        private static Land Lahan(double? min = null, double? max = null) =>
            new() { Name = "Tes", AreaHectares = 0.01, MinMoisture = min, MaxMoisture = max };

        private static Cuaca Cuaca(double suhu = 27, double rh = 60, double hujan = 0) =>
            new() { Suhu = suhu, Kelembapan = rh, CurahHujan = hujan, KondisiCuaca = "Cerah" };

        [Fact]
        public void Offline_pakai_takaran_standar()
        {
            var r = _advisor.Hitung(Lahan(), null, null, Pagi);
            Assert.Equal(KeputusanSiram.Siram, r.Keputusan);
            Assert.Equal(450, r.VolumeAnjuran);
            Assert.Equal(450, r.VolumeStandar);
            Assert.Contains("tidak tersedia", r.Alasan);
        }

        [Fact]
        public void Hujan_sekarang_3mm_atau_lebih_ditunda()
        {
            var r = _advisor.Hitung(Lahan(), Cuaca(hujan: 5), null, Pagi);
            Assert.Equal(KeputusanSiram.Tunda, r.Keputusan);
            Assert.Equal(0, r.VolumeAnjuran);
            Assert.Equal(450, r.VolumeStandar);
            Assert.Equal(string.Empty, r.SaranWaktu);
        }

        [Fact]
        public void Prakiraan_hujan_lebat_dengan_peluang_tinggi_ditunda()
        {
            var r = _advisor.Hitung(Lahan(), Cuaca(), new PrakiraanHujan(8, 75, 15), Pagi);
            Assert.Equal(KeputusanSiram.Tunda, r.Keputusan);
            Assert.Contains("pukul 15.00", r.Alasan);
            Assert.Contains("75%", r.Alasan);
            Assert.Equal(8, r.HujanPrakiraanMm);
            Assert.Equal(75, r.PeluangHujan);
        }

        [Fact]
        public void Prakiraan_peluang_rendah_tidak_ditunda_tapi_takaran_dikurangi()
        {
            // 4,5 − 0,8 × (8 × 0,4) = 1,94 mm → 194 L
            var r = _advisor.Hitung(Lahan(), Cuaca(), new PrakiraanHujan(8, 40, null), Pagi);
            Assert.Equal(KeputusanSiram.Siram, r.Keputusan);
            Assert.Equal(194, r.VolumeAnjuran);
        }

        [Fact]
        public void Panas_dan_kering_jadi_siram_ekstra()
        {
            // 4,5 × 1,3 × 1,2 = 7,02 mm → 702 L
            var r = _advisor.Hitung(Lahan(), Cuaca(suhu: 34, rh: 40), null, Pagi);
            Assert.Equal(KeputusanSiram.SiramEkstra, r.Keputusan);
            Assert.Equal(702, r.VolumeAnjuran);
            Assert.Equal("Tambah volume penyiraman", r.Judul);
        }

        [Fact]
        public void Batas_kering_mengikuti_rentang_ideal_lahan()
        {
            // RH 55 masih di atas default 45, tapi di bawah ideal minimal 60
            var r = _advisor.Hitung(Lahan(60, 80), Cuaca(suhu: 33, rh: 55), null, Pagi);
            Assert.Equal(KeputusanSiram.SiramEkstra, r.Keputusan);
        }

        [Fact]
        public void Takaran_minimal_setengah_mm()
        {
            // 4,5 − 0,8 × (2,9 + 2,9) < 0,5 → dibulatkan ke 0,5 mm = 50 L
            var r = _advisor.Hitung(Lahan(), Cuaca(hujan: 2.9), new PrakiraanHujan(2.9, 100, null), Pagi);
            Assert.Equal(KeputusanSiram.Siram, r.Keputusan);
            Assert.Equal(50, r.VolumeAnjuran);
        }

        [Fact]
        public void Saran_waktu_mengikuti_jam()
        {
            Assert.Contains("09.00", _advisor.Hitung(Lahan(), Cuaca(), null, Pagi).SaranWaktu);
            Assert.Contains("16.00", _advisor.Hitung(Lahan(), Cuaca(), null, Siang).SaranWaktu);
        }

        [Fact]
        public void Anjuran_per_m2_sama_dengan_mm()
        {
            var r = _advisor.Hitung(Lahan(), Cuaca(), null, Pagi);
            Assert.Equal(4.5, r.AnjuranPerM2, 3);
        }

        [Theory]
        [InlineData(KeputusanSiram.Siram, "siram")]
        [InlineData(KeputusanSiram.Tunda, "tunda")]
        [InlineData(KeputusanSiram.SiramEkstra, "siram_ekstra")]
        public void Keputusan_bolak_balik_ke_teks_db(KeputusanSiram k, string db)
        {
            Assert.Equal(db, k.ToDb());
            Assert.Equal(k, KeputusanSiramExtensions.FromDb(db));
        }
    }
}
