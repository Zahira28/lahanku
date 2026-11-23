using Lahanku.Models;
using Xunit;

namespace Lahanku.Tests
{
    public class PrakiraanHujanTests
    {
        private static readonly string[] Jam = { "2026-09-14T13:00", "2026-09-14T14:00", "2026-09-14T15:00", "2026-09-14T16:00" };

        [Fact]
        public void Menjumlah_hujan_dan_ambil_peluang_tertinggi()
        {
            var p = PrakiraanHujan.DariPerJam(Jam, new double?[] { 0, 0.2, 5, 3 }, new double?[] { 10, 30, 75, 60 });
            Assert.Equal(8.2, p.TotalMm, 3);
            Assert.Equal(75, p.PeluangMaks);
        }

        [Fact]
        public void Jam_mulai_adalah_jam_pertama_hujan_minimal_setengah_mm_dan_peluang_50()
        {
            // 14:00 hujan 0,2 mm diabaikan; 15:00 hujan 5 mm peluang 75% → mulai jam 15
            var p = PrakiraanHujan.DariPerJam(Jam, new double?[] { 0, 0.2, 5, 3 }, new double?[] { 10, 30, 75, 60 });
            Assert.Equal(15, p.JamMulai);
        }

        [Fact]
        public void Tanpa_hujan_jam_mulai_kosong()
        {
            var p = PrakiraanHujan.DariPerJam(Jam, new double?[] { 0, 0, 0, 0 }, new double?[] { 5, 5, 5, 5 });
            Assert.Equal(0, p.TotalMm);
            Assert.Null(p.JamMulai);
        }

        [Fact]
        public void Nilai_null_dari_api_dianggap_nol()
        {
            var p = PrakiraanHujan.DariPerJam(Jam, new double?[] { null, 1, null, null }, new double?[] { null, null, null, null });
            Assert.Equal(1, p.TotalMm, 3);
            Assert.Equal(0, p.PeluangMaks);
        }
    }
}
