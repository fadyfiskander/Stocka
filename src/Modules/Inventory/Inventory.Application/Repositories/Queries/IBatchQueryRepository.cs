using BuildingBlocks.Application.Common.Models;
using Inventory.Contracts.DTOs.Batch;
using Inventory.Domain.Batches;
using Inventory.Domain.Warehouses;

namespace Inventory.Application.Repositories.Queries
{
    public interface IBatchQueryRepository
    {
        Task<BatchDto?> GetByIdAsync(BatchId id, CancellationToken cancellationToken);

        Task<PaginatedResult<BatchDto>> GetAllPagedAsync(int page, int pageSize, CancellationToken cancellationToken);

        /// <summary>Batches of one product (by the batch's soft Catalog product reference).</summary>
        Task<PaginatedResult<BatchDto>> GetByProductAsync(Guid productId, int page, int pageSize, CancellationToken cancellationToken);

        /// <summary>Batches originating from one purchase item.</summary>
        Task<PaginatedResult<BatchDto>> GetByPurchaseItemAsync(Guid purchaseItemId, int page, int pageSize, CancellationToken cancellationToken);

        /// <summary>Batches currently holding stock (Quantity &gt; 0) in the given warehouse.</summary>
        Task<IEnumerable<BatchDto>> GetByWarehouseAsync(WarehouseId warehouseId, CancellationToken cancellationToken);

        /// <summary>Batches with units not yet allocated to any warehouse (RemainingQuantity &gt; 0).</summary>
        Task<IEnumerable<BatchDto>> GetUnallocatedAsync(CancellationToken cancellationToken);
    }
}
