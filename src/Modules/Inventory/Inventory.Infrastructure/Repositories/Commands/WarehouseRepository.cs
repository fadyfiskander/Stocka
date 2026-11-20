using Inventory.Application.Repositories.Commands;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Repositories.Commands.Bases;
using Inventory.Primitives.Identifiers;

namespace Inventory.Infrastructure.Repositories.Commands
{
    internal class WarehouseRepository : Repository<Warehouse, WarehouseId>, IWarehouseRepository
    {
        public WarehouseRepository(InventoryDbContext context) : base(context)
        {
        }
    }
}
