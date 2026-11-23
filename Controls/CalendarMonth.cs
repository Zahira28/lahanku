using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lahanku.Controls
{
    public record CalendarDay(DateTime Date, bool IsOtherMonth, bool IsToday, bool IsSelected, bool IsDisabled)
    {
        public int DayNumber => Date.Day;
    }

    // build 6 weeks (42 cells, starting Monday) for one month
    public static class CalendarMonth
    {
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("id-ID");

        public static List<CalendarDay> Build(DateTime month, DateTime selected, DateTime today, DateTime maximum)
        {
            // Local kind on purpose: Unspecified gets stored as UTC by Supabase (+7h shift)
            var monthStart = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Local);
            int offset = ((int)monthStart.DayOfWeek + 6) % 7; // Senin = 0
            var start = monthStart.AddDays(-offset);

            var result = new List<CalendarDay>(42);
            for (int i = 0; i < 42; i++)
            {
                var t = start.AddDays(i);
                result.Add(new CalendarDay(
                    t,
                    IsOtherMonth: t.Month != monthStart.Month,
                    IsToday: t == today.Date,
                    IsSelected: t == selected.Date,
                    IsDisabled: t > maximum.Date));
            }
            return result;
        }

        public static bool CanGoForward(DateTime month, DateTime maximum) =>
            new DateTime(month.Year, month.Month, 1) < new DateTime(maximum.Year, maximum.Month, 1);

        public static string Title(DateTime month)
        {
            var name = Culture.DateTimeFormat.GetMonthName(month.Month);
            return $"{char.ToUpper(name[0])}{name[1..]} {month.Year}";
        }
    }
}
