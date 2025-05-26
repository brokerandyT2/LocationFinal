using Location.Core.Application.Common.Interfaces;
using System.Globalization;

namespace Location.Core.Converters
{
    /// <summary>
    /// Converter that returns the temperature scale (F or C) based on user settings
    /// </summary>
    public class TemperatureScaleConverter : IValueConverter
    {
        private static string _cachedScale = "C"; // Default to Celsius
        private static DateTime _lastCacheUpdate = DateTime.MinValue;
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5); // Cache for 5 minutes

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if we need to refresh the cache
            if (DateTime.UtcNow - _lastCacheUpdate > CacheExpiry)
            {
                // Try to get the current temperature scale setting
                Task.Run(async () => await RefreshTemperatureScaleAsync());
            }

            return _cachedScale;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("TemperatureScaleConverter does not support ConvertBack");
        }

        private static async Task RefreshTemperatureScaleAsync()
        {
            try
            {
                // Get the service provider from the current application
                if (Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services != null)
                {
                    var serviceProvider = Microsoft.Maui.Controls.Application.Current.Handler.MauiContext.Services;
                    var unitOfWork = serviceProvider.GetService<IUnitOfWork>();

                    if (unitOfWork != null)
                    {
                        var tempScaleSetting = await unitOfWork.Settings.GetByKeyAsync("TemperatureType");
                        if (tempScaleSetting.IsSuccess && tempScaleSetting.Data != null)
                        {
                            _cachedScale = tempScaleSetting.Data.Value == "F" ? "F" : "C";
                        }
                        else
                        {
                            _cachedScale = "C"; // Default to Celsius if setting not found
                        }

                        _lastCacheUpdate = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception)
            {
                // If anything fails, keep the current cached value
                // This ensures the UI doesn't break if settings are unavailable
            }
        }

        /// <summary>
        /// Forces a refresh of the temperature scale cache
        /// Call this method when temperature settings are changed
        /// </summary>
        public static async Task ForceRefreshAsync()
        {
            _lastCacheUpdate = DateTime.MinValue; // Force cache refresh
            await RefreshTemperatureScaleAsync();
        }
    }
}