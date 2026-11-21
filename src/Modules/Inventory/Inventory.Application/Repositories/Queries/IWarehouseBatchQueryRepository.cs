using Inventory.Contracts.DTOs.WarehouseBatch;
using Inventory.Domain.Batches;
using Inventory.Primitives.Identifiers;

namespace Inventory.Application.Repositories.Queries
{
    public interface IWarehouseBatchQueryRepository
    {
        Task<WarehouseBatchDto?> GetByIdAsync(Guid organizationId, WarehouseBatchId id);
        Task<IEnumerable<WarehouseBatchDto>> GetAllAsync(Guid organizationId, WarehouseId warehouseId);

        /// <summary>The single warehouse/batch row (unique pair), or null if not yet allocated.</summary>
        Task<WarehouseBatchDto?> GetByWarehouseAndBatchAsync(Guid organizationId, WarehouseId warehouseId, BatchId batchId);

        /// <summary>All warehouse placements of a batch across warehouses.</summary>
        Task<IEnumerable<WarehouseBatchDto>> GetByBatchAsync(Guid organizationId, BatchId batchId);

        /// <summary>Current quantity of a batch in one warehouse (0 when not allocated).</summary>
        Task<int> GetQuantityInWarehouseAsync(Guid organizationId, WarehouseId warehouseId, BatchId batchId);

        /// <summary>Total current units of a batch across all warehouses of the Organization.</summary>
        Task<int> GetTotalQuantityInWarehousesAsync(Guid organizationId, BatchId batchId);
    }
}
