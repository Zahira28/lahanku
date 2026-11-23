using Lahanku.Helpers;
using Xunit;

namespace Lahanku.Tests
{
    public class WaktuLokalTests
    {
        [Fact]
        public void Unspecified_dianggap_waktu_lokal()
        {
            var t = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var hasil = WaktuLokal.Pastikan(t);
            Assert.Equal(DateTimeKind.Local, hasil.Kind);
            Assert.Equal(t.Ticks, hasil.Ticks); // jam-nya nggak berubah, cuma ditandai lokal
        }

        [Fact]
        public void Utc_dikonversi_ke_lokal()
        {
            var utc = new DateTime(2026, 11, 23, 4, 32, 0, DateTimeKind.Utc);
            Assert.Equal(utc.ToLocalTime(), WaktuLokal.Pastikan(utc));
        }

        [Fact]
        public void Lokal_tidak_diubah()
        {
            var lokal = new DateTime(2026, 11, 23, 11, 32, 0, DateTimeKind.Local);
            Assert.Equal(lokal, WaktuLokal.Pastikan(lokal));
        }
    }
}
