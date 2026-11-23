using System;

namespace Lahanku.Helpers
{
    // Supabase menganggap DateTime "Unspecified" sebagai UTC, jadi jamnya geser +7.
    // Semua waktu yang dikirim ke DB dilewatkan ke sini dulu.
    public static class WaktuLokal
    {
        public static DateTime Pastikan(DateTime waktu) => waktu.Kind switch
        {
            DateTimeKind.Utc => waktu.ToLocalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(waktu, DateTimeKind.Local),
            _ => waktu
        };
    }
}
