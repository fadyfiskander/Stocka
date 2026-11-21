using BuildingBlocks.Application.Common.Interfaces;
using Inventory.Domain.Batches;
using Inventory.Domain.Entities;
using Inventory.Primitives.Identifiers;

namespace Inventory.Application.Repositories.Commands
{
    public interface IWarehouseBatchRepository : IRepository<WarehouseBatch, WarehouseBatchId>
    {
        /// <summary>
        /// Finds the unique warehouse/batch row (or null when this batch is not yet held
        /// by this warehouse). EF global tenant filters apply, as everywhere else.
        /// </summary>
        Task<WarehouseBatch?> FindByWarehouseAndBatchAsync(WarehouseId warehouseId, BatchId batchId);
    }
}
