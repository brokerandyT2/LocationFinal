using System.Globalization;

namespace Location.Core.Converters
{
    /// <summary>
    /// Converts a 0-100 score value to a color based on score quality.
    /// Higher scores get better colors (green), lower scores get worse colors (red).
    /// </summary>
    public class ScoreToColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a score (0-100) to a color representing quality.
        /// </summary>
        /// <param name="value">The score value, expected to be a double between 0 and 100.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">An optional parameter for the conversion.</param>
        /// <param name="culture">The culture to use in the conversion.</param>
        /// <returns>A Color representing the quality of the score.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double score)
            {
                // Clamp the score between 0 and 100
                var clampedScore = Math.Max(0, Math.Min(100, score));

                return clampedScore switch
                {
                    >= 80 => Color.FromArgb("#4CAF50"),  // Excellent - Green
                    >= 60 => Color.FromArgb("#8BC34A"),  // Good - Light Green
                    >= 40 => Color.FromArgb("#FF9800"),  // Fair - Orange
                    >= 20 => Color.FromArgb("#FF5722"),  // Poor - Red-Orange
                    _ => Color.FromArgb("#F44336")        // Very Poor - Red
                };
            }

            return Color.FromArgb("#9E9E9E"); // Default gray for invalid values
        }

        /// <summary>
        /// ConvertBack is not supported for this converter.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ScoreToColorConverter does not support ConvertBack");
        }
    }
}