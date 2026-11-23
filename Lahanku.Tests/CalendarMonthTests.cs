using Lahanku.Controls;
using Xunit;

namespace Lahanku.Tests
{
    public class CalendarMonthTests
    {
        private static readonly DateTime Today = new(2026, 11, 23);

        [Fact]
        public void Always_42_cells_starting_monday()
        {
            var day = CalendarMonth.Build(new DateTime(2026, 11, 1), Today, Today, Today);
            Assert.Equal(42, day.Count);
            // 1 November 2026 is a Sunday → first cell is Monday 26 October
            Assert.Equal(new DateTime(2026, 10, 26), day[0].Date);
            Assert.Equal(DayOfWeek.Monday, day[0].Date.DayOfWeek);
        }

        [Fact]
        public void Other_month_dates_are_flagged()
        {
            var day = CalendarMonth.Build(new DateTime(2026, 11, 1), Today, Today, Today);
            Assert.True(day[0].IsOtherMonth);
            Assert.False(day.Single(h => h.Date == new DateTime(2026, 11, 15)).IsOtherMonth);
        }

        [Fact]
        public void After_maximum_is_disabled()
        {
            var day = CalendarMonth.Build(new DateTime(2026, 11, 1), Today, Today, Today);
            Assert.False(day.Single(h => h.Date == Today).IsDisabled);
            Assert.True(day.Single(h => h.Date == Today.AddDays(1)).IsDisabled);
        }

        [Fact]
        public void Today_and_selected_are_flagged()
        {
            var selected = new DateTime(2026, 11, 20, 7, 0, 0);
            var day = CalendarMonth.Build(new DateTime(2026, 11, 1), selected, Today, Today);
            Assert.True(day.Single(h => h.Date == Today).IsToday);
            Assert.True(day.Single(h => h.Date == selected.Date).IsSelected);
            Assert.Equal(1, day.Count(h => h.IsSelected));
        }

        [Fact]
        public void Next_month_locked_at_maximum_month()
        {
            Assert.False(CalendarMonth.CanGoForward(new DateTime(2026, 11, 1), Today));
            Assert.True(CalendarMonth.CanGoForward(new DateTime(2026, 10, 1), Today));
        }

        [Fact]
        public void All_dates_have_local_kind()
        {
            // Unspecified dates are treated as UTC by Supabase → +7h shift
            var day = CalendarMonth.Build(new DateTime(2026, 11, 1), Today, Today, Today);
            Assert.All(day, h => Assert.Equal(DateTimeKind.Local, h.Date.Kind));
        }

        [Fact]
        public void Month_title_in_indonesian()
        {
            Assert.Equal("November 2026", CalendarMonth.Title(new DateTime(2026, 11, 1)));
        }
    }
}
