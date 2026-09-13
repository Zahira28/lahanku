using System.Collections.Generic;
using System.Threading.Tasks;
using Lahanku.Models;

namespace Lahanku.Services
{
    public interface ILandService
    {
        Task<List<Land>> GetLandsAsync();
        Task<Land?> GetLandByIdAsync(int id);
        Task<Land> AddLandAsync(Land land);
        Task<bool> UpdateLandAsync(Land land);
        Task<bool> DeleteLandAsync(int id);
        Task<IrrigationLog> AddIrrigationLogAsync(int landId, IrrigationLog log);
        Task<List<IrrigationLog>> GetIrrigationLogsAsync(int landId);
    }
}
