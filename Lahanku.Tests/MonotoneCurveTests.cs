using System.Windows;
using Lahanku.Controls;
using Xunit;

namespace Lahanku.Tests
{
    public class MonotoneCurveTests
    {
        private static readonly Point[] Pts =
        {
            new(0, 100), new(10, 40), new(20, 40), new(30, 200), new(40, 120)
        };

        [Fact]
        public void Passes_exactly_through_every_point()
        {
            var curve = new MonotoneCurve(Pts);
            foreach (var p in Pts)
            {
                Assert.Equal(p.Y, curve.YAt(p.X), 6);
            }
        }

        [Fact]
        public void Never_overshoots_between_points()
        {
            var curve = new MonotoneCurve(Pts);
            for (int i = 0; i < Pts.Length - 1; i++)
            {
                double lo = Math.Min(Pts[i].Y, Pts[i + 1].Y), hi = Math.Max(Pts[i].Y, Pts[i + 1].Y);
                for (double x = Pts[i].X; x <= Pts[i + 1].X; x += 0.5)
                {
                    double y = curve.YAt(x);
                    Assert.InRange(y, lo - 1e-6, hi + 1e-6);
                }
            }
        }

        [Fact]
        public void Flat_segment_stays_flat()
        {
            var curve = new MonotoneCurve(Pts);
            Assert.Equal(40, curve.YAt(15), 6);
        }

        [Fact]
        public void Path_data_has_one_bezier_per_segment()
        {
            var data = new MonotoneCurve(Pts).ToPathData();
            Assert.StartsWith("M", data);
            Assert.Equal(Pts.Length - 1, data.Count(c => c == 'C'));
        }

        [Fact]
        public void Single_point_is_handled()
        {
            var curve = new MonotoneCurve(new[] { new Point(5, 30) });
            Assert.Equal(30, curve.YAt(5));
            Assert.StartsWith("M", curve.ToPathData());
        }
    }
}
