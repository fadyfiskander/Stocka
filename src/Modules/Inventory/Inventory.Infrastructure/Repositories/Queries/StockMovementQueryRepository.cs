using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Models;
using Dapper;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.StockMovement;
using Inventory.Domain.Batches;
using Inventory.Domain.Warehouses;
using Inventory.Infrastructure.Repositories.Queries.Bases;
using System.Text;

namespace Inventory.Infrastructure.Repositories.Queries
{
    internal class StockMovementQueryRepository : QueryRepository, IStockMovementQueryRepository
    {
        private const string BaseSelect =
            "SELECT Id, ProductId, BatchId, WarehouseId, OrganizationId, Quantity, MovementType, ReferenceType, ReferenceId, CreatedAt, Reason FROM " + TableStockMovements;

        public StockMovementQueryRepository(IInventoryDbConnection connection, ICurrentUserService currentUser)
            : base(connection, currentUser)
        {
        }

        public async Task<IEnumerable<StockMovementDto>> GetByBatchAsync(
            BatchId batchId,
            CancellationToken cancellationToken = default)
        {
            var sql = $"{BaseSelect} WHERE BatchId = @BatchId AND OrganizationId = @OrganizationId ORDER BY CreatedAt ASC";
            var command = new CommandDefinition(
                sql,
                new { BatchId = batchId.Value, OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.QueryAsync<StockMovementDto>(command);
        }

        public async Task<PaginatedResult<StockMovementDto>> GetPagedAsync(
            MovementQueryCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = Math.Max(1, criteria.Page);
            var pageSize = Math.Clamp(criteria.PageSize, 1, 100);

            var where = new StringBuilder(" WHERE OrganizationId = @OrganizationId");
            var dp = new DynamicParameters();
            dp.Add("OrganizationId", OrganizationId);

            AppendFilter(where, dp, "WarehouseId", criteria.WarehouseId);
            AppendFilter(where, dp, "BatchId", criteria.BatchId);
            AppendFilter(where, dp, "ProductId", criteria.ProductId);

            if (criteria.MovementTypes is { Count: > 0 })
            {
                where.Append(" AND MovementType IN @MovementTypes");
                dp.Add("MovementTypes", criteria.MovementTypes);
            }

            if (criteria.DateFromUtc.HasValue)
            {
                where.Append(" AND CreatedAt >= @DateFromUtc");
                dp.Add("DateFromUtc", criteria.DateFromUtc.Value);
            }

            if (criteria.DateToUtc.HasValue)
            {
                where.Append(" AND CreatedAt <= @DateToUtc");
                dp.Add("DateToUtc", criteria.DateToUtc.Value);
            }

            var countSql = $"SELECT COUNT(*) FROM {TableStockMovements}{where}";
            var countCommand = new CommandDefinition(
                countSql,
                dp,
                cancellationToken: cancellationToken);

            var total = await _connection.ExecuteScalarAsync<int>(countCommand);

            var offset = (page - 1) * pageSize;
            dp.Add("Offset", offset);
            dp.Add("PageSize", pageSize);

            var sql = $"{BaseSelect}{where} ORDER BY CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var itemsCommand = new CommandDefinition(
                sql,
                dp,
                cancellationToken: cancellationToken);

            var items = await _connection.QueryAsync<StockMovementDto>(itemsCommand);
            return new PaginatedResult<StockMovementDto>(items.ToList(), total, page, pageSize);
        }

        private static void AppendFilter(StringBuilder where, DynamicParameters parameters, string columnName, object? value)
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
            parameters.Add(columnName, guidValue);
        }
    }
}