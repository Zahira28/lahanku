using System;
using System.Threading.Tasks;
using Lahanku.Models;

namespace Lahanku.Services
{
    // satu pintu buat rekomendasi harian, dipakai Dashboard dan Detail Lahan
    public class RencanaHarianService : IRencanaHarianService
    {
        private readonly IWeatherService _weather;
        private readonly IIrrigationAdvisorService _advisor;
        private readonly IRekomendasiHarianService _rekomendasi;

        public RencanaHarianService(IWeatherService weather, IIrrigationAdvisorService advisor, IRekomendasiHarianService rekomendasi)
        {
            _weather = weather;
            _advisor = advisor;
            _rekomendasi = rekomendasi;
        }

        public async Task<RencanaLahan> HitungAsync(Land land, DateTime waktu)
        {
            // dua request cuaca jalan barengan
            var cuacaTask = _weather.GetCurrentWeatherAsync(land.Latitude, land.Longitude, land.Id);
            var prakiraanTask = _weather.GetPrakiraanHujanAsync(land.Latitude, land.Longitude);
            await Task.WhenAll(cuacaTask, prakiraanTask);

            var rekomendasi = _advisor.Hitung(land, cuacaTask.Result, prakiraanTask.Result, waktu);
            await _rekomendasi.SimpanAsync(land, rekomendasi, waktu);

            return new RencanaLahan(land, cuacaTask.Result, rekomendasi);
        }
    }
}
