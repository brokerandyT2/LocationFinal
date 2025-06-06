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
        /// <summary>
        /// Provides the components used to determine equality for the current object.
        /// </summary>
        /// <remarks>This method returns an enumerable of objects that represent the significant 
        /// components of the object for equality comparison. The components are returned  in a consistent order to
        /// ensure reliable equality checks.</remarks>
        /// <returns>An <see cref="IEnumerable{T}"/> of objects representing the equality components of the current object.</returns>
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return City.ToUpperInvariant();
            yield return State.ToUpperInvariant();
        }
        /// <summary>
        /// Returns a string representation of the location, formatted as "City, State".
        /// </summary>
        /// <remarks>If both <see cref="City"/> and <see cref="State"/> are null, empty, or whitespace,
        /// the method returns an empty string. If only one of the two is provided, the method returns that value.
        /// Otherwise, the result is formatted as "City, State".</remarks>
        /// <returns>A string representing the location. Returns an empty string if both <see cref="City"/> and <see
        /// cref="State"/> are null, empty, or whitespace.</returns>
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