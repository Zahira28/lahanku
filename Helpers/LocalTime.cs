using System;

namespace Lahanku.Helpers
{
    // Supabase treats an "Unspecified" DateTime as UTC, so the time shifts by +7h.
    // Every time sent to the DB goes through here first.
    public static class LocalTime
    {
        public static DateTime Ensure(DateTime time) => time.Kind switch
        {
            DateTimeKind.Utc => time.ToLocalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(time, DateTimeKind.Local),
            _ => time
        };
    }
}
