namespace Location.Core.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when setting domain business rules are violated
    /// </summary>
    public class SettingDomainException : Exception
    {
        /// <summary>
        /// Gets the error code that identifies the specific business rule violation
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Initializes a new instance of the SettingDomainException class
        /// </summary>
        /// <param name="code">The error code identifying the business rule violation</param>
        /// <param name="message">The error message</param>
        public SettingDomainException(string code, string message) : base(message)
        {
            Code = code;
        }

        /// <summary>
        /// Initializes a new instance of the SettingDomainException class
        /// </summary>
        /// <param name="code">The error code identifying the business rule violation</param>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The exception that caused this exception</param>
        public SettingDomainException(string code, string message, Exception innerException) : base(message, innerException)
        {
            Code = code;
        }
    }
}