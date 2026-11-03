using BuildingBlocks.Application.Common.Interfaces;
using Organization.Domain.Organizations;

namespace Organization.Application.Repositories.Commands
{
    // Alias: Organization clashes with the namespace (Organization.Domain.Organization)
    using OrganizationAggregate = Organization.Domain.Organizations.Organization;

    /// <summary>
    /// Write-side repository for the Organization aggregate.
    /// <para>
    /// Membership rows (<c>OrganizationMembership</c>) are owned by the aggregate —
    /// they are mutated only through <c>AddMembership</c>/<c>RemoveMembership</c>
    /// on a loaded aggregate, never with a standalone write repository.
    /// </para>
    /// </summary>
    public interface IOrganizationRepository : IRepository<OrganizationAggregate, OrganizationId>
    {
        /// <summary>Exact slug match, used to prevent duplicate organization slugs.</summary>
        Task<OrganizationAggregate?> GetBySlugAsync(string slug);

        /// <summary>
        /// Loads the aggregate including its <c>OrganizationMemberships</c>.
        /// Required before calling <c>AddMembership</c>/<c>RemoveMembership</c>,
        /// because membership mutations operate on the in-memory collection.
        /// </summary>
        Task<OrganizationAggregate?> GetWithMembershipsAsync(OrganizationId organizationId);

        /// <summary>True when an Organization with this id exists (cheap existence check).</summary>
        Task<bool> ExistsAsync(OrganizationId organizationId);
    }
}
