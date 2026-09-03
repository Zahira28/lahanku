using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Lahanku.Models;

namespace Lahanku.Services
{
    public class LandService : ILandService
    {
        private readonly List<Land> _lands = new();
        private int _nextLandId = 4;
        private int _nextLogId = 10;

        public LandService()
        {
            SeedInitialData();
        }

        private void SeedInitialData()
        {
            // Seed matching Mockup 5 & 6 exactly
            var landA = new Land
            {
                Id = 1,
                Name = "Lahan A",
                LocationDescription = "Sawah Sentosa Selatan",
                Latitude = -6.2088,
                Longitude = 106.8456,
                AreaHectares = 0.5,
                CropType = "Padi Pandan Wangi",
                CreatedAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Local),
                IrrigationLogs = new ObservableCollection<IrrigationLog>
                {
                    new IrrigationLog
                    {
                        Id = 1,
                        LandId = 1,
                        Date = new DateTime(2026, 3, 14, 8, 30, 0),
                        VolumeLiters = 250,
                        Notes = "Penyiraman pagi rutin, kondisi cuaca cerah."
                    },
                    new IrrigationLog
                    {
                        Id = 2,
                        LandId = 1,
                        Date = new DateTime(2026, 3, 13, 16, 15, 0),
                        VolumeLiters = 200,
                        Notes = "Sore hari, tanah kering di bagian selatan"
                    },
                    new IrrigationLog
                    {
                        Id = 3,
                        LandId = 1,
                        Date = new DateTime(2026, 3, 12, 8, 0, 0),
                        VolumeLiters = 250,
                        Notes = "Penyiraman pagi rutin"
                    },
                    new IrrigationLog
                    {
                        Id = 4,
                        LandId = 1,
                        Date = new DateTime(2026, 3, 11, 9, 0, 0),
                        VolumeLiters = 300,
                        Notes = "Volume ditingkatkan karena cuaca sangat terik"
                    },
                    new IrrigationLog
                    {
                        Id = 5,
                        LandId = 1,
                        Date = new DateTime(2026, 3, 10, 15, 45, 0),
                        VolumeLiters = 200,
                        Notes = "Penyiraman sore berkala"
                    }
                }
            };

            var landB = new Land
            {
                Id = 2,
                Name = "Lahan B",
                LocationDescription = "Kebun Jagung Lereng Barat",
                Latitude = -6.2140,
                Longitude = 106.8480,
                AreaHectares = 4.0,
                CropType = "Jagung Manis",
                CreatedAt = new DateTime(2026, 3, 2, 9, 0, 0, DateTimeKind.Local),
                IrrigationLogs = new ObservableCollection<IrrigationLog>
                {
                    new IrrigationLog
                    {
                        Id = 6,
                        LandId = 2,
                        Date = new DateTime(2026, 3, 14, 9, 0, 0),
                        VolumeLiters = 180,
                        Notes = "Penyiraman bibit muda jagung"
                    }
                }
            };

            var landC = new Land
            {
                Id = 3,
                Name = "Lahan C",
                LocationDescription = "Plaza Agro Mandiri",
                Latitude = -6.2050,
                Longitude = 106.8400,
                AreaHectares = 8.0,
                CropType = "Padi Pandan Wangi",
                CreatedAt = new DateTime(2026, 3, 3, 8, 30, 0, DateTimeKind.Local),
                IrrigationLogs = new ObservableCollection<IrrigationLog>()
            };

            _lands.Add(landA);
            _lands.Add(landB);
            _lands.Add(landC);
        }

        public async Task<List<Land>> GetLandsAsync()
        {
            await Task.Delay(50);
            return _lands.Select(l => l.Clone()).ToList();
        }

        public async Task<Land?> GetLandByIdAsync(int id)
        {
            await Task.Delay(50);
            var land = _lands.FirstOrDefault(l => l.Id == id);
            return land?.Clone();
        }

        public async Task<Land> AddLandAsync(Land land)
        {
            await Task.Delay(50);
            var newLand = land.Clone();
            newLand.Id = _nextLandId++;
            newLand.CreatedAt = DateTime.Now;
            _lands.Add(newLand);
            return newLand.Clone();
        }

        public async Task<bool> UpdateLandAsync(Land land)
        {
            await Task.Delay(50);
            var existing = _lands.FirstOrDefault(l => l.Id == land.Id);
            if (existing == null) return false;

            existing.Name = land.Name;
            existing.LocationDescription = land.LocationDescription;
            existing.Latitude = land.Latitude;
            existing.Longitude = land.Longitude;
            existing.AreaHectares = land.AreaHectares;
            existing.CropType = land.CropType;

            return true;
        }

        public async Task<bool> DeleteLandAsync(int id)
        {
            await Task.Delay(50);
            var existing = _lands.FirstOrDefault(l => l.Id == id);
            if (existing == null) return false;

            _lands.Remove(existing);
            return true;
        }

        public async Task<IrrigationLog> AddIrrigationLogAsync(int landId, IrrigationLog log)
        {
            await Task.Delay(50);
            var land = _lands.FirstOrDefault(l => l.Id == landId);
            if (land == null)
            {
                throw new KeyNotFoundException($"Land with ID {landId} not found.");
            }

            var newLog = new IrrigationLog
            {
                Id = _nextLogId++,
                LandId = landId,
                Date = log.Date,
                VolumeLiters = log.VolumeLiters,
                Notes = log.Notes
            };

            land.IrrigationLogs.Insert(0, newLog);
            return newLog;
        }

        public async Task<List<IrrigationLog>> GetIrrigationLogsAsync(int landId)
        {
            await Task.Delay(50);
            var land = _lands.FirstOrDefault(l => l.Id == landId);
            if (land == null) return new List<IrrigationLog>();

            return land.IrrigationLogs.ToList();
        }
    }
}
