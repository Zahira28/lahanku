using System;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Lahanku.Models
{
    [Table("tanaman")]
    public class Tanaman : BaseModel
    {
        [PrimaryKey("id_tanaman", false)]
        public long IdTanaman { get; set; }

        [Column("nama_tanaman")]
        public string NamaTanaman { get; set; } = string.Empty;

        [Column("varietas")]
        public string? Varietas { get; set; }

        [Column("kelembapan_ideal_min")]
        public double KelembapanIdealMin { get; set; }

        [Column("kelembapan_ideal_max")]
        public double KelembapanIdealMax { get; set; }

        [Column("kebutuhan_air_harian")]
        public double KebutuhanAirHarian { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Nama tampilan gabungan nama tanaman dan varietas untuk UI Dropdown.
        /// </summary>
        [JsonIgnore]
        public string DisplayName => string.IsNullOrWhiteSpace(Varietas)
            ? NamaTanaman
            : $"{NamaTanaman} ({Varietas})";

        [JsonIgnore]
        public string KelembapanRangeText => $"{KelembapanIdealMin:0}% - {KelembapanIdealMax:0}%";

        [JsonIgnore]
        public string KebutuhanAirText => $"{KebutuhanAirHarian:0.0} mm/hari";
    }
}
