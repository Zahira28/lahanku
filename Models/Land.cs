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

        [Column("id_user")]
        public long? UserId { get; set; }

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

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

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
                UserId = this.UserId,
                TanamanId = this.TanamanId,
                Name = this.Name,
                LocationDescription = this.LocationDescription,
                Latitude = this.Latitude,
                Longitude = this.Longitude,
                AreaHectares = this.AreaHectares,
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
