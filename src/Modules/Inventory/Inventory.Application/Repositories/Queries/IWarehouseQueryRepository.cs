using BuildingBlocks.Application.Common.Models;
using Inventory.Contracts.DTOs.Warehouse;
using Inventory.Domain.Warehouses;

namespace Inventory.Application.Repositories.Queries
{
    public interface IWarehouseQueryRepository
    {
        Task<WarehouseDto?> GetByIdAsync(
            WarehouseId id,
            CancellationToken cancellationToken = default);

        Task<PaginatedResult<WarehouseDto>> GetPagedAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>Paged list, optionally filtered to a single activation state.</summary>
        Task<PaginatedResult<WarehouseDto>> GetPagedAsync(
            bool? isActive,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>Warehouses whose name CONTAINS the search value (existing name-matching convention).</summary>
        Task<IEnumerable<WarehouseDto>> GetByNameAsync(
            string name,
            CancellationToken cancellationToken = default);

        /// <summary>Exact-name match, used to prevent duplicate warehouse names within an organization.</summary>
        Task<WarehouseDto?> GetByExactNameAsync(
            string name,
            CancellationToken cancellationToken = default);
    }
}