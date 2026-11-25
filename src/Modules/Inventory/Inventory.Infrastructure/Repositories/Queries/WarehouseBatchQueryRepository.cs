using BuildingBlocks.Application.Abstractions;
using Dapper;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.WarehouseBatch;
using Inventory.Domain.Batches;
using Inventory.Domain.Warehouses;
using Inventory.Infrastructure.Repositories.Queries.Bases;

namespace Inventory.Infrastructure.Repositories.Queries
{
    internal class WarehouseBatchQueryRepository : QueryRepository, IWarehouseBatchQueryRepository
    {
        private const string BaseSelect =
            "SELECT Id, WarehouseId, BatchId, Quantity, OrganizationId FROM " + TableWarehouseBatches;

        public WarehouseBatchQueryRepository(IInventoryDbConnection connection, ICurrentUserService currentUser)
            : base(connection, currentUser)
        {
        }

        public async Task<IEnumerable<WarehouseBatchDto>> GetAllAsync(
            WarehouseId warehouseId,
            CancellationToken cancellationToken = default)
        {
            var sql = $"{BaseSelect} WHERE WarehouseId = @WarehouseId AND OrganizationId = @OrganizationId ORDER BY BatchId";
            var command = new CommandDefinition(
                sql,
                new { WarehouseId = warehouseId.Value, OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.QueryAsync<WarehouseBatchDto>(command);
        }

        public async Task<int> GetQuantityInWarehouseAsync(
            WarehouseId warehouseId,
            BatchId batchId,
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT COALESCE(SUM(Quantity), 0) FROM {TableWarehouseBatches} WHERE WarehouseId = @WarehouseId AND BatchId = @BatchId AND OrganizationId = @OrganizationId";
            var command = new CommandDefinition(
                sql,
                new { WarehouseId = warehouseId.Value, BatchId = batchId.Value, OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.ExecuteScalarAsync<int>(command);
        }

        public async Task<int> GetTotalQuantityInWarehousesAsync(
            BatchId batchId,
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT COALESCE(SUM(Quantity), 0) FROM {TableWarehouseBatches} WHERE BatchId = @BatchId AND OrganizationId = @OrganizationId";
            var command = new CommandDefinition(
                sql,
                new { BatchId = batchId.Value, OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.ExecuteScalarAsync<int>(command);
        }
    }
}