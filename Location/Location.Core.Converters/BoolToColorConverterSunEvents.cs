﻿using System.Globalization;

namespace Location.Core.Converters
{
    public class BoolToColorConverterSunEvents : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive && isActive)
                return Colors.LightGreen;  // Active color
            return Colors.Transparent;     // Inactive color
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}