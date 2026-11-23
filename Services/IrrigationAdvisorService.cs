using System;
using System.Globalization;
using Lahanku.Models;

namespace Lahanku.Services
{
    // Hitung rekomendasi penyiraman harian dari kebutuhan air tanaman (mm/hari),
    // cuaca sekarang, dan prakiraan hujan 24 jam. Fungsi murni, gampang dites.
    public class IrrigationAdvisorService : IIrrigationAdvisorService
    {
        private const double KebutuhanDefaultMm = 4.5;
        private const double LuasDefaultM2 = 100;
        private const double BatasHujanMm = 3.0;
        private const double BatasPeluang = 60;
        private static readonly CultureInfo Id = CultureInfo.InvariantCulture;

        public Rekomendasi Hitung(Land land, Cuaca? cuaca, PrakiraanHujan? prakiraan, DateTime waktu)
        {
            double luasM2 = land.AreaHectares > 0 ? land.AreaHectares * 10000 : LuasDefaultM2;
            double kebutuhanMm = land.Tanaman?.KebutuhanAirHarian ?? KebutuhanDefaultMm;
            double standar = Math.Round(kebutuhanMm * luasM2, 0);
            double? hujanPrakiraan = prakiraan?.TotalMm;
            double? peluang = prakiraan?.PeluangMaks;

            if (cuaca == null)
            {
                return new Rekomendasi(KeputusanSiram.Siram, standar, standar, luasM2,
                    "Siram dengan takaran standar",
                    "Data cuaca tidak tersedia, gunakan takaran standar.",
                    SaranWaktu(waktu), hujanPrakiraan, peluang);
            }

            if (cuaca.CurahHujan >= BatasHujanMm)
            {
                return Tunda(standar, luasM2,
                    $"Sedang turun hujan sekitar {cuaca.CurahHujan.ToString("0.#", Id)} mm. Kelembapan tanah mencukupi sehingga penyiraman tidak diperlukan.",
                    hujanPrakiraan, peluang);
            }

            if (prakiraan != null && prakiraan.TotalMm >= BatasHujanMm && prakiraan.PeluangMaks >= BatasPeluang)
            {
                string jam = prakiraan.JamMulai.HasValue ? $" sekitar pukul {prakiraan.JamMulai.Value:00}.00" : string.Empty;
                return Tunda(standar, luasM2,
                    $"Hujan sekitar {prakiraan.TotalMm.ToString("0.#", Id)} mm diperkirakan turun{jam} (peluang {prakiraan.PeluangMaks:0}%). Kelembapan tanah diperkirakan mencukupi sehingga penyiraman tidak diperlukan.",
                    hujanPrakiraan, peluang);
            }

            double faktorSuhu = cuaca.Suhu switch
            {
                >= 33.0 => 1.30,
                >= 30.0 => 1.15,
                <= 22.0 => 0.85,
                _ => 1.0
            };

            double batasKering = land.HasIdealMoisture ? land.IdealMoistureMin!.Value : 45.0;
            double batasLembap = land.HasIdealMoisture ? land.IdealMoistureMax!.Value : 80.0;
            double faktorKelembapan = cuaca.Kelembapan <= batasKering ? 1.20
                : cuaca.Kelembapan >= batasLembap ? 0.85
                : 1.0;

            // hujan ringan yang diprediksi ikut mengurangi takaran sesuai peluangnya
            double hujanEfektif = cuaca.CurahHujan + (prakiraan == null ? 0 : prakiraan.TotalMm * prakiraan.PeluangMaks / 100.0);
            double netMm = Math.Max(0.5, kebutuhanMm * faktorSuhu * faktorKelembapan - 0.8 * hujanEfektif);
            double volume = Math.Round(netMm * luasM2, 0);

            string suhu = cuaca.Suhu.ToString("0.#", Id);
            if (cuaca.Suhu >= 32.0 && cuaca.Kelembapan < batasKering)
            {
                return new Rekomendasi(KeputusanSiram.SiramEkstra, volume, standar, luasM2,
                    "Tambah volume penyiraman",
                    $"Suhu {suhu}°C dan kelembapan {cuaca.Kelembapan:0}% berada di bawah batas ideal, sehingga air lebih cepat menguap.",
                    SaranWaktu(waktu), hujanPrakiraan, peluang);
            }

            return new Rekomendasi(KeputusanSiram.Siram, volume, standar, luasM2,
                "Siram sesuai kebutuhan normal",
                $"Kondisi cuaca {cuaca.KondisiCuaca.ToLowerInvariant()} dengan suhu {suhu}°C dan kelembapan {cuaca.Kelembapan:0}%.",
                SaranWaktu(waktu), hujanPrakiraan, peluang);
        }

        public EvaluasiIrigasi CalculateRecommendation(Land land, Cuaca? cuaca)
        {
            var r = Hitung(land, cuaca, null, DateTime.Now);
            return new EvaluasiIrigasi
            {
                IdLahan = land.Id,
                IdCuaca = cuaca?.IdCuaca,
                VolumeAnjuran = r.VolumeAnjuran,
                StatusEfisiensi = r.Judul,
                Saran = string.IsNullOrEmpty(r.SaranWaktu) ? r.Alasan : $"{r.Alasan} {r.SaranWaktu}",
                CreatedAt = DateTime.UtcNow
            };
        }

        private static Rekomendasi Tunda(double standar, double luasM2, string alasan, double? hujanPrakiraan, double? peluang) =>
            new(KeputusanSiram.Tunda, 0, standar, luasM2, "Penyiraman ditunda", alasan, string.Empty, hujanPrakiraan, peluang);

        private static string SaranWaktu(DateTime waktu) =>
            waktu.Hour < 9 ? "Lakukan sebelum pukul 09.00." : "Waktu terbaik setelah pukul 16.00.";
    }
}
