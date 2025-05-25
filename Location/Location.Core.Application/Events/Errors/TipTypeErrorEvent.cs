using System.Collections.Generic;

namespace Location.Core.Application.Events.Errors
{
    public class TipTypeErrorEvent : DomainErrorEvent
    {
        public string TipTypeName { get; }
        public int? TipTypeId { get; }
        public TipTypeErrorType ErrorType { get; }
        public string? AdditionalContext { get; }

        public TipTypeErrorEvent(string tipTypeName, int? tipTypeId, TipTypeErrorType errorType, string? additionalContext = null)
            : base("TipTypeCommandHandler")
        {
            TipTypeName = tipTypeName;
            TipTypeId = tipTypeId;
            ErrorType = errorType;
            AdditionalContext = additionalContext;
        }

        public override string GetResourceKey()
        {
            return ErrorType switch
            {
                TipTypeErrorType.DuplicateName => "TipType_Error_DuplicateName",
                TipTypeErrorType.TipTypeNotFound => "TipType_Error_NotFound",
                TipTypeErrorType.TipTypeInUse => "TipType_Error_InUse",
                TipTypeErrorType.InvalidName => "TipType_Error_InvalidName",
                TipTypeErrorType.DatabaseError => "TipType_Error_DatabaseError",
                _ => "TipType_Error_Unknown"
            };
        }

        public override Dictionary<string, object> GetParameters()
        {
            var parameters = new Dictionary<string, object>
            {
                { "TipTypeName", TipTypeName }
            };

            if (TipTypeId.HasValue)
            {
                parameters.Add("TipTypeId", TipTypeId.Value);
            }

            if (!string.IsNullOrEmpty(AdditionalContext))
            {
                parameters.Add("AdditionalContext", AdditionalContext);
            }

            return parameters;
        }
    }

    public enum TipTypeErrorType
    {
        DuplicateName,
        TipTypeNotFound,
        TipTypeInUse,
        InvalidName,
        DatabaseError
    }
}