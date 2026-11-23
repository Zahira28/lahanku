using System.Globalization;

namespace Lahanku.Models
{
    // today's recommendation for one land, plus display-ready text for the Dashboard card
    public record LandPlan(Land Land, Weather? Weather, Recommendation Recommendation)
    {
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("id-ID");

        public bool IsPostponed => Recommendation.Decision == WateringDecision.Postpone;

        // "tunda", "ekstra", or "normal" → used to pick the icon & color
        public string Kind => Recommendation.Decision switch
        {
            WateringDecision.Postpone => "tunda",
            WateringDecision.WaterExtra => "ekstra",
            _ => "normal"
        };

        public string VolumeNumber => Recommendation.RecommendedVolume.ToString("#,0", Culture);

        public string Caption => IsPostponed
            ? $"normal {Recommendation.BaselineVolume.ToString("#,0", Culture)} L"
            : $"{Recommendation.RecommendedPerM2.ToString("0.#", Culture)} L/m²";

        public string ShortReason => string.IsNullOrEmpty(Recommendation.TimingAdvice)
            ? Recommendation.Reason
            : $"{Recommendation.Reason} {Recommendation.TimingAdvice}";
    }
}
