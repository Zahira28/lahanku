using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lahanku.Models
{
    public enum KeputusanSiram
    {
        Siram,
        Tunda,
        SiramEkstra
    }

    public static class KeputusanSiramExtensions
    {
        public static string ToDb(this KeputusanSiram keputusan) => keputusan switch
        {
            KeputusanSiram.Tunda => "tunda",
            KeputusanSiram.SiramEkstra => "siram_ekstra",
            _ => "siram"
        };

        public static KeputusanSiram FromDb(string? value) => value switch
        {
            "tunda" => KeputusanSiram.Tunda,
            "siram_ekstra" => KeputusanSiram.SiramEkstra,
            _ => KeputusanSiram.Siram
        };
    }

    // ringkasan prakiraan hujan 24 jam ke depan
    public record PrakiraanHujan(double TotalMm, double PeluangMaks, int? JamMulai)
    {
        // rangkum data per jam dari Open-Meteo (null dianggap 0)
        public static PrakiraanHujan DariPerJam(IReadOnlyList<string> waktu, IReadOnlyList<double?> hujanMm, IReadOnlyList<double?> peluang)
        {
            double total = 0, peluangMaks = 0;
            int? jamMulai = null;
            int n = Math.Min(waktu.Count, Math.Min(hujanMm.Count, peluang.Count));

            for (int i = 0; i < n; i++)
            {
                double mm = hujanMm[i] ?? 0;
                double p = peluang[i] ?? 0;
                total += mm;
                peluangMaks = Math.Max(peluangMaks, p);

                if (jamMulai == null && mm >= 0.5 && p >= 50 &&
                    DateTime.TryParse(waktu[i], CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
                {
                    jamMulai = t.Hour;
                }
            }

            return new PrakiraanHujan(Math.Round(total, 1), peluangMaks, jamMulai);
        }
    }

    public record Rekomendasi(
        KeputusanSiram Keputusan,
        double VolumeAnjuran,
        double VolumeStandar,
        double LuasM2,
        string Judul,
        string Alasan,
        string SaranWaktu,
        double? HujanPrakiraanMm,
        double? PeluangHujan)
    {
        // 1 mm air di atas 1 m² = 1 liter
        public double AnjuranPerM2 => LuasM2 > 0 ? VolumeAnjuran / LuasM2 : 0;
    }
}
