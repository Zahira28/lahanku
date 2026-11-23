using Lahanku.Helpers;
using Xunit;

namespace Lahanku.Tests
{
    public class LocalTimeTests
    {
        [Fact]
        public void Unspecified_is_treated_as_local()
        {
            var t = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var result = LocalTime.Ensure(t);
            Assert.Equal(DateTimeKind.Local, result.Kind);
            Assert.Equal(t.Ticks, result.Ticks); // the clock time doesn't change, it's just marked as local
        }

        [Fact]
        public void Utc_is_converted_to_local()
        {
            var utc = new DateTime(2026, 11, 23, 4, 32, 0, DateTimeKind.Utc);
            Assert.Equal(utc.ToLocalTime(), LocalTime.Ensure(utc));
        }

        [Fact]
        public void Local_is_unchanged()
        {
            var local = new DateTime(2026, 11, 23, 11, 32, 0, DateTimeKind.Local);
            Assert.Equal(local, LocalTime.Ensure(local));
        }
    }
}
