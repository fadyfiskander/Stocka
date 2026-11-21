using BuildingBlocks.Application.Abstractions;
using Inventory.Application.Common.Interfaces;
using System.Data;

namespace Inventory.Infrastructure.Repositories.Queries.Bases
{
    public abstract class QueryRepository
    {
        protected readonly IDbConnection _connection;
        protected readonly ICurrentUserService _currentUser;

        protected Guid OrganizationId
        {
            get 
            {
                var organizationId = _currentUser.OrganizationId
                    ?? throw new InvalidOperationException(
                        $"Cannot resolve organization context from current user '{_currentUser.UserId}': {nameof(_currentUser.OrganizationId)} is null.");

                return organizationId;
            } 
        }

        protected const string TableWarehouses = "Inventory.Warehouses";
        protected const string TableBatches = "Inventory.Batches";
        protected const string TableWarehouseBatches = "Inventory.WarehouseBatches";
        protected const string TableStockMovements = "Inventory.StockMovements";

        protected QueryRepository(IInventoryDbConnection connection, ICurrentUserService currentUser)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(currentUser);

            _connection = connection.Connection;
            _currentUser = currentUser;
        }

        protected IDbConnection GetConnection() => _connection;
    }
}
