using System;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Lahanku.Models
{
    [Table("cuaca")]
    public class Weather : BaseModel
    {
        [PrimaryKey("id_cuaca", false)]
        public long WeatherId { get; set; }

        [Column("id_lahan")]
        public long? LandId { get; set; }

        [Column("suhu")]
        public double Temperature { get; set; }

        [Column("kelembapan")]
        public double Humidity { get; set; }

        [Column("curah_hujan")]
        public double Rainfall { get; set; }

        [Column("kondisi_cuaca")]
        public string Condition { get; set; } = "Cerah";

        [Column("waktu_cek")]
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

        // WMO code from Open-Meteo, only used to pick the icon (not stored in the DB)
        [JsonIgnore]
        public int WeatherCode { get; set; }

        [JsonIgnore]
        public string Summary => $"{Condition}, {Temperature:0.0}°C (Kelembapan {Humidity:0}%)";
    }
}
