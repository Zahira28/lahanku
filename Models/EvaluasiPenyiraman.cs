using System;
using System.Globalization;

namespace Lahanku.Models
{
    public enum KategoriEvaluasi
    {
        Ditunda,
        TidakDiperlukan,
        Sesuai,
        Melebihi,
        Kurang
    }

    public record HasilEvaluasi(KategoriEvaluasi Kategori, double Selisih, string Teks)
    {
        public bool SesuaiRekomendasi => Kategori is KategoriEvaluasi.Ditunda or KategoriEvaluasi.Sesuai;
    }

    // bandingin total air yang disiram dalam sehari dengan anjuran hari itu
    public static class EvaluasiPenyiraman
    {
        public static HasilEvaluasi Nilai(KeputusanSiram keputusan, double anjuran, double aktual)
        {
            if (keputusan == KeputusanSiram.Tunda)
            {
                return aktual <= 0
                    ? new HasilEvaluasi(KategoriEvaluasi.Ditunda, 0, "Ditunda karena hujan")
                    : new HasilEvaluasi(KategoriEvaluasi.TidakDiperlukan, aktual, "Tidak diperlukan");
            }

            // toleransi kecil: 420 × 1.2 di double bisa jadi 503.99999...
            const double eps = 1e-6;

            if (aktual > anjuran * 1.2 + eps)
            {
                double lebih = Math.Round(aktual - anjuran, 0);
                return new HasilEvaluasi(KategoriEvaluasi.Melebihi, lebih, $"Melebihi {Liter(lebih)}");
            }

            if (aktual < anjuran * 0.8 - eps)
            {
                double kurang = Math.Round(anjuran - aktual, 0);
                return new HasilEvaluasi(KategoriEvaluasi.Kurang, kurang, $"Kurang {Liter(kurang)}");
            }

            return new HasilEvaluasi(KategoriEvaluasi.Sesuai, Math.Round(Math.Abs(aktual - anjuran), 0), "Sesuai rekomendasi");
        }

        private static string Liter(double value) =>
            value.ToString("#,0", CultureInfo.GetCultureInfo("id-ID")) + " L";
    }
}
