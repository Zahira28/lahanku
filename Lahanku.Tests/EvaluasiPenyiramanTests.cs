using Lahanku.Models;
using Xunit;

namespace Lahanku.Tests
{
    public class EvaluasiPenyiramanTests
    {
        [Fact]
        public void Tunda_tanpa_siram_berarti_ditunda_karena_hujan()
        {
            var h = EvaluasiPenyiraman.Nilai(KeputusanSiram.Tunda, 0, 0);
            Assert.Equal(KategoriEvaluasi.Ditunda, h.Kategori);
            Assert.Equal("Ditunda karena hujan", h.Teks);
            Assert.True(h.SesuaiRekomendasi);
        }

        [Fact]
        public void Tunda_tapi_tetap_siram_berarti_tidak_diperlukan()
        {
            var h = EvaluasiPenyiraman.Nilai(KeputusanSiram.Tunda, 0, 200);
            Assert.Equal(KategoriEvaluasi.TidakDiperlukan, h.Kategori);
            Assert.Equal(200, h.Selisih);
            Assert.False(h.SesuaiRekomendasi);
        }

        [Theory]
        [InlineData(420, 336)]   // 80% batas bawah
        [InlineData(420, 504)]   // 120% batas atas
        [InlineData(450, 460)]
        public void Dalam_80_sampai_120_persen_sesuai(double anjuran, double aktual)
        {
            var h = EvaluasiPenyiraman.Nilai(KeputusanSiram.Siram, anjuran, aktual);
            Assert.Equal(KategoriEvaluasi.Sesuai, h.Kategori);
            Assert.Equal("Sesuai rekomendasi", h.Teks);
            Assert.True(h.SesuaiRekomendasi);
        }

        [Fact]
        public void Lebih_dari_120_persen_melebihi()
        {
            var h = EvaluasiPenyiraman.Nilai(KeputusanSiram.Siram, 420, 700);
            Assert.Equal(KategoriEvaluasi.Melebihi, h.Kategori);
            Assert.Equal(280, h.Selisih);
            Assert.Equal("Melebihi 280 L", h.Teks);
        }

        [Fact]
        public void Kurang_dari_80_persen_kurang()
        {
            var h = EvaluasiPenyiraman.Nilai(KeputusanSiram.SiramEkstra, 500, 300);
            Assert.Equal(KategoriEvaluasi.Kurang, h.Kategori);
            Assert.Equal(200, h.Selisih);
            Assert.Equal("Kurang 200 L", h.Teks);
            Assert.False(h.SesuaiRekomendasi);
        }
    }
}
