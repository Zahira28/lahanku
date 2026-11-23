using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lahanku.Models;

namespace Lahanku.Services
{
    public interface IDailyRecommendationService
    {
        // save/update a land's recommendation record for that date; null on failure
        Task<DailyRecommendation?> SaveForDateAsync(Land land, Recommendation recommendation, DateTime date);

        // daily records of one land within a date range (newest first); empty on failure
        Task<List<DailyRecommendation>> GetHistoryAsync(long landId, DateTime from, DateTime to);
    }
}
