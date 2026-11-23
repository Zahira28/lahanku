using System.Collections.Generic;
using System.Linq;

namespace Lahanku.Models
{
    // total air dibanding penyiraman rutin. Cuma hari yang punya catatan rekomendasi yang dihitung.
    public record RingkasanAir(double Standar, double Dipakai)
    {
        public double Dihemat => Standar - Dipakai;
        public double PersenHemat => Standar > 0 ? Dihemat / Standar * 100 : 0;
        public bool AdaData => Standar > 0;

        public static RingkasanAir Hitung(IEnumerable<EvaluasiIrigasi> catatan, IEnumerable<IrrigationLog> logs)
        {
            var daftar = catatan.ToList();
            var hariTercatat = daftar.Select(c => (c.IdLahan, c.Tanggal.Date)).ToHashSet();

            double standar = daftar.Sum(c => c.VolumeStandar);
            double dipakai = logs
                .Where(l => hariTercatat.Contains((l.LandId, l.Date.Date)))
                .Sum(l => l.VolumeLiters);

            return new RingkasanAir(standar, dipakai);
        }
    }
}
