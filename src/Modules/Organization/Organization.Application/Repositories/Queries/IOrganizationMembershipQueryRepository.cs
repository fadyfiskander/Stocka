using Organization.Contracts.DTOs;

namespace Organization.Application.Repositories.Queries
{
    /// <summary>Read-side (CQRS) queries over the organization membership rows.</summary>
    public interface IOrganizationMembershipQueryRepository
    {
        /// <summary>All membership rows of one organization.</summary>
        Task<IEnumerable<OrganizationMembershipDto>> GetByOrganizationAsync(Guid organizationId);

        /// <summary>All memberships of one user (the organizations he belongs to).</summary>
        Task<IEnumerable<OrganizationMembershipDto>> GetByUserAsync(Guid userId);

        /// <summary>True when the (organization, user) membership row exists.</summary>
        Task<bool> ExistsAsync(Guid organizationId, Guid userId);
    }
}
