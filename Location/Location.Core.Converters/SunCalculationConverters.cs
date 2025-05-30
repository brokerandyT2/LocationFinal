// Location.Photography.Maui/Converters/EnhancedSunCalculatorConverters.cs
using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Location.Photography.Maui.Converters
{
    public class BoolToSunStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSunUp)
            {
                return isSunUp ? "☀️ Above Horizon" : "🌙 Below Horizon";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToActiveColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? Color.FromArgb("#C8E6C9") : Color.FromArgb("#F5F5F5");
            }
            return Color.FromArgb("#F5F5F5");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToOptimalColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isOptimal)
            {
                return isOptimal ? Color.FromArgb("#DCEDC8") : Color.FromArgb("#FAFAFA");
            }
            return Color.FromArgb("#FAFAFA");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DoubleToEVStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double evValue)
            {
                return $"EV {evValue:F1}";
            }
            return "EV --";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DoubleToAzimuthStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double azimuth)
            {
                var direction = azimuth switch
                {
                    >= 337.5 or < 22.5 => "N",
                    >= 22.5 and < 67.5 => "NE",
                    >= 67.5 and < 112.5 => "E",
                    >= 112.5 and < 157.5 => "SE",
                    >= 157.5 and < 202.5 => "S",
                    >= 202.5 and < 247.5 => "SW",
                    >= 247.5 and < 292.5 => "W",
                    >= 292.5 and < 337.5 => "NW",
                    _ => ""
                };
                return $"{azimuth:F1}° {direction}";
            }
            return "--";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DoubleToElevationStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double elevation)
            {
                var status = elevation > 0 ? "above horizon" : "below horizon";
                return $"{elevation:F1}° ({status})";
            }
            return "--";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LightReductionToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double reduction)
            {
                return reduction switch
                {
                    >= 0.8 => Color.FromArgb("#C8E6C9"), // Green - Excellent
                    >= 0.6 => Color.FromArgb("#FFF9C4"), // Yellow - Good
                    >= 0.4 => Color.FromArgb("#FFCC80"), // Orange - Moderate
                    _ => Color.FromArgb("#FFCDD2")        // Red - Poor
                };
            }
            return Color.FromArgb("#F5F5F5");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ColorTemperatureToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double kelvin)
            {
                return kelvin switch
                {
                    < 3000 => Color.FromArgb("#FFB74D"),  // Warm orange
                    < 4000 => Color.FromArgb("#FFF176"),  // Warm yellow
                    < 5500 => Color.FromArgb("#F5F5F5"),  // Neutral white
                    < 7000 => Color.FromArgb("#E1F5FE"),  // Cool blue-white
                    _ => Color.FromArgb("#BBDEFB")         // Cool blue
                };
            }
            return Color.FromArgb("#F5F5F5");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TimeSpanToDurationStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan duration)
            {
                if (duration.TotalMinutes < 60)
                {
                    return $"{duration.Minutes}m";
                }
                else if (duration.TotalHours < 24)
                {
                    return $"{duration.Hours}h {duration.Minutes}m";
                }
                else
                {
                    return $"{duration.Days}d {duration.Hours}h";
                }
            }
            return "--";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ConfidenceToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double confidence)
            {
                return confidence switch
                {
                    >= 0.9 => Color.FromArgb("#4CAF50"),  // High confidence - Green
                    >= 0.7 => Color.FromArgb("#FF9800"),  // Medium confidence - Orange
                    >= 0.5 => Color.FromArgb("#F44336"),  // Low confidence - Red
                    _ => Color.FromArgb("#9E9E9E")         // Very low - Gray
                };
            }
            return Color.FromArgb("#9E9E9E");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LightQualityToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string lightQuality)
            {
                return lightQuality.ToLower() switch
                {
                    "golden hour" => "🌅",
                    "blue hour" => "🌆",
                    "harsh" => "☀️",
                    "soft" => "⛅",
                    "overcast" => "☁️",
                    "dramatic" => "⛈️",
                    _ => "🌤️"
                };
            }
            return "🌤️";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NullableDoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                var format = parameter?.ToString() ?? "F1";
                return doubleValue.ToString(format);
            }
            return "--";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && double.TryParse(stringValue, out double result))
            {
                return result;
            }
            return null;
        }
    }
}