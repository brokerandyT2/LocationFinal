using Location.Photography.Application.Resources;

namespace Location.Photography.Application.Errors
{
    /// <summary>
    /// Base class for exposure-related errors
    /// </summary>
    public abstract class ExposureError : Exception
    {
        public ExposureError(string message) : base(message)
        {
        }

        public ExposureError(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Error thrown when the calculated exposure is too bright (overexposed)
    /// </summary>
    public class OverexposedError : ExposureError
    {
        public double StopsOverexposed { get; }

        public OverexposedError(double stopsOverexposed)
            : base(string.Format(AppResources.Exposure_Error_Overexposed, stopsOverexposed))
        {
            StopsOverexposed = stopsOverexposed;
        }
    }

    /// <summary>
    /// Error thrown when the calculated exposure is too dark (underexposed)
    /// </summary>
    public class UnderexposedError : ExposureError
    {
        public double StopsUnderexposed { get; }

        public UnderexposedError(double stopsUnderexposed)
            : base(string.Format(AppResources.Exposure_Error_Underexposed, stopsUnderexposed))
        {
            StopsUnderexposed = stopsUnderexposed;
        }
    }

    /// <summary>
    /// Error thrown when a parameter exceeds the physical limits of the camera
    /// </summary>
    public class ExposureParameterLimitError : ExposureError
    {
        public string ParameterName { get; }
        public string RequestedValue { get; }
        public string AvailableLimit { get; }

        public ExposureParameterLimitError(string parameterName, string requestedValue, string availableLimit)
            : base(string.Format(AppResources.Exposure_Error_ParameterLimit, parameterName, requestedValue, availableLimit))
        {
            ParameterName = parameterName;
            RequestedValue = requestedValue;
            AvailableLimit = availableLimit;
        }
    }
}