using BuildingBlocks.Domain.Bases;
using Identity.Domain.Parties;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Identity.Domain.Customers
{
    public class Customer : AggregateRoot<PartyId>
    {
        public int LoyaltyPoint { get; private set; }

        public Party Party { get; private set; } = null!;

        private Customer() { }

        [SetsRequiredMembers]
        public Customer(PartyId partyId, int loyaltyPoint = 0)
        {
            if (partyId == default)
                throw new ArgumentException("PartyId cannot be empty.", nameof(partyId));

            if (loyaltyPoint < 0)
                throw new ArgumentException("Loyalty point cannot be negative.", nameof(loyaltyPoint));

            // The Customer's identity IS the Party's identity (shared primary key).
            Id = partyId;
            LoyaltyPoint = loyaltyPoint;
        }

        public static Customer Create(PartyId partyId, int loyaltyPoint = 0)
        {
            return new Customer(partyId, loyaltyPoint);
        }

        public void AddLoyaltyPoint(int points)
        {
            if (points <= 0)
                throw new ArgumentException("Points to add must be greater than zero.", nameof(points));

            LoyaltyPoint += points;
        }

        public void DeductLoyaltyPoint(int points)
        {
            if (points <= 0)
                throw new ArgumentException("Points to deduct must be greater than zero.", nameof(points));

            if (points > LoyaltyPoint)
                throw new InvalidOperationException("Insufficient loyalty points.");

            LoyaltyPoint -= points;
        }

        public void UpdateLoyaltyPoint(int points)
        {
            if (points < 0)
                throw new ArgumentException("Loyalty point cannot be negative.", nameof(points));

            LoyaltyPoint = points;
        }
    }
}
