using Inventory.Application.Repositories.Commands;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Repositories.Commands.Bases;
using Inventory.Primitives.Identifiers;

namespace Inventory.Infrastructure.Repositories.Commands
{
    internal class BatchRepository : Repository<Batch, BatchId>, IBatchRepository
    {
        public BatchRepository(InventoryDbContext context) : base(context)
        {
        }
    }
}
