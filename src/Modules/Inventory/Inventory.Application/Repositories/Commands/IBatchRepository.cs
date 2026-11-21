using BuildingBlocks.Application.Common.Interfaces;
using Inventory.Domain.Batches;

namespace Inventory.Application.Repositories.Commands
{
    public interface IBatchRepository : IRepository<Batch, BatchId>
    {
    }
}
