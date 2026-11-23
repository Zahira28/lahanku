using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Lahanku.Models
{
    // satu baris di tabel riwayat penyiraman: log asli, atau hari yang ditunda tanpa penyiraman
    public class BarisRiwayat
    {
        private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

        public DateTime Tanggal { get; init; }
        public string TanggalText { get; init; } = string.Empty;
        public string VolumeText { get; init; } = string.Empty;
        public string RekomendasiText { get; init; } = "-";
        public HasilEvaluasi? Evaluasi { get; init; }
        public string CatatanText { get; init; } = string.Empty;
        public bool CatatanKosong { get; init; }
        public IrrigationLog? Log { get; init; }

        public bool BisaDiubah => Log != null;
        public string EvaluasiText => Evaluasi?.Teks ?? "-";

        // "ok", "warn", "skip", atau "none" → dipakai buat warna teks evaluasi
        public string EvaluasiJenis => Evaluasi?.Kategori switch
        {
            KategoriEvaluasi.Sesuai => "ok",
            KategoriEvaluasi.Ditunda => "skip",
            null => "none",
            _ => "warn"
        };

        public static List<BarisRiwayat> Susun(IEnumerable<IrrigationLog> logs, IEnumerable<EvaluasiIrigasi> catatan)
        {
            var catatanPerHari = catatan
                .GroupBy(c => c.Tanggal.Date)
                .ToDictionary(g => g.Key, g => g.First());

            var logList = logs.ToList();
            var totalPerHari = logList
                .GroupBy(l => l.Date.Date)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.VolumeLiters));

            var rows = new List<BarisRiwayat>();

            foreach (var log in logList)
            {
                catatanPerHari.TryGetValue(log.Date.Date, out var c);
                bool kosong = string.IsNullOrWhiteSpace(log.Notes);
                rows.Add(new BarisRiwayat
                {
                    Tanggal = log.Date,
                    TanggalText = log.Date.ToString("dd MMM yyyy, HH:mm", Id),
                    VolumeText = Liter(log.VolumeLiters),
                    RekomendasiText = TeksRekomendasi(c),
                    Evaluasi = c == null ? null : Nilai(c, totalPerHari[log.Date.Date]),
                    CatatanText = kosong ? "Tidak ada catatan" : log.Notes!.Trim(),
                    CatatanKosong = kosong,
                    Log = log
                });
            }

            // hari yang ditunda dan memang nggak disiram juga ditampilkan
            foreach (var (hari, c) in catatanPerHari)
            {
                if (KeputusanSiramExtensions.FromDb(c.Keputusan) != KeputusanSiram.Tunda || totalPerHari.ContainsKey(hari))
                {
                    continue;
                }

                rows.Add(new BarisRiwayat
                {
                    Tanggal = hari,
                    TanggalText = hari.ToString("dd MMM yyyy", Id),
                    VolumeText = "0 L",
                    RekomendasiText = "Tunda",
                    Evaluasi = Nilai(c, 0),
                    CatatanText = "Tidak ada catatan",
                    CatatanKosong = true
                });
            }

            return rows.OrderByDescending(r => r.Tanggal).ToList();
        }

        private static HasilEvaluasi Nilai(EvaluasiIrigasi c, double totalHariItu) =>
            EvaluasiPenyiraman.Nilai(KeputusanSiramExtensions.FromDb(c.Keputusan), c.VolumeAnjuran, totalHariItu);

        private static string TeksRekomendasi(EvaluasiIrigasi? c) =>
            c == null ? "-"
            : KeputusanSiramExtensions.FromDb(c.Keputusan) == KeputusanSiram.Tunda ? "Tunda"
            : Liter(c.VolumeAnjuran);

        private static string Liter(double value) => value.ToString("#,0.##", Id) + " L";
    }
}
