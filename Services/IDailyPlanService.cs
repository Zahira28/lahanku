using System;
using System.Threading.Tasks;
using Lahanku.Models;

namespace Lahanku.Services
{
    public interface IDailyPlanService
    {
        // get weather + forecast, calculate the recommendation, save the daily record
        Task<LandPlan> CalculateAsync(Land land, DateTime waktu);
    }
}
