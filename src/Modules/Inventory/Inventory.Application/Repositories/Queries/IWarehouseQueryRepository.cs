using BuildingBlocks.Application.Common.Models;
using Inventory.Contracts.DTOs.Warehouse;
using Inventory.Primitives.Identifiers;

namespace Inventory.Application.Repositories.Queries
{
    public interface IWarehouseQueryRepository
    {
        Task<WarehouseDto?> GetByIdAsync(Guid organizationId, WarehouseId id);
        Task<IEnumerable<WarehouseDto>> GetAllAsync(Guid organizationId);

        Task<PaginatedResult<WarehouseDto>> GetPagedAsync(Guid organizationId, int page, int pageSize);

        /// <summary>Paged list, optionally filtered to a single activation state.</summary>
        Task<PaginatedResult<WarehouseDto>> GetPagedAsync(Guid organizationId, bool? isActive, int page, int pageSize);

        /// <summary>Warehouses whose name CONTAINS the search value (existing name-matching convention).</summary>
        Task<IEnumerable<WarehouseDto>> GetByNameAsync(Guid organizationId, string name);

        /// <summary>Exact-name match, used to prevent duplicate warehouse names within an organization.</summary>
        Task<WarehouseDto?> GetByExactNameAsync(Guid organizationId, string name);
    }
}
