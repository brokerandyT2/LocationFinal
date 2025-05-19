using System;

namespace Location.Core.Domain.Common
{
    /// <summary>
    /// Base class for all domain entities
    /// </summary>
    public abstract class Entity
    {
        private int? _requestedHashCode;

        public int Id { get; protected set; }
        /// <summary>
        /// Determines whether the entity is considered transient.
        /// </summary>
        /// <returns><see langword="true"/> if the entity is transient (i.e., its <see cref="Id"/> is set to the default value); 
        /// otherwise, <see langword="false"/>.</returns>
        public bool IsTransient()
        {
            return Id == default;
        }
        /// <summary>
        /// Determines whether the specified object is equal to the current entity.
        /// </summary>
        /// <remarks>Two entities are considered equal if they are of the same type, are not transient, 
        /// and have the same identifier. If either entity is transient, they are not considered equal.</remarks>
        /// <param name="obj">The object to compare with the current entity.</param>
        /// <returns><see langword="true"/> if the specified object is equal to the current entity;  otherwise, <see
        /// langword="false"/>.</returns>
        public override bool Equals(object? obj)
        {
            if (obj == null || !(obj is Entity))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (GetType() != obj.GetType())
                return false;

            Entity item = (Entity)obj;

            if (item.IsTransient() || IsTransient())
                return false;
            else
                return item.Id == Id;
        }
        /// <summary>
        /// Serves as the default hash function for the object, providing a hash code based on the object's identifier.
        /// </summary>
        /// <remarks>If the object is not transient, the hash code is computed using the object's <see
        /// cref="Id"/> property  combined with a constant value to reduce collisions. For transient objects, the base
        /// implementation is used.</remarks>
        /// <returns>An integer hash code that can be used in hashing algorithms and data structures such as hash tables.</returns>
        public override int GetHashCode()
        {
            if (!IsTransient())
            {
                if (!_requestedHashCode.HasValue)
                    _requestedHashCode = Id.GetHashCode() ^ 31;

                return _requestedHashCode.Value;
            }
            else
                return base.GetHashCode();
        }
        /// <summary>
        /// Determines whether two <see cref="Entity"/> instances are equal.
        /// </summary>
        /// <param name="left">The first <see cref="Entity"/> to compare.</param>
        /// <param name="right">The second <see cref="Entity"/> to compare.</param>
        /// <returns><see langword="true"/> if the two <see cref="Entity"/> instances are equal; otherwise, <see
        /// langword="false"/>.</returns>
        public static bool operator ==(Entity left, Entity right)
        {
            if (Equals(left, null))
                return Equals(right, null);
            else
                return left.Equals(right);
        }
        /// <summary>
        /// Determines whether two <see cref="Entity"/> instances are not equal.
        /// </summary>
        /// <param name="left">The first <see cref="Entity"/> to compare.</param>
        /// <param name="right">The second <see cref="Entity"/> to compare.</param>
        /// <returns><see langword="true"/> if the two <see cref="Entity"/> instances are not equal;  otherwise, <see
        /// langword="false"/>.</returns>
        public static bool operator !=(Entity left, Entity right)
        {
            return !(left == right);
        }
    }
}