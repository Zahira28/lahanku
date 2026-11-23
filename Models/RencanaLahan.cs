using System.Globalization;

namespace Lahanku.Models
{
    // rekomendasi hari ini untuk satu lahan, plus teks siap tampil buat kartu Dashboard
    public record RencanaLahan(Land Land, Cuaca? Cuaca, Rekomendasi Rekomendasi)
    {
        private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

        public bool Ditunda => Rekomendasi.Keputusan == KeputusanSiram.Tunda;

        // "tunda", "ekstra", atau "normal" → dipakai buat pilih ikon & warna
        public string Jenis => Rekomendasi.Keputusan switch
        {
            KeputusanSiram.Tunda => "tunda",
            KeputusanSiram.SiramEkstra => "ekstra",
            _ => "normal"
        };

        public string VolumeAngka => Rekomendasi.VolumeAnjuran.ToString("#,0", Id);

        public string Keterangan => Ditunda
            ? $"normal {Rekomendasi.VolumeStandar.ToString("#,0", Id)} L"
            : $"{Rekomendasi.AnjuranPerM2.ToString("0.#", Id)} L/m²";

        public string AlasanSingkat => string.IsNullOrEmpty(Rekomendasi.SaranWaktu)
            ? Rekomendasi.Alasan
            : $"{Rekomendasi.Alasan} {Rekomendasi.SaranWaktu}";
    }
}
