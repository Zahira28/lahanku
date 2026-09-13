using System;
using System.Globalization;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Lahanku.Models
{
    [Table("penyiraman")]
    public class IrrigationLog : BaseModel
    {
        [PrimaryKey("id_penyiraman", false)]
        public long Id { get; set; }

        [Column("id_lahan")]
        public long LandId { get; set; }

        [Column("tanggal_waktu")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Column("volume_liter")]
        public double VolumeLiters { get; set; }

        [Column("catatan")]
        public string? Notes { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // UI-friendly formatted properties
        [JsonIgnore]
        public string FormattedDate => Date.ToString("dd MMM yyyy, HH:mm");

        [JsonIgnore]
        public string FormattedDateOnly => Date.ToString("dd MMMM yyyy");

        [JsonIgnore]
        public string FormattedVolume
        {
            get
            {
                if (VolumeLiters < 1.0 && VolumeLiters > 0)
                {
                    double ml = VolumeLiters * 1000.0;
                    return $"{ml.ToString("0.##", CultureInfo.InvariantCulture)} mL";
                }
                return $"{VolumeLiters.ToString("0.##", CultureInfo.InvariantCulture)} Liter";
            }
        }
    }
}
