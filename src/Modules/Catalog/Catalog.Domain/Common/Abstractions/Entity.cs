using System;
using System.Collections.Generic;
using System.Text;

namespace Catalog.Domain.Common.Abstractions
{
    public abstract class Entity<TKey> : IEquatable<Entity<TKey>>
    {
        public required TKey Id { get; init; }

        public bool Equals(Entity<TKey>? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (GetType() != other.GetType())
                return false;

            return EqualityComparer<TKey>.Default.Equals(Id, other.Id);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Entity<TKey>);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(GetType(), Id);
        }

        public static bool operator ==(Entity<TKey>? left, Entity<TKey>? right)
        {
            return EqualityComparer<Entity<TKey>?>.Default.Equals(left, right);
        }

        public static bool operator !=(Entity<TKey>? left, Entity<TKey>? right)
        {
            return !(left == right);
        }
    }
}