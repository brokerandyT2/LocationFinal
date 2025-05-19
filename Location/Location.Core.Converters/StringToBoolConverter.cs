using System.Globalization;

namespace Location.Core.Converters
{
    /// <summary>
    /// Provides a value converter that converts a string to a boolean value.
    /// </summary>
    /// <remarks>This converter returns <see langword="true"/> if the input value is a non-empty string; 
    /// otherwise, it returns <see langword="false"/>. The <see cref="ConvertBack"/> method is not implemented  and will
    /// throw a <see cref="NotImplementedException"/> if called.</remarks>
    public class StringToBoolConverter : IValueConverter
    {
        /// <summary>
        /// Converts the specified value to a boolean indicating whether the input string is not null or empty.
        /// </summary>
        /// <remarks>If the input <paramref name="value"/> is not of type <see cref="string"/>, the method
        /// returns <see langword="false"/>.</remarks>
        /// <param name="value">The value to convert. Expected to be of type <see cref="string"/>.</param>
        /// <param name="targetType">The type to convert to. This parameter is not used in the conversion.</param>
        /// <param name="parameter">An optional parameter for the conversion. This parameter is not used in the conversion.</param>
        /// <param name="culture">The culture to use in the conversion. This parameter is not used in the conversion.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is a non-null, non-empty string; otherwise, <see
        /// langword="false"/>.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return !string.IsNullOrEmpty(stringValue);
            }
            return false;
        }
        /// <summary>
        /// Converts a value back to its source type in a data binding scenario.
        /// </summary>
        /// <param name="value">The value produced by the binding target to be converted back.</param>
        /// <param name="targetType">The type to which the value should be converted.</param>
        /// <param name="parameter">An optional parameter to use during the conversion process.</param>
        /// <param name="culture">The culture to use during the conversion process.</param>
        /// <returns>The converted value, typically of the source type expected by the binding source.</returns>
        /// <exception cref="NotImplementedException">This method is not implemented and will always throw this exception.</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}