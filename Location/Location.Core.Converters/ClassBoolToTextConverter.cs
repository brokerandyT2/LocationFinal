using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Location.Core.Converters
{
    /// <summary>
    /// Converts a <see langword="bool"/> value to a corresponding text representation and vice versa.
    /// </summary>
    /// <remarks>This converter is typically used in data binding scenarios to display text based on a boolean
    /// value. The text representation can be customized by providing a parameter in the format "TrueText|FalseText". If
    /// no parameter is provided, the default values "True" and "False" are used.</remarks>
    public class BoolToTextConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to a string representation based on the provided parameter.
        /// </summary>
        /// <remarks>If the <paramref name="parameter"/> is provided in the format "TrueText|FalseText",
        /// the method uses the specified text for <see langword="true"/> and <see langword="false"/> values,
        /// respectively. If the format is invalid or not provided, the method defaults to returning "True" for <see
        /// langword="true"/> and "False" for <see langword="false"/>.</remarks>
        /// <param name="value">The value to convert. Must be of type <see langword="bool"/>.</param>
        /// <param name="targetType">The target type of the conversion. This parameter is not used.</param>
        /// <param name="parameter">An optional string parameter specifying the text to return for <see langword="true"/> and <see
        /// langword="false"/> values. The format should be "TrueText|FalseText". If not provided or invalid, default
        /// values "True" and "False" are used.</param>
        /// <param name="culture">The culture to use in the conversion. This parameter is not used.</param>
        /// <returns>A string representation of the boolean value. Returns an empty string if <paramref name="value"/> is not a
        /// boolean.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not bool boolValue)
                return string.Empty;

            if (parameter is not string parameterString)
                return boolValue ? "True" : "False";

            // Expected format: "TrueText|FalseText"
            var textOptions = parameterString.Split('|');
            if (textOptions.Length != 2)
                return boolValue ? "True" : "False";

            return boolValue ? textOptions[0] : textOptions[1];
        }
        /// <summary>
        /// Converts a string value back to a boolean based on the provided parameter.
        /// </summary>
        /// <param name="value">The value to convert, expected to be a string.</param>
        /// <param name="targetType">The type of the binding target property. This parameter is not used in this implementation.</param>
        /// <param name="parameter">An optional string parameter specifying the expected true and false text values, separated by a pipe ('|').
        /// For example, "Yes|No" would interpret "Yes" as true and "No" as false. If not provided, "True" and "False"
        /// are used by default.</param>
        /// <param name="culture">The culture to use in the conversion. This parameter is not used in this implementation.</param>
        /// <returns>A boolean value indicating whether the input string matches the true text.  Returns <see langword="false"/>
        /// if the input is not a string or if the parameter format is invalid.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string stringValue)
                return false;

            if (parameter is not string parameterString)
                return stringValue.Equals("True", StringComparison.OrdinalIgnoreCase);

            // Expected format: "TrueText|FalseText"
            var textOptions = parameterString.Split('|');
            if (textOptions.Length != 2)
                return stringValue.Equals("True", StringComparison.OrdinalIgnoreCase);

            return stringValue.Equals(textOptions[0], StringComparison.OrdinalIgnoreCase);
        }
    }
}