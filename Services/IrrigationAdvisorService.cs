using System;
using Lahanku.Models;

namespace Lahanku.Services
{
    /// <summary>
    /// Layanan kalkulasi kebutuhan air dan rekomendasi irigasi cerdas (SRP & DIP).
    /// Mengimplementasikan pedoman FAO 56 Evapotranspirasi dan SDG 13 (Climate Action).
    /// </summary>
    public class IrrigationAdvisorService : IIrrigationAdvisorService
    {
        public EvaluasiIrigasi CalculateRecommendation(Land land, Cuaca? cuaca)
        {
            // Luas lahan dalam meter persegi (1 Hektar = 10.000 m²)
            // Jika luas belum diisi/0, asumsikan lahan pekarangan urban farming 100 m² (0.01 Ha)
            double areaM2 = land.AreaHectares > 0 ? land.AreaHectares * 10000 : 100;

            // Standar kebutuhan air harian tanaman (mm/hari) dari database FAO 56
            double kebutuhanAirMm = land.Tanaman?.KebutuhanAirHarian ?? 4.5;
            string cropName = land.CropType;

            double volumeAnjuran;
            string status;
            string saran;

            if (cuaca == null)
            {
                volumeAnjuran = Math.Round(kebutuhanAirMm * areaM2, 0);
                status = "Estimasi Standar";
                saran = $"Kebutuhan air standar untuk tanaman {cropName} adalah {volumeAnjuran:N0} Liter/hari (data cuaca offline).";
            }
            else if (cuaca.CurahHujan >= 3.0)
            {
                // Terdeteksi hujan signifikan (>= 3 mm) -> Tunda penyiraman untuk konservasi air
                volumeAnjuran = 0;
                status = "Tunda Penyiraman (Hemat Air)";
                saran = $"Terdeteksi curah hujan {cuaca.CurahHujan.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)} mm dan kelembapan udara {cuaca.Kelembapan:0}%. Tanah sudah cukup basah. Tunda penyiraman hari ini untuk menghemat air dan mencegah pembusukan akar (SDG 13).";
            }
            else
            {
                // Penyesuaian faktor suhu (°C)
                double tempFactor = cuaca.Suhu switch
                {
                    >= 33.0 => 1.30,
                    >= 30.0 => 1.15,
                    <= 22.0 => 0.85,
                    _ => 1.0
                };

                // Penyesuaian faktor kelembapan udara (%)
                double humFactor = cuaca.Kelembapan switch
                {
                    <= 45.0 => 1.20,
                    >= 80.0 => 0.85,
                    _ => 1.0
                };

                // Pengurangan dari curah hujan ringan jika ada
                double rainDeduction = cuaca.CurahHujan * 0.8;
                double netWaterMm = Math.Max(0.5, (kebutuhanAirMm * tempFactor * humFactor) - rainDeduction);

                volumeAnjuran = Math.Round(netWaterMm * areaM2, 0);

                if (cuaca.Suhu >= 32.0 && cuaca.Kelembapan <= 55.0)
                {
                    status = "Perlu Penyiraman Ekstra";
                    saran = $"Suhu lingkungan terik ({cuaca.Suhu.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)}°C) dengan kelembapan rendah ({cuaca.Kelembapan:0}%). Disarankan menyiram sekitar {volumeAnjuran:N0} Liter pada pagi atau sore hari agar air tidak cepat menguap.";
                }
                else
                {
                    status = "Optimal";
                    saran = $"Kondisi cuaca {cuaca.KondisiCuaca.ToLower()} (Suhu {cuaca.Suhu.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)}°C, Kelembapan {cuaca.Kelembapan:0}%). Rekomendasi volume air harian untuk tanaman {cropName} adalah {volumeAnjuran:N0} Liter.";
                }
            }

            return new EvaluasiIrigasi
            {
                IdLahan = land.Id,
                IdCuaca = cuaca?.IdCuaca,
                VolumeAnjuran = volumeAnjuran,
                StatusEfisiensi = status,
                Saran = saran,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
