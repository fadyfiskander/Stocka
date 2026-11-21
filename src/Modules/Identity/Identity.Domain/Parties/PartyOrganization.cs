namespace Identity.Domain.Parties
{
    public class PartyOrganization
    {
        public PartyId PartyId { get; private set; }
        public Guid Organization { get; private set; }

        public Party Party { get; private set; } = null!;

        private PartyOrganization() { }

        private PartyOrganization(PartyId partyId, Guid organizationId)
        {
            if (organizationId == Guid.Empty)
                throw new ArgumentNullException(nameof(organizationId), "Organization Id cannot be null or empty");

            PartyId = partyId;
            Organization = organizationId;
        }

        public static PartyOrganization Create(PartyId partyId, Guid organizationId)
        {
            return new PartyOrganization(partyId, organizationId);
        }
    }
}
