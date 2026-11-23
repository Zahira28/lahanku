using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Lahanku.Controls
{
    // Pemilih tanggal + jam buatan sendiri. Tanggal setelah Maximum (default: sekarang) nggak bisa dipilih.
    public partial class TanggalPicker : UserControl
    {
        private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value), typeof(DateTime), typeof(TanggalPicker),
            new FrameworkPropertyMetadata(DateTime.Now, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((TanggalPicker)d).Tampilkan()));

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            nameof(Maximum), typeof(DateTime?), typeof(TanggalPicker),
            new PropertyMetadata(null, (d, _) => ((TanggalPicker)d).Tampilkan()));

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

        private DateTime _bulan;
        private bool _lagiNulisWaktu;

        private DateTime Maks => Maximum ?? DateTime.Now;

        public TanggalPicker()
        {
            InitializeComponent();
            _bulan = AwalBulan(Value);
            Loaded += (_, _) => { _bulan = AwalBulan(Value); Tampilkan(); };
        }

        private static DateTime AwalBulan(DateTime t) => new(t.Year, t.Month, 1);

        // semua perubahan nilai lewat sini biar nggak pernah lewat dari Maximum
        private void Atur(DateTime nilai)
        {
            nilai = Helpers.WaktuLokal.Pastikan(nilai);
            Value = nilai > Maks ? Maks : nilai;
        }

        private void Tampilkan()
        {
            if (TanggalText == null)
            {
                return;
            }

            var nilai = Value;
            TanggalText.Text = nilai.ToString("dddd, d MMMM yyyy", Id);

            if (!_lagiNulisWaktu)
            {
                JamBox.Text = nilai.Hour.ToString("00");
                MenitBox.Text = nilai.Minute.ToString("00");
            }

            var hariIni = DateTime.Today;
            ChipHariIni.IsChecked = nilai.Date == hariIni;
            ChipKemarin.IsChecked = nilai.Date == hariIni.AddDays(-1);
            ChipLusa.IsChecked = nilai.Date == hariIni.AddDays(-2);

            JudulBulan.Text = KalenderBulan.Judul(_bulan);
            TombolMaju.IsEnabled = KalenderBulan.BolehMaju(_bulan, Maks);
            DaftarHari.ItemsSource = KalenderBulan.Susun(_bulan, nilai, hariIni, Maks);
        }

        private void Chip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton chip && int.TryParse(chip.Tag?.ToString(), out int mundur))
            {
                Atur(DateTime.Today.AddDays(-mundur) + Value.TimeOfDay);
                _bulan = AwalBulan(Value);
                Tampilkan();
            }
        }

        private void FieldTanggal_Click(object sender, MouseButtonEventArgs e)
        {
            _bulan = AwalBulan(Value);
            Tampilkan();
            KalenderPopup.IsOpen = !KalenderPopup.IsOpen;
        }

        private void Hari_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: DateTime tanggal })
            {
                Atur(tanggal.Date + Value.TimeOfDay);
                KalenderPopup.IsOpen = false;
            }
        }

        private void Mundur_Click(object sender, RoutedEventArgs e)
        {
            _bulan = _bulan.AddMonths(-1);
            Tampilkan();
        }

        private void Maju_Click(object sender, RoutedEventArgs e)
        {
            if (KalenderBulan.BolehMaju(_bulan, Maks))
            {
                _bulan = _bulan.AddMonths(1);
                Tampilkan();
            }
        }

        // jam/menit langsung dipakai begitu angkanya valid, jadi Enter di form tetap dapat nilai terbaru
        private void Waktu_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (JamBox == null || MenitBox == null)
            {
                return;
            }

            if (int.TryParse(JamBox.Text, out int jam) && jam is >= 0 and <= 23 &&
                int.TryParse(MenitBox.Text, out int menit) && menit is >= 0 and <= 59)
            {
                _lagiNulisWaktu = true;
                Atur(Value.Date.AddHours(jam).AddMinutes(menit));
                _lagiNulisWaktu = false;

                // typed a future time and it got clamped → show the real time instead of the typed one
                if (Value.Hour != jam || Value.Minute != menit)
                {
                    JamBox.Text = Value.Hour.ToString("00");
                    MenitBox.Text = Value.Minute.ToString("00");
                }
            }
        }
    }
}
