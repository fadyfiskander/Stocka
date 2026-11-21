using Inventory.Application.Repositories.Commands;
using Inventory.Domain.StockMovements;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Repositories.Commands.Bases;

namespace Inventory.Infrastructure.Repositories.Commands
{
    internal class StockMovementRepository : Repository<StockMovement, StockMovementId>, IStockMovementRepository
    {
        public StockMovementRepository(InventoryDbContext context) : base(context)
        {
        }
    }
}
