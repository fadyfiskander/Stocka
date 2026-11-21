using BuildingBlocks.Domain.Bases;
using System.Diagnostics.CodeAnalysis;

namespace Identity.Domain.Parties
{
    public class Party : AggregateRoot<PartyId>
    {
        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;
        public string PhoneNumber { get; private set; } = string.Empty;

        private readonly List<PartyOrganization> _partyOrganizations = new();
        public ICollection<PartyOrganization> PartyOrganizations => _partyOrganizations.AsReadOnly();

        private Party() { }

        [SetsRequiredMembers]
        public Party(Guid organizationId, string firstName, string lastName, string phoneNumber)
        {

            if (organizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId cannot be empty.", nameof(organizationId));

            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("First Name cannot be empty.", nameof(firstName));

            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Last Name cannot be empty.", nameof(lastName));

            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("PhoneNumber cannot be empty.", nameof(phoneNumber));

            Id = PartyId.New();
            FirstName = firstName.Trim();
            LastName = lastName.Trim();
            PhoneNumber = phoneNumber.Trim();

            _partyOrganizations.Add(PartyOrganization.Create(Id, organizationId));
        }

        public static Party Create(Guid organizationId, string firstName, string lastName, string phoneNumber)
        {
            return new Party(organizationId, firstName, lastName, phoneNumber);
        }

        public void UpdateName(string firstName, string lastName)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("First Name cannot be empty.", nameof(firstName));

            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Last Name cannot be empty.", nameof(lastName));

            FirstName = firstName.Trim();
            LastName = lastName.Trim();
        }

        public void UpdatePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("PhoneNumber cannot be empty.", nameof(phoneNumber));

            PhoneNumber = phoneNumber.Trim();
        }

        public void AssignOrganization(Guid organizationId)
        {
            if (organizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId cannot be empty.", nameof(organizationId));

            _partyOrganizations.Add(PartyOrganization.Create(Id, organizationId));
        }

        public void UnassignOrganization(Guid organizationId)
        {
            if (organizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId cannot be empty.", nameof(organizationId));

            var membership = _partyOrganizations
                .FirstOrDefault(po => po.Organization == organizationId);

            if (membership is not null)
                _partyOrganizations.Remove(membership);
        }
    }
}
