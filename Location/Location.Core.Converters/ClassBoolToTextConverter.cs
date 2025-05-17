using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Location.Core.Converters
{
    public class BoolToTextConverter : IValueConverter
    {
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