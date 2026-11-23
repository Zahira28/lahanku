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
    /// Implementasi layanan lahan dan irigasi terhubung langsung ke cloud database Supabase.
    /// Mematuhi Single Responsibility Principle (SRP) dan Dependency Inversion (DIP).
    /// </summary>
    public class LandService : ILandService
    {
        private List<Tanaman>? _tanamanCache;

        public async Task<List<Tanaman>> GetTanamanListAsync()
        {
            if (_tanamanCache != null && _tanamanCache.Count > 0)
            {
                return _tanamanCache;
            }

            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                var response = await client
                    .From<Tanaman>()
                    .Order(t => t.NamaTanaman, Ordering.Ascending)
                    .Get();

                _tanamanCache = response.Models;
                return _tanamanCache;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LandService] Error fetching tanaman: {ex.Message}");
                return new List<Tanaman>();
            }
        }

        public async Task<List<Land>> GetLandsAsync(long? userId = null)
        {
            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                var query = client.From<Land>();

                var response = userId.HasValue
                    ? await query.Where(l => l.UserId == userId.Value).Get()
                    : await query.Get();

                var lands = response.Models;
                var tanamanList = await GetTanamanListAsync();
                var tanamanMap = tanamanList.ToDictionary(t => t.IdTanaman);

                foreach (var land in lands)
                {
                    if (land.TanamanId.HasValue && tanamanMap.TryGetValue(land.TanamanId.Value, out var tanaman))
                    {
                        land.Tanaman = tanaman;
                    }

                    var logs = await GetIrrigationLogsAsync(land.Id);
                    land.IrrigationLogs = new ObservableCollection<IrrigationLog>(logs);
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
                    if (land.TanamanId.HasValue)
                    {
                        var tanamanList = await GetTanamanListAsync();
                        land.Tanaman = tanamanList.FirstOrDefault(t => t.IdTanaman == land.TanamanId.Value);
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

                // Sync Tanaman info
                if (created.TanamanId.HasValue)
                {
                    var tanamanList = await GetTanamanListAsync();
                    created.Tanaman = tanamanList.FirstOrDefault(t => t.IdTanaman == created.TanamanId.Value);
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
