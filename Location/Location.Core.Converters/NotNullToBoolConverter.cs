using System.Globalization;

namespace Location.Core.Converters
{
    /// <summary>
    /// Converts null/non-null objects to boolean values.
    /// Returns true if the value is not null, false if null.
    /// </summary>
    public class NotNullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("NotNullToBoolConverter does not support ConvertBack");
        }
    }
}