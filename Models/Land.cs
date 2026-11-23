using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Lahanku.Models
{
    [Table("lahan")]
    public class Land : BaseModel
    {
        [PrimaryKey("id_lahan", false)]
        public long Id { get; set; }

        [Column("owner_id")]
        public Guid? OwnerId { get; set; }

        [Column("id_tanaman")]
        public long? TanamanId { get; set; }

        [Column("nama_lahan")]
        public string Name { get; set; } = string.Empty;

        [Column("lokasi_deskripsi")]
        public string LocationDescription { get; set; } = string.Empty;

        [Column("latitude")]
        public double Latitude { get; set; }

        [Column("longitude")]
        public double Longitude { get; set; }

        [Column("luas_hektar")]
        public double AreaHectares { get; set; }

        // kelembapan udara ideal (%), kosong = pakai default dari katalog tanaman
        [Column("kelembapan_min")]
        public double? MinMoisture { get; set; }

        [Column("kelembapan_max")]
        public double? MaxMoisture { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public double? IdealMoistureMin => MinMoisture ?? (Tanaman != null ? Tanaman.KelembapanIdealMin : null);

        [JsonIgnore]
        public double? IdealMoistureMax => MaxMoisture ?? (Tanaman != null ? Tanaman.KelembapanIdealMax : null);

        [JsonIgnore]
        public bool HasIdealMoisture => IdealMoistureMin.HasValue && IdealMoistureMax.HasValue && IdealMoistureMax > 0;

        [JsonIgnore]
        public string MoistureRangeText => HasIdealMoisture
            ? $"{IdealMoistureMin!.Value.ToString("0.##", CultureInfo.InvariantCulture)}–{IdealMoistureMax!.Value.ToString("0.##", CultureInfo.InvariantCulture)}%"
            : "-";

        // Objek Tanaman yang ditanam di lahan ini (Relasi OOP)
        [JsonIgnore]
        public Tanaman? Tanaman { get; set; }

        private string _cropType = string.Empty;

        /// <summary>
        /// Nama jenis tanaman untuk tampilan UI. Jika relasi Tanaman terisi, menggunakan Tanaman.DisplayName.
        /// </summary>
        [JsonIgnore]
        public string CropType
        {
            get => Tanaman != null ? Tanaman.DisplayName : _cropType;
            set => _cropType = value;
        }

        [JsonIgnore]
        public ObservableCollection<IrrigationLog> IrrigationLogs { get; set; } = new();

        // UI Formatted properties (diabaikan oleh serializer Supabase PostgREST)
        [JsonIgnore]
        public string CoordinateText => 
            $"{Latitude.ToString("0.0000", CultureInfo.InvariantCulture)}, {Longitude.ToString("0.0000", CultureInfo.InvariantCulture)}";

        [JsonIgnore]
        public string AreaText
        {
            get
            {
                if (AreaHectares < 0.1 && AreaHectares > 0)
                {
                    double m2 = AreaHectares * 10000.0;
                    return $"{m2.ToString("0.##", CultureInfo.InvariantCulture)} m²";
                }
                return $"{AreaHectares.ToString("0.##", CultureInfo.InvariantCulture)} Ha";
            }
        }

        [JsonIgnore]
        public string SubtitleText => string.IsNullOrWhiteSpace(LocationDescription) 
            ? $"Koordinat: {CoordinateText}" 
            : $"{LocationDescription} • Koordinat: {CoordinateText}";

        public Land Clone()
        {
            var clone = new Land
            {
                Id = this.Id,
                OwnerId = this.OwnerId,
                TanamanId = this.TanamanId,
                Name = this.Name,
                LocationDescription = this.LocationDescription,
                Latitude = this.Latitude,
                Longitude = this.Longitude,
                AreaHectares = this.AreaHectares,
                MinMoisture = this.MinMoisture,
                MaxMoisture = this.MaxMoisture,
                Tanaman = this.Tanaman,
                CropType = this.CropType,
                CreatedAt = this.CreatedAt
            };

            foreach (var log in this.IrrigationLogs)
            {
                clone.IrrigationLogs.Add(new IrrigationLog
                {
                    Id = log.Id,
                    LandId = log.LandId,
                    Date = log.Date,
                    VolumeLiters = log.VolumeLiters,
                    Notes = log.Notes,
                    CreatedAt = log.CreatedAt
                });
            }

            return clone;
        }
    }
}
