using Dapper;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.Batch;
using Inventory.Infrastructure.Repositories.Queries.Bases;
using Inventory.Primitives.Identifiers;
using BuildingBlocks.Application.Common.Models;
using System.Data;

namespace Inventory.Infrastructure.Repositories.Queries
{
    internal class BatchQueryRepository : QueryRepository, IBatchQueryRepository
    {
        private const string BaseSelect =
            "SELECT Id, ProductId, PurchaseItemId, OrganizationId, InitialQuantity, RemainingQuantity, UnitCost, CreatedAt FROM " + TableBatches;

        public BatchQueryRepository(IInventoryDbConnection connection) : base(connection)
        {
        }

        public async Task<IEnumerable<BatchDto>> GetAllAsync(Guid organizationId)
        {
            var sql = $"{BaseSelect} WHERE OrganizationId = @OrganizationId ORDER BY CreatedAt DESC";
            return await _connection.QueryAsync<BatchDto>(sql, new { OrganizationId = organizationId });
        }

        public async Task<PaginatedResult<BatchDto>> GetAllPagedAsync(Guid organizationId, int page, int pageSize)
        {
            return await GetPagedCoreAsync(
                organizationId,
                " WHERE OrganizationId = @OrganizationId",
                new { OrganizationId = organizationId },
                page, pageSize);
        }

        public async Task<PaginatedResult<BatchDto>> GetByProductAsync(Guid organizationId, Guid productId, int page, int pageSize)
        {
            if (productId == Guid.Empty) throw new ArgumentException("Product ID cannot be empty.", nameof(productId));
            return await GetPagedCoreAsync(
                organizationId,
                " WHERE OrganizationId = @OrganizationId AND ProductId = @ProductId",
                new { OrganizationId = organizationId, ProductId = productId },
                page, pageSize);
        }

        public async Task<PaginatedResult<BatchDto>> GetByPurchaseItemAsync(Guid organizationId, Guid purchaseItemId, int page, int pageSize)
        {
            if (purchaseItemId == Guid.Empty) throw new ArgumentException("Purchase item ID cannot be empty.", nameof(purchaseItemId));
            return await GetPagedCoreAsync(
                organizationId,
                " WHERE OrganizationId = @OrganizationId AND PurchaseItemId = @PurchaseItemId",
                new { OrganizationId = organizationId, PurchaseItemId = purchaseItemId },
                page, pageSize);
        }

        private async Task<PaginatedResult<BatchDto>> GetPagedCoreAsync(Guid organizationId, string where, object parameters, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var countSql = "SELECT COUNT(*) FROM " + TableBatches + where;
            var total = await _connection.ExecuteScalarAsync<int>(countSql, parameters);

            var offset = (page - 1) * pageSize;
            var sql = $"{BaseSelect}{where} ORDER BY CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var merged = new System.Collections.Generic.Dictionary<string, object?>();
            foreach (var prop in parameters.GetType().GetProperties())
                merged[prop.Name] = prop.GetValue(parameters);
            merged["Offset"] = offset;
            merged["PageSize"] = pageSize;

            var items = await _connection.QueryAsync<BatchDto>(sql, merged);
            return new PaginatedResult<BatchDto>(items.ToList(), total, page, pageSize);
        }

        public async Task<IEnumerable<BatchDto>> GetByWarehouseAsync(Guid organizationId, WarehouseId warehouseId)
        {
            // Batches currently holding (positive) stock in that warehouse.
            var sql = $"""
                SELECT b.Id, b.ProductId, b.PurchaseItemId, b.OrganizationId, b.InitialQuantity, b.RemainingQuantity, b.UnitCost, b.CreatedAt
                FROM {TableBatches} b
                JOIN {TableWarehouseBatches} wb ON wb.BatchId = b.Id
                WHERE wb.WarehouseId = @WarehouseId AND wb.Quantity > 0 AND wb.OrganizationId = @OrganizationId
                ORDER BY b.CreatedAt DESC
                """;
            return await _connection.QueryAsync<BatchDto>(sql, new { WarehouseId = warehouseId.Value, OrganizationId = organizationId });
        }

        public async Task<IEnumerable<BatchDto>> GetUnallocatedAsync(Guid organizationId)
        {
            var sql = $"{BaseSelect} WHERE OrganizationId = @OrganizationId AND RemainingQuantity > 0 ORDER BY CreatedAt DESC";
            return await _connection.QueryAsync<BatchDto>(sql, new { OrganizationId = organizationId });
        }

        public async Task<IEnumerable<BatchDto>> GetExhaustedAsync(Guid organizationId)
        {
            var sql = $"{BaseSelect} WHERE OrganizationId = @OrganizationId AND RemainingQuantity = 0 ORDER BY CreatedAt DESC";
            return await _connection.QueryAsync<BatchDto>(sql, new { OrganizationId = organizationId });
        }

        public async Task<BatchDto?> GetByIdAsync(Guid organizationId, BatchId id)
        {
            var sql = $"{BaseSelect} WHERE Id = @Id AND OrganizationId = @OrganizationId";
            return await _connection.QueryFirstOrDefaultAsync<BatchDto>(sql, new { Id = id, OrganizationId = organizationId });
        }
    }
}
