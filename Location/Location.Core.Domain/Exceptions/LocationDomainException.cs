using System;

namespace Location.Core.Domain.Exceptions
{
    /// <summary>
    /// Base exception for domain-specific errors
    /// </summary>
    public class LocationDomainException : Exception
    {
        public string Code { get; }

        public LocationDomainException(string message, string code = "DOMAIN_ERROR")
            : base(message)
        {
            Code = code;
        }

        public LocationDomainException(string message, Exception innerException, string code = "DOMAIN_ERROR")
            : base(message, innerException)
        {
            Code = code;
        }
    }
}