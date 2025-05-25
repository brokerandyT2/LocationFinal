using System.Collections.Generic;
using System.Linq;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Events.Errors
{
    public class ValidationErrorEvent : DomainErrorEvent
    {
        public string EntityType { get; }
        public Dictionary<string, List<string>> ValidationErrors { get; }

        public ValidationErrorEvent(string entityType, IEnumerable<Error> errors, string source)
            : base(source)
        {
            EntityType = entityType;
            ValidationErrors = errors
                .Where(e => !string.IsNullOrEmpty(e.PropertyName))
                .GroupBy(e => e.PropertyName!)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Message).ToList()
                );
        }

        public ValidationErrorEvent(string entityType, Dictionary<string, List<string>> validationErrors, string source)
            : base(source)
        {
            EntityType = entityType;
            ValidationErrors = validationErrors;
        }

        public override string GetResourceKey()
        {
            return ValidationErrors.Count == 1
                ? "Validation_Error_Single"
                : "Validation_Error_Multiple";
        }

        public override Dictionary<string, object> GetParameters()
        {
            var parameters = new Dictionary<string, object>
            {
                { "EntityType", EntityType },
                { "ErrorCount", ValidationErrors.Count }
            };

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