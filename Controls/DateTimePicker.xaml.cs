using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Lahanku.Controls
{
    // Custom date + time picker. Dates after Maximum (default: now) can't be picked.
    public partial class DateTimePicker : UserControl
    {
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("id-ID");

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value), typeof(DateTime), typeof(DateTimePicker),
            new FrameworkPropertyMetadata(DateTime.Now, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((DateTimePicker)d).Render()));

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            nameof(Maximum), typeof(DateTime?), typeof(DateTimePicker),
            new PropertyMetadata(null, (d, _) => ((DateTimePicker)d).Render()));

        public DateTime Value
        {
            get => (DateTime)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public DateTime? Maximum
        {
            get => (DateTime?)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        private DateTime _month;
        private bool _isTypingTime;

        private DateTime EffectiveMax => Maximum ?? DateTime.Now;

        public DateTimePicker()
        {
            InitializeComponent();
            _month = FirstOfMonth(Value);
            Loaded += (_, _) => { _month = FirstOfMonth(Value); Render(); };
        }

        private static DateTime FirstOfMonth(DateTime t) => new(t.Year, t.Month, 1);

        // every value change goes through here so it never goes past Maximum
        private void SetClamped(DateTime value)
        {
            value = Helpers.LocalTime.Ensure(value);
            Value = value > EffectiveMax ? EffectiveMax : value;
        }

        private void Render()
        {
            if (DateText == null)
            {
                return;
            }

            var value = Value;
            DateText.Text = value.ToString("dddd, d MMMM yyyy", Culture);

            if (!_isTypingTime)
            {
                HourBox.Text = value.Hour.ToString("00");
                MinuteBox.Text = value.Minute.ToString("00");
            }

            var today = DateTime.Today;
            ChipToday.IsChecked = value.Date == today;
            ChipYesterday.IsChecked = value.Date == today.AddDays(-1);
            ChipTwoDaysAgo.IsChecked = value.Date == today.AddDays(-2);

            MonthTitle.Text = CalendarMonth.Title(_month);
            NextButton.IsEnabled = CalendarMonth.CanGoForward(_month, EffectiveMax);
            DayList.ItemsSource = CalendarMonth.Build(_month, value, today, EffectiveMax);
        }

        private void Chip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton chip && int.TryParse(chip.Tag?.ToString(), out int daysBack))
            {
                SetClamped(DateTime.Today.AddDays(-daysBack) + Value.TimeOfDay);
                _month = FirstOfMonth(Value);
                Render();
            }
        }

        private void DateField_Click(object sender, MouseButtonEventArgs e)
        {
            _month = FirstOfMonth(Value);
            Render();
            CalendarPopup.IsOpen = !CalendarPopup.IsOpen;
        }

        private void Day_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: DateTime date })
            {
                SetClamped(date.Date + Value.TimeOfDay);
                CalendarPopup.IsOpen = false;
            }
        }

        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            _month = _month.AddMonths(-1);
            Render();
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            if (CalendarMonth.CanGoForward(_month, EffectiveMax))
            {
                _month = _month.AddMonths(1);
                Render();
            }
        }

        // hour/minute is applied as soon as the number is valid, so pressing Enter in the form still gets the latest value
        private void Time_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (HourBox == null || MinuteBox == null)
            {
                return;
            }

            if (int.TryParse(HourBox.Text, out int hour) && hour is >= 0 and <= 23 &&
                int.TryParse(MinuteBox.Text, out int minute) && minute is >= 0 and <= 59)
            {
                _isTypingTime = true;
                SetClamped(Value.Date.AddHours(hour).AddMinutes(minute));
                _isTypingTime = false;

                // typed a future time and it got clamped → show the real time instead of the typed one
                if (Value.Hour != hour || Value.Minute != minute)
                {
                    HourBox.Text = Value.Hour.ToString("00");
                    MinuteBox.Text = Value.Minute.ToString("00");
                }
            }
        }
    }
}
