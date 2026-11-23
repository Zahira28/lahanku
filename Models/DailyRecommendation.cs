using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Lahanku.Models
{
    [Table("evaluasi_irigasi")]
    public class DailyRecommendation : BaseModel
    {
        [PrimaryKey("id_evaluasi", false)]
        public long Id { get; set; }

        [Column("id_lahan")]
        public long LandId { get; set; }

        [Column("id_penyiraman")]
        public long? IrrigationLogId { get; set; }

        [Column("id_cuaca")]
        public long? WeatherId { get; set; }

        [Column("tanggal")]
        public DateTime Date { get; set; }

        // "siram", "tunda", or "siram_ekstra" (see WateringDecisionExtensions)
        [Column("keputusan")]
        public string Decision { get; set; } = "siram";

        // baseline routine watering, ignoring the weather
        [Column("volume_standar")]
        public double BaselineVolume { get; set; }

        [Column("hujan_prakiraan_mm")]
        public double? ForecastRainMm { get; set; }

        [Column("peluang_hujan")]
        public double? RainProbability { get; set; }

        [Column("volume_anjuran")]
        public double RecommendedVolume { get; set; }

        [Column("status_efisiensi")]
        public string StatusLabel { get; set; } = "Optimal";

        [Column("saran")]
        public string? Advice { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
