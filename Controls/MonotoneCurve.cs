using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;

namespace Lahanku.Controls
{
    // Smooth curve through points that never overshoots (Fritsch–Carlson monotone cubic).
    // Used by the impact chart so a dip to 0 L never draws below the axis.
    public class MonotoneCurve
    {
        private readonly Point[] _points;
        private readonly double[] _tangents;

        public MonotoneCurve(IEnumerable<Point> points)
        {
            _points = points.OrderBy(p => p.X).ToArray();
            _tangents = BuildTangents(_points);
        }

        private static double[] BuildTangents(Point[] p)
        {
            int n = p.Length;
            var t = new double[n];
            if (n < 2)
            {
                return t;
            }

            var slope = new double[n - 1];
            for (int i = 0; i < n - 1; i++)
            {
                slope[i] = (p[i + 1].Y - p[i].Y) / (p[i + 1].X - p[i].X);
            }

            t[0] = slope[0];
            t[n - 1] = slope[n - 2];
            for (int i = 1; i < n - 1; i++)
            {
                t[i] = slope[i - 1] * slope[i] <= 0 ? 0 : (slope[i - 1] + slope[i]) / 2;
            }

            for (int i = 0; i < n - 1; i++)
            {
                if (slope[i] == 0)
                {
                    t[i] = 0;
                    t[i + 1] = 0;
                    continue;
                }

                double a = t[i] / slope[i], b = t[i + 1] / slope[i], s = a * a + b * b;
                if (s > 9)
                {
                    double k = 3 / Math.Sqrt(s);
                    t[i] = k * a * slope[i];
                    t[i + 1] = k * b * slope[i];
                }
            }

            return t;
        }

        // y on the curve for any x (same Hermite formula the path uses)
        public double YAt(double x)
        {
            int n = _points.Length;
            if (n == 0)
            {
                return 0;
            }
            if (n == 1 || x <= _points[0].X)
            {
                return _points[0].Y;
            }
            if (x >= _points[n - 1].X)
            {
                return _points[n - 1].Y;
            }

            int i = 0;
            while (i < n - 2 && x > _points[i + 1].X)
            {
                i++;
            }

            double dx = _points[i + 1].X - _points[i].X;
            double s = (x - _points[i].X) / dx, s2 = s * s, s3 = s2 * s;
            return (2 * s3 - 3 * s2 + 1) * _points[i].Y + (s3 - 2 * s2 + s) * dx * _tangents[i]
                 + (-2 * s3 + 3 * s2) * _points[i + 1].Y + (s3 - s2) * dx * _tangents[i + 1];
        }

        // SVG-style path data ("M x,y C ...") that Geometry.Parse understands
        public string ToPathData()
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            if (_points.Length == 0)
            {
                return sb.ToString();
            }

            sb.Append(inv, $"M{_points[0].X:0.##},{_points[0].Y:0.##}");
            for (int i = 0; i < _points.Length - 1; i++)
            {
                double h = (_points[i + 1].X - _points[i].X) / 3;
                sb.Append(inv, $" C{_points[i].X + h:0.##},{_points[i].Y + _tangents[i] * h:0.##}");
                sb.Append(inv, $" {_points[i + 1].X - h:0.##},{_points[i + 1].Y - _tangents[i + 1] * h:0.##}");
                sb.Append(inv, $" {_points[i + 1].X:0.##},{_points[i + 1].Y:0.##}");
            }
            return sb.ToString();
        }
    }
}
