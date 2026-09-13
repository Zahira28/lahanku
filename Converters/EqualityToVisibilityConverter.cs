using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Lahanku.Converters
{
    public class EqualityToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values != null && values.Length >= 2 && values[0] != null && values[1] != null)
            {
                bool areEqual = string.Equals(values[0].ToString(), values[1].ToString(), StringComparison.OrdinalIgnoreCase);

                if (parameter is string paramStr && paramStr.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
                {
                    areEqual = !areEqual;
                }

                return areEqual ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
