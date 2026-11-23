using System.Collections.Generic;
using System.Threading.Tasks;
using Lahanku.Models;

namespace Lahanku.Services
{
    /// <summary>
    /// Contract for land, irrigation, and crop catalog management (DIP - Dependency Inversion Principle).
    /// </summary>
    public interface ILandService
    {
        Task<List<Land>> GetLandsAsync(Guid? ownerId = null);
        Task<Land?> GetLandByIdAsync(long id);
        Task<Land> AddLandAsync(Land land);
        Task<bool> UpdateLandAsync(Land land);
        Task<bool> DeleteLandAsync(long id);
        Task<IrrigationLog> AddIrrigationLogAsync(long landId, IrrigationLog log);
        Task<List<IrrigationLog>> GetIrrigationLogsAsync(long landId);
        Task<bool> UpdateIrrigationLogAsync(IrrigationLog log);
        Task<bool> DeleteIrrigationLogAsync(long logId);
        Task<List<Crop>> GetCropsAsync();
    }
}
