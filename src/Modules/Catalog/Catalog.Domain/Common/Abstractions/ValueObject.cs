using System;
using System.Collections.Generic;
using System.Text;

namespace Catalog.Domain.Common.Abstractions
{
    public abstract class ValueObject : IEquatable<ValueObject>
    {
        protected abstract IEnumerable<object?> GetEqualityComponents();

        public bool Equals(ValueObject? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (GetType() != other.GetType())
                return false;

            return GetEqualityComponents()
                .SequenceEqual(other.GetEqualityComponents());
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ValueObject);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            foreach (var component in GetEqualityComponents())
                hash.Add(component);

            return hash.ToHashCode();
        }

        public static bool operator ==(ValueObject? left, ValueObject? right)
            => EqualityComparer<ValueObject?>.Default.Equals(left, right);

        public static bool operator !=(ValueObject? left, ValueObject? right)
            => !(left == right);
    }
}
