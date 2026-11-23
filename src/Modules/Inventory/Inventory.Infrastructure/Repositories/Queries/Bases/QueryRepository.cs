using Inventory.Application.Common.Interfaces;
using System.Data;

namespace Inventory.Infrastructure.Repositories.Queries.Bases
{
    public abstract class QueryRepository
    {
        protected readonly IDbConnection _connection;

        protected const string TableWarehouses = "Inventory.Warehouses";
        protected const string TableBatches = "Inventory.Batches";
        protected const string TableWarehouseBatches = "Inventory.WarehouseBatches";
        protected const string TableStockMovements = "Inventory.StockMovements";

        protected QueryRepository(IInventoryDbConnection connection)
        {
            _connection = connection.Connection;
        }

        protected IDbConnection GetConnection() => _connection;
    }
}
