using Inventory.Application.Repositories.Commands;
using Inventory.Domain.Warehouses;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Repositories.Commands.Bases;

namespace Inventory.Infrastructure.Repositories.Commands
{
    internal class WarehouseRepository : Repository<Warehouse, WarehouseId>, IWarehouseRepository
    {
        public WarehouseRepository(InventoryDbContext context) : base(context)
        {
        }
    }
}
