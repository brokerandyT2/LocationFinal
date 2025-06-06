namespace Location.Core.Domain.ValueObjects
{
    /// <summary>
    /// Base class for value objects
    /// </summary>
    public abstract class ValueObject
    {
        /// <summary>
        /// Determines whether two <see cref="ValueObject"/> instances are equal.
        /// </summary>
        /// <param name="left">The first <see cref="ValueObject"/> instance to compare, or <see langword="null"/>.</param>
        /// <param name="right">The second <see cref="ValueObject"/> instance to compare, or <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both instances are equal or both are <see langword="null"/>;  otherwise, <see
        /// langword="false"/>.</returns>
        protected static bool EqualOperator(ValueObject? left, ValueObject? right)
        {
            if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null))
            {
                return false;
            }
            return ReferenceEquals(left, null) || left.Equals(right);
        }
        /// <summary>
        /// Determines whether two <see cref="ValueObject"/> instances are not equal.
        /// </summary>
        /// <param name="left">The first <see cref="ValueObject"/> to compare. Can be <see langword="null"/>.</param>
        /// <param name="right">The second <see cref="ValueObject"/> to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the two <see cref="ValueObject"/> instances are not equal; otherwise, <see
        /// langword="false"/>.</returns>
        protected static bool NotEqualOperator(ValueObject? left, ValueObject? right)
        {
            return !EqualOperator(left, right);
        }
        /// <summary>
        /// Provides the components that define equality for the derived type.
        /// </summary>
        /// <remarks>This method should be overridden in derived classes to return a sequence of objects
        /// that represent the significant fields or properties used to determine equality. The returned components are
        /// compared in sequence to evaluate equality.</remarks>
        /// <returns>An <see cref="IEnumerable{T}"/> of objects representing the equality components.</returns>
        protected abstract IEnumerable<object> GetEqualityComponents();
        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <remarks>Two objects are considered equal if they are of the same type and their equality
        /// components  are equal in sequence. This method performs a type check and compares the equality components 
        /// of the objects.</remarks>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><see langword="true"/> if the specified object is equal to the current object;  otherwise, <see
        /// langword="false"/>.</returns>
        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            var other = (ValueObject)obj;
            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }
        /// <summary>
        /// Returns a hash code for the current object based on its equality components.
        /// </summary>
        /// <remarks>The hash code is computed by combining the hash codes of the equality components 
        /// using a bitwise XOR operation. This ensures that objects with the same equality  components produce the same
        /// hash code.</remarks>
        /// <returns>An integer representing the hash code of the current object.</returns>
        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Select(x => x?.GetHashCode() ?? 0)
                .Aggregate((x, y) => x ^ y);
        }

        public static bool operator ==(ValueObject? one, ValueObject? two)
        {
            return EqualOperator(one, two);
        }

        public static bool operator !=(ValueObject? one, ValueObject? two)
        {
            return NotEqualOperator(one, two);
        }
    }
}