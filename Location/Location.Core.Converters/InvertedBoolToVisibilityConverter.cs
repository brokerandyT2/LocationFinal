// Better approach: Add IsToday property to DailyWeatherViewModel
// Then create simple converters

// Location.Core.Converters/BoolToVisibilityConverter.cs
using System.Globalization;



namespace Location.Core.Converters
{
    /// <summary>
    /// Converts a boolean value to an inverted visibility state for UI elements.
    /// True = Collapsed, False = Visible
    /// </summary>
    public class InvertedBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}