using Lahanku.Models;
using Xunit;

namespace Lahanku.Tests
{
    public class WateringEvaluatorTests
    {
        [Fact]
        public void Postponed_without_watering_is_rain_postponed()
        {
            var h = WateringEvaluator.Evaluate(WateringDecision.Postpone, 0, 0);
            Assert.Equal(EvaluationCategory.IsPostponed, h.Category);
            Assert.Equal("Ditunda karena hujan", h.Text);
            Assert.True(h.MatchesRecommendation);
        }

        [Fact]
        public void Postponed_but_watered_is_unneeded()
        {
            var h = WateringEvaluator.Evaluate(WateringDecision.Postpone, 0, 200);
            Assert.Equal(EvaluationCategory.Unneeded, h.Category);
            Assert.Equal(200, h.Difference);
            Assert.False(h.MatchesRecommendation);
        }

        [Theory]
        [InlineData(420, 336)]   // 80% batas bawah
        [InlineData(420, 504)]   // 120% batas atas
        [InlineData(450, 460)]
        public void Within_80_to_120_percent_is_on_target(double recommended, double actual)
        {
            var h = WateringEvaluator.Evaluate(WateringDecision.Water, recommended, actual);
            Assert.Equal(EvaluationCategory.OnTarget, h.Category);
            Assert.Equal("Sesuai rekomendasi", h.Text);
            Assert.True(h.MatchesRecommendation);
        }

        [Fact]
        public void Above_120_percent_is_over()
        {
            var h = WateringEvaluator.Evaluate(WateringDecision.Water, 420, 700);
            Assert.Equal(EvaluationCategory.Over, h.Category);
            Assert.Equal(280, h.Difference);
            Assert.Equal("Melebihi 280 L", h.Text);
        }

        [Fact]
        public void On_target_message_mentions_savings()
        {
            var h = WateringEvaluator.Evaluate(WateringDecision.Water, 450, 460);
            var p = WateringEvaluator.BuildMessage(h, baseline: 480, dayTotal: 460);
            Assert.False(p.IsWarning);
            Assert.Equal("Sesuai rekomendasi. Lebih hemat 20 L dibandingkan penyiraman rutin.", p.Body);
        }

        [Fact]
        public void Over_message_is_a_warning()
        {
            var h = WateringEvaluator.Evaluate(WateringDecision.Water, 420, 700);
            var p = WateringEvaluator.BuildMessage(h, baseline: 480, dayTotal: 700);
            Assert.True(p.IsWarning);
            Assert.Contains("280 L", p.Body);
        }

        [Fact]
        public void Unneeded_message_when_rain()
        {
            var h = WateringEvaluator.Evaluate(WateringDecision.Postpone, 0, 100);
            var p = WateringEvaluator.BuildMessage(h, baseline: 480, dayTotal: 100);
            Assert.True(p.IsWarning);
            Assert.Contains("tidak diperlukan", p.Body);
        }

        [Fact]
        public void Below_80_percent_is_under()
        {
            var h = WateringEvaluator.Evaluate(WateringDecision.WaterExtra, 500, 300);
            Assert.Equal(EvaluationCategory.Under, h.Category);
            Assert.Equal(200, h.Difference);
            Assert.Equal("Kurang 200 L", h.Text);
            Assert.False(h.MatchesRecommendation);
        }
    }
}
