using Inventory.Application.Repositories.Commands;
using Inventory.Domain.Batches;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Repositories.Commands.Bases;

namespace Inventory.Infrastructure.Repositories.Commands
{
    internal class BatchRepository : Repository<Batch, BatchId>, IBatchRepository
    {
        public BatchRepository(InventoryDbContext context) : base(context)
        {
        }
    }
}
