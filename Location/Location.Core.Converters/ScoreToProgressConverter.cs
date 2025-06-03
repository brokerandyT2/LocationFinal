using System.Globalization;

namespace Location.Core.Converters
{
    /// <summary>
    /// Converts a 0-100 score value to a 0-1 progress value for ProgressBar controls.
    /// </summary>
    public class ScoreToProgressConverter : IValueConverter
    {
        /// <summary>
        /// Converts a score (0-100) to a progress value (0-1).
        /// </summary>
        /// <param name="value">The score value, expected to be a double between 0 and 100.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">An optional parameter for the conversion.</param>
        /// <param name="culture">The culture to use in the conversion.</param>
        /// <returns>A double value between 0 and 1 representing the progress.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double score)
            {
                // Clamp the score between 0 and 100, then convert to 0-1 range
                var clampedScore = Math.Max(0, Math.Min(100, score));
                return clampedScore / 100.0;
            }

            return 0.0;
        }

        /// <summary>
        /// Converts a progress value (0-1) back to a score (0-100).
        /// </summary>
        /// <param name="value">The progress value, expected to be a double between 0 and 1.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">An optional parameter for the conversion.</param>
        /// <param name="culture">The culture to use in the conversion.</param>
        /// <returns>A double value between 0 and 100 representing the score.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double progress)
            {
                // Clamp the progress between 0 and 1, then convert to 0-100 range
                var clampedProgress = Math.Max(0, Math.Min(1, progress));
                return clampedProgress * 100.0;
            }

            return 0.0;
        }
    }
}