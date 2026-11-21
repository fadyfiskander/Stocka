using Inventory.Contracts.DTOs.WarehouseBatch;
using Inventory.Domain.Batches;
using Inventory.Domain.Warehouses;

namespace Inventory.Application.Repositories.Queries
{
    public interface IWarehouseBatchQueryRepository
    {
        Task<IEnumerable<WarehouseBatchDto>> GetAllAsync(
            WarehouseId warehouseId,
            CancellationToken cancellationToken = default);

        /// <summary>Current quantity of a batch in one warehouse (0 when not allocated).</summary>
        Task<int> GetQuantityInWarehouseAsync(
            WarehouseId warehouseId,
            BatchId batchId,
            CancellationToken cancellationToken = default);

        /// <summary>Total current units of a batch across all warehouses of the Organization.</summary>
        Task<int> GetTotalQuantityInWarehousesAsync(
            BatchId batchId,
            CancellationToken cancellationToken = default);
    }
}