using System;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Lahanku.Models
{
    public class Land
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LocationDescription { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double AreaHectares { get; set; }
        public string CropType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ObservableCollection<IrrigationLog> IrrigationLogs { get; set; } = new();

        // UI Formatted properties
        public string CoordinateText => 
            $"{Latitude.ToString("0.0000", CultureInfo.InvariantCulture)}, {Longitude.ToString("0.0000", CultureInfo.InvariantCulture)}";

        public string AreaText => $"{AreaHectares.ToString("0.##", CultureInfo.InvariantCulture)} Ha";

        public string SubtitleText => string.IsNullOrWhiteSpace(LocationDescription) 
            ? $"Koordinat: {CoordinateText}" 
            : $"{LocationDescription} • Koordinat: {CoordinateText}";

        public Land Clone()
        {
            var clone = new Land
            {
                Id = this.Id,
                Name = this.Name,
                LocationDescription = this.LocationDescription,
                Latitude = this.Latitude,
                Longitude = this.Longitude,
                AreaHectares = this.AreaHectares,
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
                    Notes = log.Notes
                });
            }

            return clone;
        }
    }
}
