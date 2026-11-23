using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Lahanku.Models
{
    [Table("evaluasi_irigasi")]
    public class EvaluasiIrigasi : BaseModel
    {
        [PrimaryKey("id_evaluasi", false)]
        public long IdEvaluasi { get; set; }

        [Column("id_lahan")]
        public long IdLahan { get; set; }

        [Column("id_penyiraman")]
        public long? IdPenyiraman { get; set; }

        [Column("id_cuaca")]
        public long? IdCuaca { get; set; }

        [Column("tanggal")]
        public DateTime Tanggal { get; set; }

        // "siram", "tunda", atau "siram_ekstra" (lihat KeputusanSiramExtensions)
        [Column("keputusan")]
        public string Keputusan { get; set; } = "siram";

        // patokan penyiraman rutin tanpa melihat cuaca
        [Column("volume_standar")]
        public double VolumeStandar { get; set; }

        [Column("hujan_prakiraan_mm")]
        public double? HujanPrakiraanMm { get; set; }

        [Column("peluang_hujan")]
        public double? PeluangHujan { get; set; }

        [Column("volume_anjuran")]
        public double VolumeAnjuran { get; set; }

        [Column("status_efisiensi")]
        public string StatusEfisiensi { get; set; } = "Optimal";

        [Column("saran")]
        public string? Saran { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
