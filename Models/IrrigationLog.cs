using System;

namespace Lahanku.Models
{
    public class IrrigationLog
    {
        public int Id { get; set; }
        public int LandId { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public double VolumeLiters { get; set; }
        public string Notes { get; set; } = string.Empty;

        // UI-friendly formatted properties
        public string FormattedDate => Date.ToString("dd MMM yyyy, HH:mm");
        public string FormattedDateOnly => Date.ToString("dd MMMM yyyy");
        public string FormattedVolume => $"{VolumeLiters:0} Liter";
    }
}
