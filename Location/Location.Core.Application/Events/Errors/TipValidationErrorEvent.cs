using System.Collections.Generic;
using System.Linq;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Events.Errors
{
    public class TipValidationErrorEvent : DomainErrorEvent
    {
        public int? TipId { get; }
        public int TipTypeId { get; }
        public Dictionary<string, List<string>> ValidationErrors { get; }

        public TipValidationErrorEvent(int? tipId, int tipTypeId, IEnumerable<Error> errors, string source)
            : base(source)
        {
            TipId = tipId;
            TipTypeId = tipTypeId;
            ValidationErrors = errors
                .Where(e => !string.IsNullOrEmpty(e.PropertyName))
                .GroupBy(e => e.PropertyName!)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Message).ToList()
                );
        }

        public TipValidationErrorEvent(int? tipId, int tipTypeId, Dictionary<string, List<string>> validationErrors, string source)
            : base(source)
        {
            TipId = tipId;
            TipTypeId = tipTypeId;
            ValidationErrors = validationErrors;
        }

        public override string GetResourceKey()
        {
            return ValidationErrors.Count == 1
                ? "Tip_Validation_Error_Single"
                : "Tip_Validation_Error_Multiple";
        }

        public override Dictionary<string, object> GetParameters()
        {
            var parameters = new Dictionary<string, object>
            {
                { "TipTypeId", TipTypeId },
                { "ErrorCount", ValidationErrors.Count }
            };

            if (TipId.HasValue)
            {
                parameters.Add("TipId", TipId.Value);
            }

            if (ValidationErrors.Count == 1)
            {
                var firstError = ValidationErrors.First();
                parameters.Add("PropertyName", firstError.Key);
                parameters.Add("ErrorMessage", firstError.Value.First());
            }

            return parameters;
        }

        public override ErrorSeverity Severity => ErrorSeverity.Warning;
    }
}