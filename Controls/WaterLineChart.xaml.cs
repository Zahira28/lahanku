using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Lahanku.Models;

namespace Lahanku.Controls
{
    // Daily water usage line chart for the impact page.
    // One series ("Dipakai"), a dashed reference line ("Penyiraman rutin"), blue dots for postponed days.
    // Hover: the dot slides along the curve under the cursor; the popover shows the nearest day's real numbers.
    public partial class WaterLineChart : UserControl
    {
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("id-ID");
        private static readonly Brush Green = Hex("#059669");
        private static readonly Brush Blue = Hex("#3B82F6");
        private static readonly Brush Ink = Hex("#64748B");
        private static readonly Brush AxisText = Hex("#94A3B8");
        private static readonly Brush GridLine = Hex("#F1F5F9");
        private static readonly Brush BaseLine = Hex("#CBD5E1");

        private const double PadLeft = 58, PadRight = 12, PadTop = 10, PadBottom = 30;

        public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
            nameof(Points), typeof(IReadOnlyList<DailyPoint>), typeof(WaterLineChart),
            new PropertyMetadata(null, (d, _) => ((WaterLineChart)d).Redraw()));

        public static readonly DependencyProperty ReferenceValueProperty = DependencyProperty.Register(
            nameof(ReferenceValue), typeof(double), typeof(WaterLineChart),
            new PropertyMetadata(0d, (d, _) => ((WaterLineChart)d).Redraw()));

        public IReadOnlyList<DailyPoint>? Points
        {
            get => (IReadOnlyList<DailyPoint>?)GetValue(PointsProperty);
            set => SetValue(PointsProperty, value);
        }

        public double ReferenceValue
        {
            get => (double)GetValue(ReferenceValueProperty);
            set => SetValue(ReferenceValueProperty, value);
        }

        // layout of the last redraw, reused by the hover handlers
        private MonotoneCurve? _curve;
        private double[] _xs = Array.Empty<double>();
        private double _plotWidth, _plotHeight, _maxY;
        private Rectangle? _band;
        private Ellipse? _dot;
        private Border? _dateBadge;
        private TextBlock? _dateBadgeText;

        public WaterLineChart()
        {
            InitializeComponent();
            SizeChanged += (_, _) => Redraw();
        }

        private double Y(double value) => PadTop + _plotHeight - _plotHeight * value / _maxY;

