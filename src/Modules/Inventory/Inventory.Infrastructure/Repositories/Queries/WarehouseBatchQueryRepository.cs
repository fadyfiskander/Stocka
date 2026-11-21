using Dapper;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.WarehouseBatch;
using Inventory.Infrastructure.Repositories.Queries.Bases;
using Inventory.Domain.Batches;
using Inventory.Domain.Warehouses;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Repositories.Queries
{
    internal class WarehouseBatchQueryRepository : QueryRepository, IWarehouseBatchQueryRepository
    {
        private const string BaseSelect =
            "SELECT Id, WarehouseId, BatchId, Quantity, OrganizationId FROM " + TableWarehouseBatches;

        public WarehouseBatchQueryRepository(IInventoryDbConnection connection) : base(connection)
        {
        }

        public async Task<IEnumerable<WarehouseBatchDto>> GetAllAsync(Guid organizationId, WarehouseId warehouseId)
        {
            var sql = $"{BaseSelect} WHERE WarehouseId = @WarehouseId AND OrganizationId = @OrganizationId ORDER BY BatchId";
            return await _connection.QueryAsync<WarehouseBatchDto>(sql, new { WarehouseId = warehouseId.Value, OrganizationId = organizationId });
        }

        public async Task<WarehouseBatchDto?> GetByWarehouseAndBatchAsync(Guid organizationId, WarehouseId warehouseId, BatchId batchId)
        {
            var sql = $"{BaseSelect} WHERE WarehouseId = @WarehouseId AND BatchId = @BatchId AND OrganizationId = @OrganizationId";
            return await _connection.QueryFirstOrDefaultAsync<WarehouseBatchDto>(
                sql, new { WarehouseId = warehouseId.Value, BatchId = batchId.Value, OrganizationId = organizationId });
        }

        public async Task<IEnumerable<WarehouseBatchDto>> GetByBatchAsync(Guid organizationId, BatchId batchId)
        {
            var sql = $"{BaseSelect} WHERE BatchId = @BatchId AND OrganizationId = @OrganizationId ORDER BY WarehouseId";
            return await _connection.QueryAsync<WarehouseBatchDto>(sql, new { BatchId = batchId.Value, OrganizationId = organizationId });
        }

        public async Task<int> GetQuantityInWarehouseAsync(Guid organizationId, WarehouseId warehouseId, BatchId batchId)
        {
            var sql = $"SELECT COALESCE(SUM(Quantity), 0) FROM {TableWarehouseBatches} WHERE WarehouseId = @WarehouseId AND BatchId = @BatchId AND OrganizationId = @OrganizationId";
            return await _connection.ExecuteScalarAsync<int>(
                sql, new { WarehouseId = warehouseId.Value, BatchId = batchId.Value, OrganizationId = organizationId });
        }

        public async Task<int> GetTotalQuantityInWarehousesAsync(Guid organizationId, BatchId batchId)
        {
            var sql = $"SELECT COALESCE(SUM(Quantity), 0) FROM {TableWarehouseBatches} WHERE BatchId = @BatchId AND OrganizationId = @OrganizationId";
            return await _connection.ExecuteScalarAsync<int>(sql, new { BatchId = batchId.Value, OrganizationId = organizationId });
        }

        public async Task<WarehouseBatchDto?> GetByIdAsync(Guid organizationId, WarehouseBatchId id)
        {
            var sql = $"{BaseSelect} WHERE Id = @Id AND OrganizationId = @OrganizationId";
            return await _connection.QueryFirstOrDefaultAsync<WarehouseBatchDto>(sql, new { Id = id.Value, OrganizationId = organizationId });
        }
    }
}
