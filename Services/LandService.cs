using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Lahanku.Helpers;
using Lahanku.Models;
using static Supabase.Postgrest.Constants;

namespace Lahanku.Services
{
    /// <summary>
    /// Land and irrigation service connected directly to the Supabase cloud database.
    /// Follows the Single Responsibility Principle (SRP) and Dependency Inversion (DIP).
    /// </summary>
    public class LandService : ILandService
    {
        private List<Crop>? _cropCache;

        public async Task<List<Crop>> GetCropsAsync()
        {
            if (_cropCache != null && _cropCache.Count > 0)
            {
                return _cropCache;
            }

            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                var response = await client
                    .From<Crop>()
                    .Order(t => t.Name, Ordering.Ascending)
                    .Get();

                _cropCache = response.Models;
                return _cropCache;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LandService] Error fetching tanaman: {ex.Message}");
                return new List<Crop>();
            }
        }

        public async Task<List<Land>> GetLandsAsync(Guid? ownerId = null)
        {
            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                var query = client.From<Land>();

                // RLS already limits this to the user's own lands, this filter is just a safety net
                var response = ownerId.HasValue
                    ? await query.Where(l => l.OwnerId == ownerId.Value).Get()
                    : await query.Get();

                var lands = response.Models;
                var crops = await GetCropsAsync();
                var cropMap = crops.ToDictionary(t => t.Id);

                // watering logs aren't fetched here, Land Detail loads its own
                foreach (var land in lands)
                {
                    if (land.CropId.HasValue && cropMap.TryGetValue(land.CropId.Value, out var crop))
                    {
                        land.Crop = crop;
                    }
                }

                return lands;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LandService] Error fetching lands: {ex.Message}");
                return new List<Land>();
            }
        }

        public async Task<Land?> GetLandByIdAsync(long id)
        {
            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                var response = await client
                    .From<Land>()
                    .Where(l => l.Id == id)
                    .Get();

                var land = response.Models.FirstOrDefault();
                if (land != null)
                {
                    if (land.CropId.HasValue)
                    {
                        var crops = await GetCropsAsync();
                        land.Crop = crops.FirstOrDefault(t => t.Id == land.CropId.Value);
                    }

                    var logs = await GetIrrigationLogsAsync(land.Id);
                    land.IrrigationLogs = new ObservableCollection<IrrigationLog>(logs);
                }

                return land;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LandService] Error fetching land by ID: {ex.Message}");
                return null;
            }
        }

        public async Task<Land> AddLandAsync(Land land)
        {
            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                var response = await client.From<Land>().Insert(land);
                var created = response.Models.FirstOrDefault() ?? land;

                // Sync Crop info
                if (created.CropId.HasValue)
                {
                    var crops = await GetCropsAsync();
                    created.Crop = crops.FirstOrDefault(t => t.Id == created.CropId.Value);
                }

                return created;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LandService] Error adding land: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateLandAsync(Land land)
        {
            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                await client.From<Land>().Update(land);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LandService] Error updating land: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteLandAsync(long id)
        {
            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                await client.From<Land>().Where(l => l.Id == id).Delete();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LandService] Error deleting land: {ex.Message}");
                return false;
            }
        }

        public async Task<IrrigationLog> AddIrrigationLogAsync(long landId, IrrigationLog log)
        {
            try
            {
                log.LandId = landId;
                log.Date = LocalTime.Ensure(log.Date);
                var client = await SupabaseConfig.GetClientAsync();
                var response = await client.From<IrrigationLog>().Insert(log);
                return response.Models.FirstOrDefault() ?? log;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LandService] Error adding irrigation log: {ex.Message}");
                throw;
            }
        }

        public async Task<List<IrrigationLog>> GetIrrigationLogsAsync(long landId)
        {
            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                var response = await client
                    .From<IrrigationLog>()
                    .Where(l => l.LandId == landId)
                    .Order(l => l.Date, Ordering.Descending)
                    .Get();

                return response.Models;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LandService] Error fetching irrigation logs: {ex.Message}");
                return new List<IrrigationLog>();
            }
        }

        public async Task<bool> UpdateIrrigationLogAsync(IrrigationLog log)
        {
            try
            {
                log.Date = LocalTime.Ensure(log.Date);
                var client = await SupabaseConfig.GetClientAsync();
                await client.From<IrrigationLog>().Update(log);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LandService] Error updating irrigation log: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteIrrigationLogAsync(long logId)
        {
            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                await client.From<IrrigationLog>().Where(l => l.Id == logId).Delete();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LandService] Error deleting irrigation log: {ex.Message}");
                return false;
            }
        }
    }
}
