using BuildingBlocks.Application.Common.Models;
using Organization.Contracts.DTOs;

namespace Organization.Application.Repositories.Queries
{
    /// <summary>Read-side (CQRS) queries for organization rows, projected to their contract DTOs.</summary>
    public interface IOrganizationQueryRepository
    {
        Task<OrganizationDto?> GetByIdAsync(Guid id);

        /// <summary>Exact slug match.</summary>
        Task<OrganizationDto?> GetBySlugAsync(string slug);

        /// <summary>Organizations whose name CONTAINS the search value.</summary>
        Task<IEnumerable<OrganizationDto>> GetByNameAsync(string name);

        Task<PaginatedResult<OrganizationDto>> GetPagedAsync(int page, int pageSize);
    }
}
