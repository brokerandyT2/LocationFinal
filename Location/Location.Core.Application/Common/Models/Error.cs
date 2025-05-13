namespace Location.Core.Application.Common.Models
{
    /// <summary>
    /// Represents an error in the application
    /// </summary>
    public class Error
    {
        /// <summary>
        /// Error code for identifying the type of error
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Human-readable error message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Property name associated with the error (for validation errors)
        /// </summary>
        public string? PropertyName { get; }

        /// <summary>
        /// Creates a new error
        /// </summary>
        /// <param name="code">Error code</param>
        /// <param name="message">Error message</param>
        /// <param name="propertyName">Optional property name</param>
        public Error(string code, string message, string? propertyName = null)
        {
            Code = code;
            Message = message;
            PropertyName = propertyName;
        }

        /// <summary>
        /// Creates a validation error
        /// </summary>
        public static Error Validation(string propertyName, string message)
        {
            return new Error("VALIDATION_ERROR", message, propertyName);
        }

        /// <summary>
        /// Creates a not found error
        /// </summary>
        public static Error NotFound(string message)
        {
            return new Error("NOT_FOUND", message);
        }

        /// <summary>
        /// Creates a database error
        /// </summary>
        public static Error Database(string message)
        {
            return new Error("DATABASE_ERROR", message);
        }

        /// <summary>
        /// Creates a general domain error
        /// </summary>
        public static Error Domain(string message)
        {
            return new Error("DOMAIN_ERROR", message);
        }
    }
}