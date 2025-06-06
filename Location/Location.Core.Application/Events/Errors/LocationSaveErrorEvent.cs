namespace Location.Core.Application.Events.Errors
{
    public class LocationSaveErrorEvent : DomainErrorEvent
    {
        public string LocationTitle { get; }
        public LocationErrorType ErrorType { get; }
        public string? AdditionalContext { get; }

        public LocationSaveErrorEvent(string locationTitle, LocationErrorType errorType, string? additionalContext = null)
            : base("SaveLocationCommandHandler")
        {
            LocationTitle = locationTitle;
            ErrorType = errorType;
            AdditionalContext = additionalContext;
        }

        public override string GetResourceKey()
        {
            return ErrorType switch
            {
                LocationErrorType.DuplicateTitle => "Location_Error_DuplicateTitle",
                LocationErrorType.InvalidCoordinates => "Location_Error_InvalidCoordinates",
                LocationErrorType.NetworkError => "Location_Error_NetworkError",
                LocationErrorType.DatabaseError => "Location_Error_DatabaseError",
                LocationErrorType.ValidationError => "Location_Error_ValidationError",
                _ => "Location_Error_Unknown"
            };
        }

        public override Dictionary<string, object> GetParameters()
        {
            var parameters = new Dictionary<string, object>
            {
                { "LocationTitle", LocationTitle }
            };

            if (!string.IsNullOrEmpty(AdditionalContext))
            {
                parameters.Add("AdditionalContext", AdditionalContext);
            }

            return parameters;
        }
    }

    public enum LocationErrorType
    {
        DuplicateTitle,
        InvalidCoordinates,
        NetworkError,
        DatabaseError,
        ValidationError
    }
}