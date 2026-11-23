using Lahanku.Controls;
using Xunit;

namespace Lahanku.Tests
{
    public class KalenderBulanTests
    {
        private static readonly DateTime HariIni = new(2026, 11, 23);

        [Fact]
        public void Selalu_42_kotak_mulai_hari_senin()
        {
            var hari = KalenderBulan.Susun(new DateTime(2026, 11, 1), HariIni, HariIni, HariIni);
            Assert.Equal(42, hari.Count);
            // 1 November 2026 hari Minggu → kotak pertama Senin 26 Oktober
            Assert.Equal(new DateTime(2026, 10, 26), hari[0].Tanggal);
            Assert.Equal(DayOfWeek.Monday, hari[0].Tanggal.DayOfWeek);
        }

        [Fact]
        public void Tanggal_bulan_lain_ditandai()
        {
            var hari = KalenderBulan.Susun(new DateTime(2026, 11, 1), HariIni, HariIni, HariIni);
            Assert.True(hari[0].BulanLain);
            Assert.False(hari.Single(h => h.Tanggal == new DateTime(2026, 11, 15)).BulanLain);
        }

        [Fact]
        public void Setelah_maksimum_nonaktif()
        {
            var hari = KalenderBulan.Susun(new DateTime(2026, 11, 1), HariIni, HariIni, HariIni);
            Assert.False(hari.Single(h => h.Tanggal == HariIni).Nonaktif);
            Assert.True(hari.Single(h => h.Tanggal == HariIni.AddDays(1)).Nonaktif);
        }

        [Fact]
        public void Hari_ini_dan_terpilih_ditandai()
        {
            var dipilih = new DateTime(2026, 11, 20, 7, 0, 0);
            var hari = KalenderBulan.Susun(new DateTime(2026, 11, 1), dipilih, HariIni, HariIni);
            Assert.True(hari.Single(h => h.Tanggal == HariIni).HariIni);
            Assert.True(hari.Single(h => h.Tanggal == dipilih.Date).Terpilih);
            Assert.Equal(1, hari.Count(h => h.Terpilih));
        }

        [Fact]
        public void Bulan_berikutnya_dikunci_di_bulan_maksimum()
        {
            Assert.False(KalenderBulan.BolehMaju(new DateTime(2026, 11, 1), HariIni));
            Assert.True(KalenderBulan.BolehMaju(new DateTime(2026, 10, 1), HariIni));
        }

        [Fact]
        public void Semua_tanggal_bertanda_waktu_lokal()
        {
            // tanggal Unspecified dianggap UTC oleh Supabase → geser +7 jam
            var hari = KalenderBulan.Susun(new DateTime(2026, 11, 1), HariIni, HariIni, HariIni);
            Assert.All(hari, h => Assert.Equal(DateTimeKind.Local, h.Tanggal.Kind));
        }

        [Fact]
        public void Judul_bulan_bahasa_indonesia()
        {
            Assert.Equal("November 2026", KalenderBulan.Judul(new DateTime(2026, 11, 1)));
        }
    }
}
