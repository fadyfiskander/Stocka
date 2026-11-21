using BuildingBlocks.Application.Common.Interfaces;
using Inventory.Domain.Warehouses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Repositories.Commands
{
    public interface IWarehouseRepository : IRepository<Warehouse, WarehouseId>
    {
    }
}
