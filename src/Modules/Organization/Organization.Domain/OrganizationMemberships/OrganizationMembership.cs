using BuildingBlocks.Domain.Enums;
using Organization.Domain.Organizations;

namespace Organization.Domain.OrganizationMemberships
{
    public class OrganizationMembership
    {
        public OrganizationId OrganizationId { get; private set; }
        public Guid UserId { get; private set; }
        public OrganizationRole Role { get; private set; }

        private OrganizationMembership(OrganizationId organizationId, Guid userId, OrganizationRole role)
        {
            if (organizationId.Value == Guid.Empty)
                throw new ArgumentException("OrganizationId cannot be empty.", nameof(organizationId));

            if (userId == Guid.Empty)
                throw new ArgumentException("UserId cannot be empty.", nameof(userId));

            OrganizationId = organizationId;
            UserId = userId;
            Role = role;
        }

        public static OrganizationMembership Create(OrganizationId organizationId, Guid userId, OrganizationRole role)
        {
            return new OrganizationMembership(organizationId, userId, role);
        }
    }
}