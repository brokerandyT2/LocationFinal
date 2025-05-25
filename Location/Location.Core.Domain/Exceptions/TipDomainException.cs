using System;

namespace Location.Core.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when tip domain business rules are violated
    /// </summary>
    public class TipDomainException : Exception
    {
        /// <summary>
        /// Gets the error code that identifies the specific business rule violation
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Initializes a new instance of the TipDomainException class
        /// </summary>
        /// <param name="code">The error code identifying the business rule violation</param>
        /// <param name="message">The error message</param>
        public TipDomainException(string code, string message) : base(message)
        {
            Code = code;
        }

        /// <summary>
        /// Initializes a new instance of the TipDomainException class
        /// </summary>
        /// <param name="code">The error code identifying the business rule violation</param>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The exception that caused this exception</param>
        public TipDomainException(string code, string message, Exception innerException) : base(message, innerException)
        {
            Code = code;
        }
    }
}