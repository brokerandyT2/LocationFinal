using System.Collections;
using System.Globalization;

namespace Location.Core.Converters
{
    /// <summary>
    /// Converts a collection to a boolean value indicating whether the collection contains items.
    /// </summary>
    /// <remarks>This converter returns <see langword="true"/> if the input collection is not null and contains 
    /// at least one item; otherwise, it returns <see langword="false"/>. The <see cref="ConvertBack"/> method 
    /// is not implemented and will throw a <see cref="NotImplementedException"/> if called.</remarks>
    public class CollectionToBoolConverter : IValueConverter
    {
        /// <summary>
        /// Converts the specified collection to a boolean indicating whether it contains items.
        /// </summary>
        /// <remarks>If the input <paramref name="value"/> is not a collection type (IEnumerable), 
        /// the method returns <see langword="false"/>.</remarks>
        /// <param name="value">The value to convert. Expected to implement <see cref="IEnumerable"/>.</param>
        /// <param name="targetType">The type to convert to. This parameter is not used in the conversion.</param>
        /// <param name="parameter">An optional parameter for the conversion. This parameter is not used in the conversion.</param>
        /// <param name="culture">The culture to use in the conversion. This parameter is not used in the conversion.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is a non-null collection with at least one item; 
        /// otherwise, <see langword="false"/>.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            if (value is IEnumerable enumerable)
            {
                // Check if the enumerable has any items
                foreach (var item in enumerable)
                {
                    return true; // Found at least one item
                }
                return false; // No items found
            }

            return false; // Not a collection
        }

        /// <summary>
        /// Converts a boolean value back to a collection representation.
        /// </summary>
        /// <param name="value">The value produced by the binding target to be converted back.</param>
        /// <param name="targetType">The type to which the value should be converted.</param>
        /// <param name="parameter">An optional parameter to use during the conversion process.</param>
        /// <param name="culture">The culture to use during the conversion process.</param>
        /// <returns>This method is not implemented.</returns>
        /// <exception cref="NotImplementedException">This method is not implemented and will always throw this exception.</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("CollectionToBoolConverter does not support ConvertBack");
        }
    }
}