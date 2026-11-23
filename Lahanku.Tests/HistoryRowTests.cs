using Lahanku.Models;
using Xunit;

namespace Lahanku.Tests
{
    public class HistoryRowTests
    {
        private static readonly DateTime Monday = new(2026, 11, 16);
        private static readonly DateTime Tuesday = new(2026, 11, 17);

        private static DailyRecommendation MakeRecord(DateTime date, double recommended, string decision = "siram") =>
            new() { LandId = 1, Date = date, RecommendedVolume = recommended, BaselineVolume = 480, Decision = decision };

        private static IrrigationLog Log(DateTime time, double liters, string? note = "Pagi") =>
            new() { LandId = 1, Date = time, VolumeLiters = liters, Notes = note };

        [Fact]
        public void Log_without_record_has_no_evaluation()
        {
            var rows = HistoryRow.Build(new[] { Log(Monday.AddHours(7), 300) }, Array.Empty<DailyRecommendation>());
            var r = Assert.Single(rows);
            Assert.Equal("-", r.RecommendationText);
            Assert.Null(r.Evaluation);
            Assert.Equal("-", r.EvaluationText);
        }

        [Fact]
        public void Log_on_target()
        {
            var rows = HistoryRow.Build(new[] { Log(Monday.AddHours(7), 460) }, new[] { MakeRecord(Monday, 450) });
            var r = Assert.Single(rows);
            Assert.Equal("450 L", r.RecommendationText);
            Assert.Equal(EvaluationCategory.OnTarget, r.Evaluation!.Category);
            Assert.Equal("ok", r.EvaluationKind);
        }

        [Fact]
        public void Evaluation_uses_daily_total()
        {
            var rows = HistoryRow.Build(
                new[] { Log(Monday.AddHours(7), 200), Log(Monday.AddHours(16), 400) },
                new[] { MakeRecord(Monday, 420) });

            Assert.Equal(2, rows.Count);
            Assert.All(rows, r => Assert.Equal("Melebihi 180 L", r.EvaluationText));
            Assert.All(rows, r => Assert.Equal("warn", r.EvaluationKind));
        }

        [Fact]
        public void Postponed_day_without_log_gets_own_row()
        {
            var rows = HistoryRow.Build(Array.Empty<IrrigationLog>(), new[] { MakeRecord(Tuesday, 0, "tunda") });
            var r = Assert.Single(rows);
            Assert.Null(r.Log);
            Assert.False(r.CanEdit);
            Assert.Equal("0 L", r.VolumeText);
            Assert.Equal("Tunda", r.RecommendationText);
            Assert.Equal("Ditunda karena hujan", r.EvaluationText);
            Assert.Equal("skip", r.EvaluationKind);
        }

        [Fact]
        public void Empty_notes_are_flagged()
        {
            var rows = HistoryRow.Build(new[] { Log(Monday.AddHours(7), 300, "  ") }, Array.Empty<DailyRecommendation>());
            Assert.Equal("Tidak ada catatan", rows[0].NotesText);
            Assert.True(rows[0].NotesEmpty);
        }

        [Fact]
        public void Newest_first()
        {
            var rows = HistoryRow.Build(
                new[] { Log(Monday.AddHours(7), 300), Log(Monday.AddHours(16), 100) },
                new[] { MakeRecord(Tuesday, 0, "tunda") });

            Assert.Equal(Tuesday, rows[0].Date.Date);
            Assert.Equal(16, rows[1].Date.Hour);
            Assert.Equal(7, rows[2].Date.Hour);
        }
    }
}
