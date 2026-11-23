using BuildingBlocks.Application.Common.Models;
using Inventory.Contracts.DTOs.Batch;
using Inventory.Domain.Batches;
using Inventory.Domain.Warehouses;

namespace Inventory.Application.Repositories.Queries
{
    public interface IBatchQueryRepository
    {
        Task<BatchProductGuidDto?> GetByIdAsync(BatchId id, CancellationToken cancellationToken);

        Task<PaginatedResult<BatchProductGuidDto>> GetAllPagedAsync(int page, int pageSize, CancellationToken cancellationToken);

        /// <summary>Batches of one product (by the batch's soft Catalog product reference).</summary>
        Task<PaginatedResult<BatchProductGuidDto>> GetByProductAsync(Guid productId, int page, int pageSize, CancellationToken cancellationToken);

        /// <summary>Batches originating from one purchase item.</summary>
        Task<PaginatedResult<BatchProductGuidDto>> GetByPurchaseItemAsync(Guid purchaseItemId, int page, int pageSize, CancellationToken cancellationToken);

        /// <summary>Batches currently holding stock (Quantity &gt; 0) in the given warehouse.</summary>
        Task<IEnumerable<BatchProductGuidDto>> GetByWarehouseAsync(WarehouseId warehouseId, CancellationToken cancellationToken);

        /// <summary>Batches with units not yet allocated to any warehouse (RemainingQuantity &gt; 0).</summary>
        Task<IEnumerable<BatchProductGuidDto>> GetUnallocatedAsync(CancellationToken cancellationToken);
    }
}
