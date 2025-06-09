using System.Globalization;

namespace Location.Core.Converters
{
    /// <summary>
    /// Converts a <see langword="bool"/> value to a <see cref="Color"/> and vice versa.
    /// </summary>
    /// <remarks>This converter maps <see langword="true"/> to the <see cref="TrueColor"/> and <see
    /// langword="false"/> to the <see cref="FalseColor"/>. When converting back, it checks if the input <see
    /// cref="Color"/> matches the <see cref="TrueColor"/>.</remarks>
    public class BoolToColorConverter : IValueConverter
    {
        public Color TrueColor { get; set; } = Colors.Green;
        public Color FalseColor { get; set; } = Colors.Red;
        /// <summary>
        /// Converts a boolean value to a corresponding color based on the specified logic.
        /// </summary>
        /// <param name="value">The value to convert. Must be of type <see langword="bool"/>.</param>
        /// <param name="targetType">The type of the binding target property. This parameter is not used in the conversion.</param>
        /// <param name="parameter">An optional parameter to use in the conversion. This parameter is not used in the conversion.</param>
        /// <param name="culture">The culture to use in the conversion. This parameter is not used in the conversion.</param>
        /// <returns>The color associated with <see langword="true"/> if <paramref name="value"/> is <see langword="true"/>;
        /// otherwise, the color associated with <see langword="false"/>. Returns the color for <see langword="false"/>
        /// if <paramref name="value"/> is not a <see langword="bool"/>.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueColor : FalseColor;
            }

            return FalseColor;
        }
        /// <summary>
        /// Converts a <see cref="Color"/> value back to a boolean indicating whether it matches the specified true
        /// color.
        /// </summary>
        /// <param name="value">The value to convert back, expected to be of type <see cref="Color"/>.</param>
        /// <param name="targetType">The type to convert to. This parameter is not used in this implementation.</param>
        /// <param name="parameter">An optional parameter for the conversion. This parameter is not used in this implementation.</param>
        /// <param name="culture">The culture to use in the conversion. This parameter is not used in this implementation.</param>
        /// <returns><see langword="true"/> if the <paramref name="value"/> is a <see cref="Color"/> and equals the true color;
        /// otherwise, <see langword="false"/>.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return color.Equals(TrueColor);
            }

            return false;
        }
    }
}
