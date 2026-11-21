using BuildingBlocks.Domain.Bases;
using Identity.Domain.Parties;
using System.Diagnostics.CodeAnalysis;

namespace Identity.Domain.Suppliers
{
    public class Supplier : AggregateRoot<PartyId>
    {
        // A supplier is a company/business, so its (trade) name lives here.
        public string Name { get; private set; } = string.Empty;
        public string? Email { get; private set; }
        public string? Address { get; private set; }

        public Party Party { get; private set; } = null!;

        private Supplier() { }

        [SetsRequiredMembers]
        public Supplier(PartyId partyId, string name, string? email = null, string? address = null)
        {
            if (partyId == default)
                throw new ArgumentException("PartyId cannot be empty.", nameof(partyId));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Supplier name cannot be empty.", nameof(name));

            // The Supplier's identity IS the Party's identity (shared primary key).
            Id = partyId;
            Name = name.Trim();
            Email = email?.Trim();
            Address = address?.Trim();
        }

        public static Supplier Create(PartyId partyId, string name, string? email = null, string? address = null)
        {
            return new Supplier(partyId, name, email, address);
        }

        public void UpdateProfile(string name, string? email = null, string? address = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Supplier name cannot be empty.", nameof(name));

            Name = name.Trim();
            Email = email?.Trim();
            Address = address?.Trim();
        }
    }
}
