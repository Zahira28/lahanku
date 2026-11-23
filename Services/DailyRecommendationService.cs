using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lahanku.Helpers;
using Lahanku.Models;
using static Supabase.Postgrest.Constants;

namespace Lahanku.Services
{
    // 1 recommendation record per land per day. Updated every time the app opens,
    // but frozen once a watering is logged that day.
    public class DailyRecommendationService : IDailyRecommendationService
    {
        private readonly ILandService _landService;

        public DailyRecommendationService(ILandService landService)
        {
            _landService = landService;
        }

        public async Task<DailyRecommendation?> SaveForDateAsync(Land land, Recommendation recommendation, DateTime date)
        {
            var day = date.Date;
            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                var existing = (await client
                    .From<DailyRecommendation>()
                    .Filter("id_lahan", Operator.Equals, land.Id.ToString())
                    .Filter("tanggal", Operator.Equals, day.ToString("yyyy-MM-dd"))
                    .Get()).Models.FirstOrDefault();

                if (existing != null)
                {
                    // a watering already exists that day → the recommendation is frozen
                    var logs = await _landService.GetIrrigationLogsAsync(land.Id);
                    if (logs.Any(l => l.Date.Date == day))
                    {
                        return existing;
                    }

                    Fill(existing, recommendation);
                    await client.From<DailyRecommendation>().Update(existing);
                    return existing;
                }

                var newRecord = new DailyRecommendation { LandId = land.Id, Date = day, CreatedAt = DateTime.UtcNow };
                Fill(newRecord, recommendation);
                var response = await client.From<DailyRecommendation>().Insert(newRecord);
                return response.Models.FirstOrDefault() ?? newRecord;
            }
            catch (Exception ex)
            {
                // a failed save must not break the recommendation display
                System.Diagnostics.Debug.WriteLine($"[RekomendasiHarian] Gagal simpan: {ex}");
                return null;
            }
        }

        public async Task<List<DailyRecommendation>> GetHistoryAsync(long landId, DateTime from, DateTime to)
        {
            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                var response = await client
                    .From<DailyRecommendation>()
                    .Filter("id_lahan", Operator.Equals, landId.ToString())
                    .Filter("tanggal", Operator.GreaterThanOrEqual, from.Date.ToString("yyyy-MM-dd"))
                    .Filter("tanggal", Operator.LessThanOrEqual, to.Date.ToString("yyyy-MM-dd"))
                    .Order("tanggal", Ordering.Descending)
                    .Get();
                return response.Models;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RekomendasiHarian] Gagal ambil riwayat: {ex}");
                return new List<DailyRecommendation>();
            }
        }

        private static void Fill(DailyRecommendation target, Recommendation r)
        {
            target.Decision = r.Decision.ToDb();
            target.RecommendedVolume = r.RecommendedVolume;
            target.BaselineVolume = r.BaselineVolume;
            target.ForecastRainMm = r.ForecastRainMm;
            target.RainProbability = r.RainProbability;
            target.StatusLabel = r.Title;
            target.Advice = string.IsNullOrEmpty(r.TimingAdvice) ? r.Reason : $"{r.Reason} {r.TimingAdvice}";
        }
    }
}
