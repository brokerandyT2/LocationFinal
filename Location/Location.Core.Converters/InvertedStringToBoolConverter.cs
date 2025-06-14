using System.Globalization;

namespace Location.Core.Converters
{
    /// <summary>
    /// Provides a value converter that converts a string to an inverted boolean value.
    /// </summary>
    /// <remarks>This converter returns <see langword="true"/> if the input value is null or an empty string; 
    /// otherwise, it returns <see langword="false"/>. This is the inverse of <see cref="StringToBoolConverter"/>.
    /// The <see cref="ConvertBack"/> method is not implemented and will throw a <see cref="NotImplementedException"/> if called.</remarks>
    public class InvertedStringToBoolConverter : IValueConverter
    {
        /// <summary>
        /// Converts the specified value to a boolean indicating whether the input string is null or empty.
        /// </summary>
        /// <remarks>If the input <paramref name="value"/> is not of type <see cref="string"/>, the method
        /// returns <see langword="true"/>.</remarks>
        /// <param name="value">The value to convert. Expected to be of type <see cref="string"/>.</param>
        /// <param name="targetType">The type to convert to. This parameter is not used in the conversion.</param>
        /// <param name="parameter">An optional parameter for the conversion. This parameter is not used in the conversion.</param>
        /// <param name="culture">The culture to use in the conversion. This parameter is not used in the conversion.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is null, empty string, or not a string; 
        /// <see langword="false"/> if it's a non-empty string.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return string.IsNullOrEmpty(stringValue);
            }
            return true; // Non-string values are considered "empty"
        }

        /// <summary>
        /// Converts a boolean value back to a string representation.
        /// </summary>
        /// <param name="value">The value produced by the binding target to be converted back.</param>
        /// <param name="targetType">The type to which the value should be converted.</param>
        /// <param name="parameter">An optional parameter to use during the conversion process.</param>
        /// <param name="culture">The culture to use during the conversion process.</param>
        /// <returns>An empty string if <paramref name="value"/> is <see langword="true"/>, "false" if <see langword="false"/>.</returns>
        /// <exception cref="NotImplementedException">This method is not implemented and will always throw this exception.</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("InvertedStringToBoolConverter does not support ConvertBack");
        }
    }
}