using Organization.Application.Repositories.Queries;
using Organization.Contracts.DTOs;
using Organization.Contracts.Services;

namespace Organization.Application.Services
{
    internal class OrganizationAuthService : IOrganizationAuthService
    {
        private readonly IOrganizationMembershipQueryRepository _organizationMembershipQuery;

        public OrganizationAuthService(IOrganizationMembershipQueryRepository organizationMembershipQuery)
        {
            _organizationMembershipQuery = organizationMembershipQuery;
        }

        public async Task<IEnumerable<OrganizationAccessDto>> OrganizationAccessesAsync(Guid UserId, CancellationToken cancellationToken)
        {
            var organizations = await _organizationMembershipQuery.GetByUserAsync(UserId);

            var organizationAccesses = organizations.Select(o => new OrganizationAccessDto(
                o.OrganizationId,
                o.Name,
                o.Role
            ));

            return organizationAccesses;
        }
    }
}
