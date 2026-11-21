using Inventory.Application.Repositories.Commands;
using Inventory.Domain.Batches;
using Inventory.Domain.Warehouses;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Repositories.Commands.Bases;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories.Commands
{
    internal class WarehouseBatchRepository : Repository<WarehouseBatch, WarehouseBatchId>, IWarehouseBatchRepository
    {
        public WarehouseBatchRepository(InventoryDbContext context) : base(context)
        {
        }

        public async Task<WarehouseBatch?> FindByWarehouseAndBatchAsync(WarehouseId warehouseId, BatchId batchId)
        {
            return await _context
                .Where(wb => wb.WarehouseId == warehouseId && wb.BatchId == batchId)
                .FirstOrDefaultAsync();
        }
    }
}
