using Microsoft.EntityFrameworkCore;
using Organization.Application.Repositories.Commands;
using Organization.Domain.Organizations;
using Organization.Infrastructure.Contexts;
using Organization.Infrastructure.Repositories.Commands.Bases;

namespace Organization.Infrastructure.Repositories.Commands
{
    // Alias: the aggregate type clashes with the Organization root namespace.
    using OrganizationAggregate = Organization.Domain.Organizations.Organization;

    /// <summary>
    /// Write-side repository for the Organization aggregate.
    /// <para>
    /// The aggregate consistency rule: an <c>Organization</c> must always load together with its
    /// <c>OrganizationMemberships</c> — membership mutations (<c>AddMembership</c> /
    /// <c>RemoveMembership</c>) operate on the in-memory collection.
    /// </para>
    /// </summary>
    internal sealed class OrganizationRepository : Repository<OrganizationAggregate, OrganizationId>, IOrganizationRepository
    {
        public OrganizationRepository(OrganizationDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Always returns the full aggregate (root + memberships), so callers never
        /// accidentally work against a partially loaded Organization.
        /// </summary>
        public override async Task<OrganizationAggregate?> GetByIdAsync(OrganizationId id)
        {
            return await _set
                .Include(o => o.OrganizationMemberships)
                .FirstOrDefaultAsync(o => o.Id.Equals(id));
        }

        /// <summary>
        /// Explicit intent-named variant of <see cref="GetByIdAsync"/>.
        /// Kept separate on the contract so calling code reads clearly at the use-case level.
        /// </summary>
        public Task<OrganizationAggregate?> GetWithMembershipsAsync(OrganizationId organizationId)
            => GetByIdAsync(organizationId);

        /// <summary>Exact slug match, used to prevent duplicate organization slugs.</summary>
        public async Task<OrganizationAggregate?> GetBySlugAsync(string slug)
            => await _set.FirstOrDefaultAsync(o => o.Slug == slug);

        public async Task<bool> ExistsAsync(OrganizationId organizationId)
            => await _set.AnyAsync(o => o.Id.Equals(organizationId));
    }
}
