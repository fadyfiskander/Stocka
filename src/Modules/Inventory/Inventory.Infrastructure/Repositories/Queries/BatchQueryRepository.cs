using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Models;
using Dapper;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.Batch;
using Inventory.Domain.Batches;
using Inventory.Domain.Warehouses;
using Inventory.Infrastructure.Repositories.Queries.Bases;

namespace Inventory.Infrastructure.Repositories.Queries
{
    internal class BatchQueryRepository : QueryRepository, IBatchQueryRepository
    {
        private const string BaseSelect =
            "SELECT Id, ProductId, PurchaseItemId, OrganizationId, InitialQuantity, RemainingQuantity, UnitCost, CreatedAt FROM " + TableBatches;

        public BatchQueryRepository(IInventoryDbConnection connection, ICurrentUserService currentUser)
            : base(connection, currentUser)
        {
        }

        public async Task<PaginatedResult<BatchProductGuidDto>> GetAllPagedAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return await GetPagedCoreAsync(
                " WHERE OrganizationId = @OrganizationId",
                new { OrganizationId },
                page,
                pageSize,
                cancellationToken);
        }

        public async Task<PaginatedResult<BatchProductGuidDto>> GetByProductAsync(
            Guid productId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            if (productId == Guid.Empty)
                throw new ArgumentException("Product ID cannot be empty.", nameof(productId));

            return await GetPagedCoreAsync(
                " WHERE OrganizationId = @OrganizationId AND ProductId = @ProductId",
                new { OrganizationId, ProductId = productId },
                page,
                pageSize,
                cancellationToken);
        }

        public async Task<PaginatedResult<BatchProductGuidDto>> GetByPurchaseItemAsync(
            Guid purchaseItemId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            if (purchaseItemId == Guid.Empty)
                throw new ArgumentException("Purchase item ID cannot be empty.", nameof(purchaseItemId));

            return await GetPagedCoreAsync(
                " WHERE OrganizationId = @OrganizationId AND PurchaseItemId = @PurchaseItemId",
                new { OrganizationId, PurchaseItemId = purchaseItemId },
                page,
                pageSize,
                cancellationToken);
        }

        private async Task<PaginatedResult<BatchProductGuidDto>> GetPagedCoreAsync(
            string where,
            object parameters,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var countSql = $"SELECT COUNT(*) FROM {TableBatches}{where}";
            var countCommand = new CommandDefinition(
                countSql,
                parameters,
                cancellationToken: cancellationToken);

            var total = await _connection.ExecuteScalarAsync<int>(countCommand);

            var offset = (page - 1) * pageSize;
            var sql = $"{BaseSelect}{where} ORDER BY CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var dp = new DynamicParameters(parameters);
            dp.Add("Offset", offset);
            dp.Add("PageSize", pageSize);

            var itemsCommand = new CommandDefinition(
                sql,
                dp,
                cancellationToken: cancellationToken);

            var items = await _connection.QueryAsync<BatchProductGuidDto>(itemsCommand);
            return new PaginatedResult<BatchProductGuidDto>(items.ToList(), total, page, pageSize);
        }

        public async Task<IEnumerable<BatchProductGuidDto>> GetByWarehouseAsync(
            WarehouseId warehouseId,
            CancellationToken cancellationToken = default)
        {
            // Batches currently holding (positive) stock in that warehouse.
            var sql = $"""
                SELECT b.Id, b.ProductId, b.PurchaseItemId, b.OrganizationId, b.InitialQuantity, b.RemainingQuantity, b.UnitCost, b.CreatedAt
                FROM {TableBatches} b
                JOIN {TableWarehouseBatches} wb ON wb.BatchId = b.Id
                WHERE wb.WarehouseId = @WarehouseId
                  AND wb.Quantity > 0
                  AND wb.OrganizationId = @OrganizationId
                  AND b.OrganizationId = @OrganizationId
                ORDER BY b.CreatedAt DESC
                """;

            var command = new CommandDefinition(
                sql,
                new { WarehouseId = warehouseId.Value, OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.QueryAsync<BatchProductGuidDto>(command);
        }

        public async Task<IEnumerable<BatchProductGuidDto>> GetUnallocatedAsync(CancellationToken cancellationToken = default)
        {
            var sql = $"{BaseSelect} WHERE OrganizationId = @OrganizationId AND RemainingQuantity > 0 ORDER BY CreatedAt DESC";
            var command = new CommandDefinition(
                sql,
                new { OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.QueryAsync<BatchProductGuidDto>(command);
        }

        public async Task<BatchProductGuidDto?> GetByIdAsync(BatchId id, CancellationToken cancellationToken = default)
        {
            var sql = $"{BaseSelect} WHERE Id = @Id AND OrganizationId = @OrganizationId";
            var command = new CommandDefinition(
                sql,
                new { Id = id.Value, OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.QueryFirstOrDefaultAsync<BatchProductGuidDto>(command);
        }
    }
}