using System;
using System.Collections.Generic;

namespace Location.Core.Domain.ValueObjects
{
    /// <summary>
    /// Value object representing a physical address
    /// </summary>
    public class Address : ValueObject
    {
        public string City { get; private set; }
        public string State { get; private set; }

        public Address(string city, string state)
        {
            City = city ?? string.Empty;
            State = state ?? string.Empty;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return City.ToUpperInvariant();
            yield return State.ToUpperInvariant();
        }

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(City) && string.IsNullOrWhiteSpace(State))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(State))
                return City;

            if (string.IsNullOrWhiteSpace(City))
                return State;

            return $"{City}, {State}";
        }
    }
}