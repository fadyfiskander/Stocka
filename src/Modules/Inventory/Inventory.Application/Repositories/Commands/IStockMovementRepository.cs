using BuildingBlocks.Application.Common.Interfaces;
using Inventory.Domain.StockMovements;

namespace Inventory.Application.Repositories.Commands
{
    public interface IStockMovementRepository : IRepository<StockMovement, StockMovementId>
    {
    }
}
