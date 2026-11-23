using System;
using System.Globalization;

namespace Lahanku.Models
{
    public enum EvaluationCategory
    {
        IsPostponed,
        Unneeded,
        OnTarget,
        Over,
        Under
    }

    public record EvaluationResult(EvaluationCategory Category, double Difference, string Text)
    {
        public bool MatchesRecommendation => Category is EvaluationCategory.IsPostponed or EvaluationCategory.OnTarget;
    }

    public record EvaluationMessage(string Title, string Body, bool IsWarning);

    // compare the total water used in a day with that day's recommendation
    public static class WateringEvaluator
    {
        public static EvaluationResult Evaluate(WateringDecision decision, double recommended, double actual)
        {
            if (decision == WateringDecision.Postpone)
            {
                return actual <= 0
                    ? new EvaluationResult(EvaluationCategory.IsPostponed, 0, "Ditunda karena hujan")
                    : new EvaluationResult(EvaluationCategory.Unneeded, actual, "Tidak diperlukan");
            }

            // small tolerance: 420 × 1.2 in double can become 503.99999...
            const double eps = 1e-6;

            if (actual > recommended * 1.2 + eps)
            {
                double over = Math.Round(actual - recommended, 0);
                return new EvaluationResult(EvaluationCategory.Over, over, $"Melebihi {FormatLiters(over)}");
            }

            if (actual < recommended * 0.8 - eps)
            {
                double under = Math.Round(recommended - actual, 0);
                return new EvaluationResult(EvaluationCategory.Under, under, $"Kurang {FormatLiters(under)}");
            }

            return new EvaluationResult(EvaluationCategory.OnTarget, Math.Round(Math.Abs(actual - recommended), 0), "Sesuai rekomendasi");
        }

        // toast content after a watering is saved
        public static EvaluationMessage BuildMessage(EvaluationResult result, double baseline, double dayTotal)
        {
            double saved = Math.Round(baseline - dayTotal, 0);
            return result.Category switch
            {
                EvaluationCategory.OnTarget => new EvaluationMessage("Sesuai Rekomendasi",
                    saved > 0 ? $"Sesuai rekomendasi. Lebih hemat {FormatLiters(saved)} dibandingkan penyiraman rutin." : "Sesuai rekomendasi.",
                    false),
                EvaluationCategory.Over => new EvaluationMessage("Melebihi Rekomendasi",
                    $"Penyiraman hari ini melebihi rekomendasi sebanyak {FormatLiters(result.Difference)}.", true),
                EvaluationCategory.Under => new EvaluationMessage("Penyiraman Dicatat",
                    $"Volume hari ini masih kurang {FormatLiters(result.Difference)} dari rekomendasi.", false),
                EvaluationCategory.Unneeded => new EvaluationMessage("Penyiraman Dicatat",
                    "Penyiraman hari ini sebenarnya tidak diperlukan karena hujan diperkirakan turun.", true),
                _ => new EvaluationMessage("Berhasil", "Penyiraman berhasil dicatat.", false)
            };
        }

        private static string FormatLiters(double value) =>
            value.ToString("#,0", CultureInfo.GetCultureInfo("id-ID")) + " L";
    }
}
