using Inventory.Application.Repositories.Commands;
using Inventory.Domain.Entities;
using Inventory.Primitives.Identifiers;
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
