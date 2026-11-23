using Lahanku.Models;
using Xunit;

namespace Lahanku.Tests
{
    public class SmokeTests
    {
        [Fact]
        public void Land_bisa_dibuat_tanpa_supabase()
        {
            var land = new Land { Name = "Tes", AreaHectares = 0.01 };
            Assert.Equal("100 m²", land.AreaText);
        }
    }
}
