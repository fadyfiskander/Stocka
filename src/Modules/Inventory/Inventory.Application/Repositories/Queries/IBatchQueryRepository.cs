using BuildingBlocks.Application.Common.Models;
using Inventory.Contracts.DTOs.Batch;
using Inventory.Domain.Batches;
using Inventory.Domain.Warehouses;

namespace Inventory.Application.Repositories.Queries
{
    public interface IBatchQueryRepository
    {
        Task<BatchDto?> GetByIdAsync(Guid organizationId, BatchId id);
        Task<IEnumerable<BatchDto>> GetAllAsync(Guid organizationId);

        Task<PaginatedResult<BatchDto>> GetAllPagedAsync(Guid organizationId, int page, int pageSize);

        /// <summary>Batches of one product (by the batch's soft Catalog product reference).</summary>
        Task<PaginatedResult<BatchDto>> GetByProductAsync(Guid organizationId, Guid productId, int page, int pageSize);

        /// <summary>Batches originating from one purchase item.</summary>
        Task<PaginatedResult<BatchDto>> GetByPurchaseItemAsync(Guid organizationId, Guid purchaseItemId, int page, int pageSize);

        /// <summary>Batches currently holding stock (Quantity &gt; 0) in the given warehouse.</summary>
        Task<IEnumerable<BatchDto>> GetByWarehouseAsync(Guid organizationId, WarehouseId warehouseId);

        /// <summary>Batches with units not yet allocated to any warehouse (RemainingQuantity &gt; 0).</summary>
        Task<IEnumerable<BatchDto>> GetUnallocatedAsync(Guid organizationId);

        /// <summary>Batches with no allocatable units left (RemainingQuantity = 0).</summary>
        Task<IEnumerable<BatchDto>> GetExhaustedAsync(Guid organizationId);
    }
}
