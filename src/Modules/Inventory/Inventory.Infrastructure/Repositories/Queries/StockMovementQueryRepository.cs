using Dapper;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.StockMovement;
using Inventory.Infrastructure.Repositories.Queries.Bases;
using Inventory.Domain.Batches;
using Inventory.Domain.StockMovements;
using Inventory.Domain.Warehouses;
using BuildingBlocks.Application.Common.Models;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inventory.Infrastructure.Repositories.Queries
{
    internal class StockMovementQueryRepository : QueryRepository, IStockMovementQueryRepository
    {
        private const string BaseSelect =
            "SELECT Id, ProductId, BatchId, WarehouseId, OrganizationId, Quantity, MovementType, ReferenceType, ReferenceId, CreatedAt, Reason FROM " + TableStockMovements;

        public StockMovementQueryRepository(IInventoryDbConnection connection) : base(connection)
        {
        }

        public async Task<IEnumerable<StockMovementDto>> GetAllAsync(Guid organizationId, WarehouseId warehouseId)
        {
            var sql = $"{BaseSelect} WHERE WarehouseId = @WarehouseId AND OrganizationId = @OrganizationId ORDER BY CreatedAt DESC";
            return await _connection.QueryAsync<StockMovementDto>(sql, new { WarehouseId = warehouseId.Value, OrganizationId = organizationId });
        }

        public async Task<IEnumerable<StockMovementDto>> GetByBatchAsync(Guid organizationId, BatchId batchId)
        {
            var sql = $"{BaseSelect} WHERE BatchId = @BatchId AND OrganizationId = @OrganizationId ORDER BY CreatedAt ASC";
            return await _connection.QueryAsync<StockMovementDto>(sql, new { BatchId = batchId.Value, OrganizationId = organizationId });
        }

        public async Task<PaginatedResult<StockMovementDto>> GetPagedAsync(Guid organizationId, MovementQueryCriteria criteria)
        {
            var page = Math.Max(1, criteria.Page);
            var pageSize = Math.Clamp(criteria.PageSize, 1, 100);

            var where = new StringBuilder(" WHERE OrganizationId = @OrganizationId");
            var parameters = new Dictionary<string, object?> { ["OrganizationId"] = organizationId };

            AppendFilter(where, parameters, "WarehouseId", criteria.WarehouseId);
            AppendFilter(where, parameters, "BatchId", criteria.BatchId);
            AppendFilter(where, parameters, "ProductId", criteria.ProductId);

            if (criteria.MovementTypes is { Count: > 0 })
            {
                where.Append(" AND MovementType IN @MovementTypes");
                parameters["MovementTypes"] = criteria.MovementTypes;
            }

            if (criteria.DateFromUtc.HasValue)
            {
                where.Append(" AND CreatedAt >= @DateFromUtc");
                parameters["DateFromUtc"] = criteria.DateFromUtc.Value;
            }

            if (criteria.DateToUtc.HasValue)
            {
                where.Append(" AND CreatedAt <= @DateToUtc");
                parameters["DateToUtc"] = criteria.DateToUtc.Value;
            }

            var countSql = $"SELECT COUNT(*) FROM {TableStockMovements}{where}";
            var total = await _connection.ExecuteScalarAsync<int>(countSql, parameters);

            var offset = (page - 1) * pageSize;
            parameters["Offset"] = offset;
            parameters["PageSize"] = pageSize;
            var sql = $"{BaseSelect}{where} ORDER BY CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var items = await _connection.QueryAsync<StockMovementDto>(sql, parameters);

            return new PaginatedResult<StockMovementDto>(items.ToList(), total, page, pageSize);
        }

        private static void AppendFilter(StringBuilder where, Dictionary<string, object?> parameters, string columnName, object? value)
        {
            if (value is null) return;
            var guidValue = value switch
            {
                WarehouseId wid => wid.Value,
                BatchId bid => bid.Value,
                Guid g => g,
                _ => throw new NotSupportedException($"Unsupported filter type: {value.GetType().Name}")
            };
            if (guidValue == Guid.Empty) return;

            where.Append($" AND {columnName} = @{columnName}");
            parameters[columnName] = guidValue;
        }

        public async Task<StockMovementDto?> GetByIdAsync(Guid organizationId, StockMovementId id)
        {
            var sql = $"{BaseSelect} WHERE Id = @Id AND OrganizationId = @OrganizationId";
            return await _connection.QueryFirstOrDefaultAsync<StockMovementDto>(sql, new { Id = id.Value, OrganizationId = organizationId });
        }
    }
}
