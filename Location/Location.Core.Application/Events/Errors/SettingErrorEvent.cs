namespace Location.Core.Application.Events.Errors
{
    public class SettingErrorEvent : DomainErrorEvent
    {
        public string SettingKey { get; }
        public SettingErrorType ErrorType { get; }
        public string? AdditionalContext { get; }

        public SettingErrorEvent(string settingKey, SettingErrorType errorType, string? additionalContext = null)
            : base("SettingCommandHandler")
        {
            SettingKey = settingKey;
            ErrorType = errorType;
            AdditionalContext = additionalContext;
        }

        public override string GetResourceKey()
        {
            return ErrorType switch
            {
                SettingErrorType.DuplicateKey => "Setting_Error_DuplicateKey",
                SettingErrorType.KeyNotFound => "Setting_Error_KeyNotFound",
                SettingErrorType.InvalidValue => "Setting_Error_InvalidValue",
                SettingErrorType.ReadOnlySetting => "Setting_Error_ReadOnlySetting",
                SettingErrorType.DatabaseError => "Setting_Error_DatabaseError",
                _ => "Setting_Error_Unknown"
            };
        }

        public override Dictionary<string, object> GetParameters()
        {
            var parameters = new Dictionary<string, object>
            {
                { "SettingKey", SettingKey }
            };

            if (!string.IsNullOrEmpty(AdditionalContext))
            {
                parameters.Add("AdditionalContext", AdditionalContext);
            }

            return parameters;
        }
    }

    public enum SettingErrorType
    {
        DuplicateKey,
        KeyNotFound,
        InvalidValue,
        ReadOnlySetting,
        DatabaseError
    }
}