        private void Redraw()
        {
            Plot.Children.Clear();
            Popover.Visibility = Visibility.Collapsed;
            _curve = null;

            var points = Points ?? Array.Empty<DailyPoint>();
            double width = ActualWidth, height = ActualHeight;
            EmptyText.Visibility = points.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (points.Count == 0 || width < 100 || height < 80)
            {
                return;
            }

            _plotWidth = width - PadLeft - PadRight;
            _plotHeight = height - PadTop - PadBottom;
            double step = NiceStep(Math.Max(points.Max(p => p.Used), ReferenceValue) * 1.1 / 3);
            _maxY = step * 3;

            // grid + y labels
            for (int k = 0; k <= 3; k++)
            {
                double v = step * k, y = Y(v);
                Plot.Children.Add(new Line { X1 = PadLeft, X2 = width - PadRight, Y1 = y, Y2 = y, Stroke = k == 0 ? BaseLine : GridLine, StrokeThickness = 1 });
                AddText(k == 0 ? "0" : $"{v.ToString("#,0", Culture)} L", PadLeft - 8, y, AxisText, 11, alignRight: true);
            }

            // x positions + a few date labels
            int n = points.Count;
            _xs = Enumerable.Range(0, n).Select(i => n == 1 ? PadLeft + _plotWidth / 2 : PadLeft + _plotWidth * i / (n - 1)).ToArray();
            foreach (int i in LabelIndices(n))
            {
                AddText(points[i].Date.ToString("d MMM", Culture), _xs[i], height - 12, AxisText, 11, center: true);
            }

            // hover band sits behind the data
            _band = new Rectangle { Fill = GridLine, Height = _plotHeight, Opacity = 0, IsHitTestVisible = false };
            Canvas.SetTop(_band, PadTop);
            Plot.Children.Add(_band);

            var pixelPoints = points.Select((p, i) => new Point(_xs[i], Y(p.Used))).ToList();
            _curve = new MonotoneCurve(pixelPoints);
            string line = _curve.ToPathData();

            // soft area under the line
            var area = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            area.GradientStops.Add(new GradientStop(Color.FromArgb(0x29, 0x05, 0x96, 0x69), 0));
            area.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x05, 0x96, 0x69), 1));
            string areaData = $"{line} L{_xs[n - 1].ToString(CultureInfo.InvariantCulture)},{Y(0).ToString(CultureInfo.InvariantCulture)} L{_xs[0].ToString(CultureInfo.InvariantCulture)},{Y(0).ToString(CultureInfo.InvariantCulture)} Z";
            Plot.Children.Add(new Path { Data = Geometry.Parse(areaData), Fill = area, IsHitTestVisible = false });

            // dashed reference line + value badge on the y axis
            if (ReferenceValue > 0)
            {
                double ry = Y(ReferenceValue);
                Plot.Children.Add(new Line { X1 = PadLeft, X2 = width - PadRight, Y1 = ry, Y2 = ry, Stroke = Ink, StrokeThickness = 1.5, StrokeDashArray = new DoubleCollection { 4, 3 }, IsHitTestVisible = false });
                var badge = new Border
                {
                    Background = Hex("#475569"),
                    CornerRadius = new CornerRadius(4),
                    Width = PadLeft - 6,
                    Height = 20,
                    Child = new TextBlock { Text = $"{ReferenceValue.ToString("#,0", Culture)} L", Foreground = Brushes.White, FontSize = 10.5, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
                };
                Canvas.SetLeft(badge, 0);
                Canvas.SetTop(badge, ry - 10);
                Plot.Children.Add(badge);
            }

            Plot.Children.Add(new Path { Data = Geometry.Parse(line), Stroke = Green, StrokeThickness = 2, StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, IsHitTestVisible = false });

            // postponed days: blue dot on the zero line; with few days every day gets a dot,
            // otherwise a single day would draw nothing at all
            for (int i = 0; i < n; i++)
            {
                if (points[i].IsPostponed)
                {
                    AddMarker(_xs[i], Y(0), Blue, 9);
                }
                else if (n <= 14)
                {
                    AddMarker(_xs[i], Y(points[i].Used), Green, 8);
                }
            }

            // hover dot + date badge on the x axis
            _dot = new Ellipse { Width = 10, Height = 10, Fill = Brushes.White, Stroke = Green, StrokeThickness = 2, Opacity = 0, IsHitTestVisible = false };
            Plot.Children.Add(_dot);
            _dateBadgeText = new TextBlock { Foreground = Brushes.White, FontSize = 11, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            _dateBadge = new Border { Background = Green, CornerRadius = new CornerRadius(4), Width = 60, Height = 20, Opacity = 0, IsHitTestVisible = false, Child = _dateBadgeText };
            Canvas.SetTop(_dateBadge, height - 22);
            Plot.Children.Add(_dateBadge);
        }

        private void Plot_MouseMove(object sender, MouseEventArgs e)
        {
            var points = Points;
            if (_curve == null || points == null || points.Count == 0 || _band == null || _dot == null || _dateBadge == null || _dateBadgeText == null)
            {
                return;
            }

            double mx = Math.Clamp(e.GetPosition(Plot).X, _xs[0], _xs[^1]);
            int i = NearestIndex(mx);
            var p = points[i];
            double my = _curve.YAt(mx);
            double step = points.Count > 1 ? _plotWidth / (points.Count - 1) : _plotWidth;

            step = Math.Min(step, 80); // a lone day shouldn't light up the whole chart
            _band.Width = Math.Max(2, step);
            Canvas.SetLeft(_band, _xs[i] - step / 2);
            _band.Opacity = 1;

            _dot.Stroke = p.IsPostponed ? Blue : Green;
            Canvas.SetLeft(_dot, mx - 5);
            Canvas.SetTop(_dot, my - 5);
            _dot.Opacity = 1;

            _dateBadgeText.Text = p.Date.ToString("d MMM", Culture);
            Canvas.SetLeft(_dateBadge, mx - 30);
            _dateBadge.Opacity = 1;

            FillPopover(p);
            double left = mx + 14 + Popover.Width > ActualWidth ? mx - 14 - Popover.Width : mx + 14;
            Popover.Margin = new Thickness(left, Math.Max(0, my - 40), 0, 0);
            Popover.Visibility = Visibility.Visible;
        }

        private void Plot_MouseLeave(object sender, MouseEventArgs e)
        {
            Popover.Visibility = Visibility.Collapsed;
            if (_band != null) _band.Opacity = 0;
            if (_dot != null) _dot.Opacity = 0;
            if (_dateBadge != null) _dateBadge.Opacity = 0;
        }

        // popover always shows the nearest day's real numbers, never interpolated ones
        private void FillPopover(DailyPoint p)
        {
            PopDate.Text = p.Date.ToString("dddd, d MMMM yyyy", Culture);
            PopRows.Children.Clear();

            if (p.IsPostponed)
            {
                PopRows.Children.Add(new TextBlock { Text = "Ditunda karena hujan", Foreground = Hex("#1D4ED8"), FontWeight = FontWeights.SemiBold, FontSize = 12, Margin = new Thickness(0, 0, 0, 2) });
                PopRows.Children.Add(Row("Dihemat", Liters(p.Baseline), Hex("#047857")));
                return;
            }

            double diff = p.Baseline - p.Used;
            PopRows.Children.Add(Row("Dipakai", Liters(p.Used), null));
            PopRows.Children.Add(Row("Penyiraman rutin", Liters(p.Baseline), null));
            PopRows.Children.Add(Row("Selisih", diff >= 0 ? $"−{Liters(diff)}" : $"+{Liters(-diff)}", diff >= 0 ? Hex("#047857") : Hex("#B45309")));
        }

        private static Grid Row(string label, string value, Brush? valueBrush)
        {
            var g = new Grid { Margin = new Thickness(0, 1, 0, 1) };
            g.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = Hex("#334155") });
            g.Children.Add(new TextBlock { Text = value, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = valueBrush ?? Hex("#0F172A"), HorizontalAlignment = HorizontalAlignment.Right });
            return g;
        }

        private void AddMarker(double x, double y, Brush fill, double size)
        {
            var marker = new Ellipse { Width = size, Height = size, Fill = fill, Stroke = Brushes.White, StrokeThickness = 2, IsHitTestVisible = false };
            Canvas.SetLeft(marker, x - size / 2);
            Canvas.SetTop(marker, y - size / 2);
            Plot.Children.Add(marker);
        }

        private int NearestIndex(double x)
        {
            int best = 0;
            for (int i = 1; i < _xs.Length; i++)
            {
                if (Math.Abs(_xs[i] - x) < Math.Abs(_xs[best] - x))
                {
                    best = i;
                }
            }
            return best;
        }

        private void AddText(string text, double x, double y, Brush brush, double size, bool alignRight = false, bool center = false)
        {
            var tb = new TextBlock { Text = text, Foreground = brush, FontSize = size, IsHitTestVisible = false };
            tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double left = alignRight ? x - tb.DesiredSize.Width : center ? x - tb.DesiredSize.Width / 2 : x;
            Canvas.SetLeft(tb, left);
            Canvas.SetTop(tb, y - tb.DesiredSize.Height / 2);
            Plot.Children.Add(tb);
        }

        // at most 5 evenly spread date labels (always first and last)
        private static IEnumerable<int> LabelIndices(int n)
        {
            if (n <= 5)
            {
                return Enumerable.Range(0, n);
            }
            return Enumerable.Range(0, 5).Select(k => (int)Math.Round(k * (n - 1) / 4.0)).Distinct();
        }

        // 1, 2, 2.5, 5 × 10^k so axis labels read nicely
        private static double NiceStep(double raw)
        {
            if (raw <= 0)
            {
                return 1;
            }
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            double f = raw / magnitude;
            double nice = f <= 1 ? 1 : f <= 2 ? 2 : f <= 2.5 ? 2.5 : f <= 5 ? 5 : 10;
            return nice * magnitude;
        }

        private static string Liters(double value) => $"{value.ToString("#,0", Culture)} L";

        private static SolidColorBrush Hex(string hex)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
            brush.Freeze();
            return brush;
        }
    }
}
