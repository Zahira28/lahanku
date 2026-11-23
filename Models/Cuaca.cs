using System;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Lahanku.Models
{
    [Table("cuaca")]
    public class Cuaca : BaseModel
    {
        [PrimaryKey("id_cuaca", false)]
        public long IdCuaca { get; set; }

        [Column("id_lahan")]
        public long? IdLahan { get; set; }

        [Column("suhu")]
        public double Suhu { get; set; }

        [Column("kelembapan")]
        public double Kelembapan { get; set; }

        [Column("curah_hujan")]
        public double CurahHujan { get; set; }

        [Column("kondisi_cuaca")]
        public string KondisiCuaca { get; set; } = "Cerah";

        [Column("waktu_cek")]
        public DateTime WaktuCek { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public string RingkasanCuaca => $"{KondisiCuaca}, {Suhu:0.0}°C (Kelembapan {Kelembapan:0}%)";
    }
}
