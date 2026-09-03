using System.Windows;
using System.Windows.Media;

namespace Lahanku.Helpers
{
    public static class PlaceholderHelper
    {
        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.RegisterAttached(
                "Placeholder",
                typeof(string),
                typeof(PlaceholderHelper),
                new PropertyMetadata(string.Empty));

        public static string GetPlaceholder(DependencyObject d) => (string)d.GetValue(PlaceholderProperty);
        public static void SetPlaceholder(DependencyObject d, string value) => d.SetValue(PlaceholderProperty, value);

        public static readonly DependencyProperty SuffixTextProperty =
            DependencyProperty.RegisterAttached(
                "SuffixText",
                typeof(string),
                typeof(PlaceholderHelper),
                new PropertyMetadata(string.Empty));

        public static string GetSuffixText(DependencyObject d) => (string)d.GetValue(SuffixTextProperty);
        public static void SetSuffixText(DependencyObject d, string value) => d.SetValue(SuffixTextProperty, value);

        public static readonly DependencyProperty PrefixIconProperty =
            DependencyProperty.RegisterAttached(
                "PrefixIcon",
                typeof(Geometry),
                typeof(PlaceholderHelper),
                new PropertyMetadata(null));

        public static Geometry? GetPrefixIcon(DependencyObject d) => (Geometry?)d.GetValue(PrefixIconProperty);
        public static void SetPrefixIcon(DependencyObject d, Geometry? value) => d.SetValue(PrefixIconProperty, value);
    }
}
