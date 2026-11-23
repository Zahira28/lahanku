using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lahanku.Controls
{
    public record HariKalender(DateTime Tanggal, bool BulanLain, bool HariIni, bool Terpilih, bool Nonaktif)
    {
        public int Angka => Tanggal.Day;
    }

    // susun 6 minggu (42 kotak, mulai Senin) buat satu bulan
    public static class KalenderBulan
    {
        private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

        public static List<HariKalender> Susun(DateTime bulan, DateTime terpilih, DateTime hariIni, DateTime maksimum)
        {
            // Local kind on purpose: Unspecified gets stored as UTC by Supabase (+7h shift)
            var awalBulan = new DateTime(bulan.Year, bulan.Month, 1, 0, 0, 0, DateTimeKind.Local);
            int geser = ((int)awalBulan.DayOfWeek + 6) % 7; // Senin = 0
            var mulai = awalBulan.AddDays(-geser);

            var hasil = new List<HariKalender>(42);
            for (int i = 0; i < 42; i++)
            {
                var t = mulai.AddDays(i);
                hasil.Add(new HariKalender(
                    t,
                    BulanLain: t.Month != awalBulan.Month,
                    HariIni: t == hariIni.Date,
                    Terpilih: t == terpilih.Date,
                    Nonaktif: t > maksimum.Date));
            }
            return hasil;
        }

        public static bool BolehMaju(DateTime bulan, DateTime maksimum) =>
            new DateTime(bulan.Year, bulan.Month, 1) < new DateTime(maksimum.Year, maksimum.Month, 1);

        public static string Judul(DateTime bulan)
        {
            var nama = Id.DateTimeFormat.GetMonthName(bulan.Month);
            return $"{char.ToUpper(nama[0])}{nama[1..]} {bulan.Year}";
        }
    }
}
