using System;

namespace Location.Core.Domain.Exceptions
{
    /// <summary>
    /// Base exception for domain-specific errors
    /// </summary>
    public class LocationDomainException : Exception
    {
        public string Code { get; }
        /// <summary>
        /// Represents an exception that occurs within the location domain.
        /// </summary>
        /// <param name="message">The error message that describes the exception.</param>
        /// <param name="code">The error code associated with the exception. Defaults to <see langword="DOMAIN_ERROR"/>.</param>
        public LocationDomainException(string message, string code = "DOMAIN_ERROR")
            : base(message)
        {
            Code = code;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="LocationDomainException"/> class with a specified error
        /// message, a reference to the inner exception that caused this exception, and an optional error code.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or <see langword="null"/> if no inner exception is
        /// specified.</param>
        /// <param name="code">An optional error code that identifies the specific domain error. Defaults to <c>"DOMAIN_ERROR"</c> if not
        /// provided.</param>
        public LocationDomainException(string message, Exception innerException, string code = "DOMAIN_ERROR")
            : base(message, innerException)
        {
            Code = code;
        }
    }
}