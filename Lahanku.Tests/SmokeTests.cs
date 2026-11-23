using Lahanku.Models;
using Xunit;

namespace Lahanku.Tests
{
    public class SmokeTests
    {
        [Fact]
        public void Land_can_be_created_without_supabase()
        {
            var land = new Land { Name = "Tes", AreaHectares = 0.01 };
            Assert.Equal("100 m²", land.AreaText);
        }
    }
}
