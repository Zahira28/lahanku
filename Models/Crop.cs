using System;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Lahanku.Models
{
    [Table("tanaman")]
    public class Crop : BaseModel
    {
        [PrimaryKey("id_tanaman", false)]
        public long Id { get; set; }

        [Column("nama_tanaman")]
        public string Name { get; set; } = string.Empty;

        [Column("varietas")]
        public string? Variety { get; set; }

        [Column("kelembapan_ideal_min")]
        public double IdealHumidityMin { get; set; }

        [Column("kelembapan_ideal_max")]
        public double IdealHumidityMax { get; set; }

        [Column("kebutuhan_air_harian")]
        public double DailyWaterNeedMm { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Display name combining crop name and variety for the UI dropdown.
        /// </summary>
        [JsonIgnore]
        public string DisplayName => string.IsNullOrWhiteSpace(Variety)
            ? Name
            : $"{Name} ({Variety})";

        [JsonIgnore]
        public string HumidityRangeText => $"{IdealHumidityMin:0}% - {IdealHumidityMax:0}%";

        [JsonIgnore]
        public string DailyWaterNeedText => $"{DailyWaterNeedMm:0.0} mm/hari";
    }
}
