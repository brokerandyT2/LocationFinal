using System.Collections.Generic;

namespace Location.Core.Application.Events.Errors
{
    public class WeatherUpdateErrorEvent : DomainErrorEvent
    {
        public int LocationId { get; }
        public WeatherErrorType ErrorType { get; }
        public string? AdditionalContext { get; }

        public WeatherUpdateErrorEvent(int locationId, WeatherErrorType errorType, string? additionalContext = null)
            : base("UpdateWeatherCommandHandler")
        {
            LocationId = locationId;
            ErrorType = errorType;
            AdditionalContext = additionalContext;
        }

        public override string GetResourceKey()
        {
            return ErrorType switch
            {
                WeatherErrorType.ApiUnavailable => "Weather_Error_ApiUnavailable",
                WeatherErrorType.InvalidLocation => "Weather_Error_InvalidLocation",
                WeatherErrorType.NetworkTimeout => "Weather_Error_NetworkTimeout",
                WeatherErrorType.InvalidApiKey => "Weather_Error_InvalidApiKey",
                WeatherErrorType.RateLimitExceeded => "Weather_Error_RateLimitExceeded",
                WeatherErrorType.DatabaseError => "Weather_Error_DatabaseError",
                _ => "Weather_Error_Unknown"
            };
        }

        public override Dictionary<string, object> GetParameters()
        {
            var parameters = new Dictionary<string, object>
            {
                { "LocationId", LocationId }
            };

            if (!string.IsNullOrEmpty(AdditionalContext))
            {
                parameters.Add("AdditionalContext", AdditionalContext);
            }

            return parameters;
        }
    }

    public enum WeatherErrorType
    {
        ApiUnavailable,
        InvalidLocation,
        NetworkTimeout,
        InvalidApiKey,
        RateLimitExceeded,
        DatabaseError
    }
}