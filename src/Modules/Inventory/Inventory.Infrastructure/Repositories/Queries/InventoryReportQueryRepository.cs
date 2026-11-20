using Dapper;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.Batch;
using Inventory.Contracts.DTOs.Reports;
using Inventory.Contracts.DTOs.StockMovement;
using Inventory.Infrastructure.Repositories.Queries.Bases;
using BuildingBlocks.Application.Common.Models;
using System.Collections.Generic;
using System.Data;

namespace Inventory.Infrastructure.Repositories.Queries
{
    /// <summary>
    /// Read-side reporting (Dapper, parameterized, organization-scoped, database-side aggregation).
    /// Movement direction (enum order): PurchaseIn=0, SaleOut=1, TransferIn=2, TransferOut=3,
    /// AdjustmentIn=4, AdjustmentOut=5, OrderReturn=6. Inbound set = {0,2,4,6}, outbound = {1,3,5}.
    /// Reference types: Purchase=0, Order=1, Transfer=2, Adjustment=3, Return=4, ReversalOfMovement=5.
    /// </summary>
    internal class InventoryReportQueryRepository : QueryRepository, IInventoryReportQueryRepository
    {
        public InventoryReportQueryRepository(IInventoryDbConnection connection) : base(connection)
        {
        }

        // ---- Warehouse reports ---------------------------------------------------------

        private const string WarehouseStockSummarySql = $"""
            SELECT w.Id AS WarehouseId, w.Name, w.Location, w.IsActive, w.Type,
                   COALESCE(SUM(wb.Quantity), 0) AS TotalUnits,
                   COUNT(DISTINCT b.ProductId) AS DistinctProducts,
                   COALESCE(SUM(wb.Quantity * b.UnitCost), 0) AS TotalCost
            FROM {TableWarehouses} w
            LEFT JOIN {TableWarehouseBatches} wb ON wb.WarehouseId = w.Id AND wb.OrganizationId = w.OrganizationId
            LEFT JOIN {TableBatches} b ON b.Id = wb.BatchId AND b.OrganizationId = wb.OrganizationId
            WHERE w.OrganizationId = @OrganizationId
            GROUP BY w.Id, w.Name, w.Location, w.IsActive, w.Type
            """;
        // HAVING-clause variants (zero-stock / inactive-with-stock) append to this base query.

        public async Task<IEnumerable<WarehouseStockSummaryDto>> GetWarehouseStockSummaryAsync(Guid organizationId)
        {
            var sql = WarehouseStockSummarySql + " ORDER BY w.Name";
            return await _connection.QueryAsync<WarehouseStockSummaryDto>(sql, new { OrganizationId = organizationId });
        }

        public async Task<PaginatedResult<WarehouseStockSummaryDto>> GetWarehousesRankedByStockAsync(Guid organizationId, bool descending, int page, int pageSize)
        {
            var (total, items) = await GetPagedRankedAsync(
                organizationId, " ORDER BY TotalUnits " + (descending ? "DESC" : "ASC") + ", w.Name ASC", page, pageSize);
            return new PaginatedResult<WarehouseStockSummaryDto>(items, total, Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
        }

        public async Task<PaginatedResult<WarehouseStockSummaryDto>> GetWarehousesRankedByCostAsync(Guid organizationId, bool descending, int page, int pageSize)
        {
            var (total, items) = await GetPagedRankedAsync(
                organizationId, " ORDER BY TotalCost " + (descending ? "DESC" : "ASC") + ", w.Name ASC", page, pageSize);
            return new PaginatedResult<WarehouseStockSummaryDto>(items, total, Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
        }

        private async Task<(int total, List<WarehouseStockSummaryDto> items)> GetPagedRankedAsync(
            Guid organizationId, string orderBy, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var countSql = "SELECT COUNT(*) FROM " + TableWarehouses + " WHERE OrganizationId = @OrganizationId";
            var total = await _connection.ExecuteScalarAsync<int>(countSql, new { OrganizationId = organizationId });

            var sql = WarehouseStockSummarySql + orderBy + " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var items = (await _connection.QueryAsync<WarehouseStockSummaryDto>(
                sql, new { OrganizationId = organizationId, Offset = (page - 1) * pageSize, PageSize = pageSize })).ToList();

            return (total, items);
        }

        public async Task<PaginatedResult<WarehouseInventoryItemDto>> GetWarehouseInventoryAsync(Guid organizationId, Guid warehouseId, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var whereParts = " WHERE wb.WarehouseId = @WarehouseId AND wb.OrganizationId = @OrganizationId";
            var countSql = $"""
                SELECT COUNT(*) FROM {TableWarehouseBatches} wb
                JOIN {TableBatches} b ON b.Id = wb.BatchId AND b.OrganizationId = wb.OrganizationId
                """ + whereParts;

            var total = await _connection.ExecuteScalarAsync<int>(
                countSql, new { WarehouseId = warehouseId, OrganizationId = organizationId });

            var sql = $"""
                SELECT wb.Id AS WarehouseBatchId, wb.BatchId, b.ProductId, wb.Quantity,
                       b.UnitCost AS BatchUnitCost, (wb.Quantity * b.UnitCost) AS LineCost,
                       b.CreatedAt AS BatchCreatedAt
                FROM {TableWarehouseBatches} wb
                JOIN {TableBatches} b ON b.Id = wb.BatchId AND b.OrganizationId = wb.OrganizationId
                """ + whereParts +
                " ORDER BY b.ProductId, wb.BatchId OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var items = await _connection.QueryAsync<WarehouseInventoryItemDto>(
                sql, new { WarehouseId = warehouseId, OrganizationId = organizationId, Offset = (page - 1) * pageSize, PageSize = pageSize });

            return new PaginatedResult<WarehouseInventoryItemDto>(items.ToList(), total, page, pageSize);
        }

        public async Task<IEnumerable<WarehouseStockSummaryDto>> GetZeroStockWarehousesAsync(Guid organizationId)        {
            var sql = $"{WarehouseStockSummarySql} HAVING COALESCE(SUM(wb.Quantity), 0) = 0 ORDER BY w.Name";
            return await _connection.QueryAsync<WarehouseStockSummaryDto>(sql, new { OrganizationId = organizationId });
        }

        public async Task<IEnumerable<WarehouseStockSummaryDto>> GetInactiveWarehousesWithStockAsync(Guid organizationId)
        {
            var sql = $"{WarehouseStockSummarySql} HAVING COALESCE(SUM(wb.Quantity), 0) > 0 AND w.IsActive = 0 ORDER BY w.Name";
            return await _connection.QueryAsync<WarehouseStockSummaryDto>(sql, new { OrganizationId = organizationId });
        }

        // ---- Stock quantity reports ------------------------------------------------------

        public async Task<IEnumerable<ProductStockRowDto>> GetProductStockDistributionAsync(Guid organizationId, Guid productId)
        {
            var sql = $"""
                SELECT wb.WarehouseId, COALESCE(SUM(wb.Quantity), 0) AS Quantity
                FROM {TableWarehouseBatches} wb
                JOIN {TableBatches} b ON b.Id = wb.BatchId AND b.OrganizationId = wb.OrganizationId
                WHERE b.ProductId = @ProductId AND wb.OrganizationId = @OrganizationId AND wb.Quantity > 0
                GROUP BY wb.WarehouseId
                ORDER BY Quantity DESC
                """;
            return await _connection.QueryAsync<ProductStockRowDto>(sql, new { ProductId = productId, OrganizationId = organizationId });
        }

        public async Task<int> GetProductTotalUnitsAsync(Guid organizationId, Guid productId)
        {
            var sql = $"""
                SELECT COALESCE(SUM(wb.Quantity), 0)
                FROM {TableWarehouseBatches} wb
                JOIN {TableBatches} b ON b.Id = wb.BatchId AND b.OrganizationId = wb.OrganizationId
                WHERE b.ProductId = @ProductId AND wb.OrganizationId = @OrganizationId
                """;
            return await _connection.ExecuteScalarAsync<int>(sql, new { ProductId = productId, OrganizationId = organizationId });
        }

        public async Task<int> GetProductUnallocatedUnitsAsync(Guid organizationId, Guid productId)
        {
            var sql = $"SELECT COALESCE(SUM(RemainingQuantity), 0) FROM {TableBatches} WHERE ProductId = @ProductId AND OrganizationId = @OrganizationId";
            return await _connection.ExecuteScalarAsync<int>(sql, new { ProductId = productId, OrganizationId = organizationId });
        }

        public async Task<PaginatedResult<ZeroStockProductDto>> GetZeroStockProductsAsync(Guid organizationId, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            // Products with known batches but zero/no stock left in every warehouse
            // (distinct from a product simply not present in a warehouse yet).
            const string baseQuery = $"""
                SELECT b.ProductId,
                       SUM(b.InitialQuantity) AS InitialQuantity,
                       SUM(b.RemainingQuantity) AS UnallocatedQuantity,
                       SUM(COALESCE(wt.Quantity, 0)) AS QuantityInWarehouses
                FROM {TableBatches} b
                LEFT JOIN (SELECT BatchId, SUM(Quantity) AS Quantity FROM {TableWarehouseBatches} WHERE OrganizationId = @OrganizationId GROUP BY BatchId) wt
                       ON wt.BatchId = b.Id
                WHERE b.OrganizationId = @OrganizationId
                GROUP BY b.ProductId
                HAVING SUM(COALESCE(wt.Quantity, 0)) = 0
                """;

            var total = await _connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM ({baseQuery}) z", new { OrganizationId = organizationId });

            var sql = baseQuery + " ORDER BY b.ProductId OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var items = await _connection.QueryAsync<ZeroStockProductDto>(
                sql, new { OrganizationId = organizationId, Offset = (page - 1) * pageSize, PageSize = pageSize });

            return new PaginatedResult<ZeroStockProductDto>(items.ToList(), total, page, pageSize);
        }

        public async Task<int> GetZeroStockWarehouseBatchRowsCountAsync(Guid organizationId)
        {
            var sql = $"SELECT COUNT(*) FROM {TableWarehouseBatches} WHERE OrganizationId = @OrganizationId AND Quantity = 0";
            return await _connection.ExecuteScalarAsync<int>(sql, new { OrganizationId = organizationId });
        }

        // ---- Valuation (recorded batch unit costs only) ----------------------------------

        // See GetWarehousesRankedByCostAsync above â€” reuse of summary data keeps the rules single-sourced.

        public async Task<decimal> GetTotalInventoryCostAsync(Guid organizationId)
        {
            var sql = $"""
                SELECT COALESCE(SUM(wb.Quantity * b.UnitCost), 0)
                FROM {TableWarehouseBatches} wb
                JOIN {TableBatches} b ON b.Id = wb.BatchId AND b.OrganizationId = wb.OrganizationId
                WHERE wb.OrganizationId = @OrganizationId
                """;
            return await _connection.ExecuteScalarAsync<decimal>(sql, new { OrganizationId = organizationId });
        }

        // ---- Movement reports -------------------------------------------------------------

        public async Task<IEnumerable<MovementTypeTotalsDto>> GetMovementTotalsByTypeAsync(Guid organizationId, DateTime? dateFromUtc, DateTime? dateToUtc)
        {
            var dateFilter = string.Empty;
            if (dateFromUtc.HasValue) dateFilter += " AND CreatedAt >= @DateFromUtc";
            if (dateToUtc.HasValue) dateFilter += " AND CreatedAt <= @DateToUtc";

            var sql = $"""
                SELECT MovementType,
                       SUM(Quantity) AS TotalQuantity,
                       SUM(CASE WHEN MovementType IN (0, 2, 4, 6) THEN CAST(Quantity AS decimal(19,2)) ELSE -CAST(Quantity AS decimal(19,2)) END) AS SignedQuantity,
                       COUNT(*) AS MovementCount
                FROM {TableStockMovements}
                WHERE OrganizationId = @OrganizationId{dateFilter}
                GROUP BY MovementType
                ORDER BY MovementType
                """;

            var parameters = new { OrganizationId = organizationId, dateFromUtc, dateToUtc };
            return await _connection.QueryAsync<MovementTypeTotalsDto>(sql, parameters);
        }

        public async Task<InboundOutboundSummaryDto> GetInboundOutboundTotalsAsync(Guid organizationId, DateTime dateFromUtc, DateTime dateToUtc)
        {
            // Transfer legs (2, 3) are EXCLUDED from both totals to avoid double-counting paired movements.
            var sql = $"""
                SELECT
                    COALESCE(SUM(CASE WHEN MovementType IN (0, 4, 6) THEN Quantity ELSE 0 END), 0) AS InboundUnits,
                    COALESCE(SUM(CASE WHEN MovementType IN (1, 5) THEN Quantity ELSE 0 END), 0) AS OutboundUnits
                FROM {TableStockMovements}
                WHERE OrganizationId = @OrganizationId AND CreatedAt >= @DateFromUtc AND CreatedAt <= @DateToUtc
                """;
            var row = await _connection.QuerySingleAsync<(int InboundUnits, int OutboundUnits)>(
                sql, new { OrganizationId = organizationId, DateFromUtc = dateFromUtc, DateToUtc = dateToUtc });

            return new InboundOutboundSummaryDto(dateFromUtc, dateToUtc, row.InboundUnits, row.OutboundUnits);
        }

        public async Task<PaginatedResult<TransferHistoryDto>> GetTransferHistoryAsync(Guid organizationId, int page, int pageSize, DateTime? dateFromUtc = null, DateTime? dateToUtc = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var dateFilter = string.Empty;
            if (dateFromUtc.HasValue) dateFilter += " AND s.CreatedAt >= @DateFromUtc";
            if (dateToUtc.HasValue) dateFilter += " AND s.CreatedAt <= @DateToUtc";

            // One row per transfer operation: the TransferOut/TransferIn pair sharing one
            // Transfer reference is aggregated â€” never counted twice.
            var baseQuery = $"""
                SELECT s.ReferenceId AS TransferReferenceId,
                       MIN(s.CreatedAt) AS OccurredAtUtc,
                       MIN(s.ProductId) AS ProductId,
                       MIN(s.BatchId) AS BatchId,
                       MAX(s.Quantity) AS Quantity,
                       MAX(CASE WHEN s.MovementType = 3 THEN s.WarehouseId END) AS SourceWarehouseId,
                       MAX(CASE WHEN s.MovementType = 2 THEN s.WarehouseId END) AS DestinationWarehouseId
                FROM {TableStockMovements} s
                WHERE s.OrganizationId = @OrganizationId AND s.MovementType IN (2, 3) AND s.ReferenceType = 2 AND s.ReferenceId IS NOT NULL{dateFilter}
                GROUP BY s.ReferenceId
                """;

            var transferParams = new { OrganizationId = organizationId, dateFromUtc, dateToUtc };
            var countSql = $"SELECT COUNT(*) FROM ({baseQuery}) t";
            var total = await _connection.ExecuteScalarAsync<int>(countSql, transferParams);

            var sql = baseQuery + " ORDER BY MIN(s.CreatedAt) DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var items = await _connection.QueryAsync<TransferHistoryDto>(
                sql, new { OrganizationId = organizationId, dateFromUtc, dateToUtc, Offset = (page - 1) * pageSize, PageSize = pageSize });

            return new PaginatedResult<TransferHistoryDto>(items.ToList(), total, page, pageSize);
        }

        public async Task<PaginatedResult<StockMovementDto>> GetAdjustmentHistoryAsync(Guid organizationId, int page, int pageSize, DateTime? dateFromUtc = null, DateTime? dateToUtc = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            // Adjustment legs (4, 5) cover physical-count corrections, damage write-offs
            // and reversals (ReferenceType = 5 marks reversal corrections). Reasons preserved.
            var dateFilter = string.Empty;
            if (dateFromUtc.HasValue) dateFilter += " AND s.CreatedAt >= @DateFromUtc";
            if (dateToUtc.HasValue) dateFilter += " AND s.CreatedAt <= @DateToUtc";

            var countSql = $"SELECT COUNT(*) FROM {TableStockMovements} s WHERE s.OrganizationId = @OrganizationId AND s.MovementType IN (4, 5){dateFilter}";
            var total = await _connection.ExecuteScalarAsync<int>(countSql, new { OrganizationId = organizationId, dateFromUtc, dateToUtc });

            var sql = $"SELECT Id, ProductId, BatchId, WarehouseId, OrganizationId, Quantity, MovementType, ReferenceType, ReferenceId, CreatedAt, Reason FROM {TableStockMovements} s WHERE s.OrganizationId = @OrganizationId AND s.MovementType IN (4, 5){dateFilter} ORDER BY s.CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var items = await _connection.QueryAsync<StockMovementDto>(
                sql, new { OrganizationId = organizationId, dateFromUtc, dateToUtc, Offset = (page - 1) * pageSize, PageSize = pageSize });

            return new PaginatedResult<StockMovementDto>(items.ToList(), total, page, pageSize);
        }

        // ---- Control / audit ----------------------------------------------------------------

        public async Task<PaginatedResult<HistoricalBalanceDto>> GetHistoricalBalanceAsync(Guid organizationId, DateTime asOfUtc, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            // Balance reconstruction from the audit trail. Transfer legs cancel symmetrically
            // (out leg at source, in leg at destination), so no double counting occurs.
            const string baseQuery = $"""
                SELECT s.ProductId, s.WarehouseId,
                       SUM(CASE WHEN s.MovementType IN (0, 2, 4, 6) THEN s.Quantity ELSE -s.Quantity END) AS Quantity,
                       COUNT(*) AS MovementsConsidered
                FROM {TableStockMovements} s
                WHERE s.OrganizationId = @OrganizationId AND s.CreatedAt <= @AsOfUtc
                GROUP BY s.ProductId, s.WarehouseId
                HAVING SUM(CASE WHEN s.MovementType IN (0, 2, 4, 6) THEN s.Quantity ELSE -s.Quantity END) <> 0
                """;

            var total = await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM (" + baseQuery + ") h", new { OrganizationId = organizationId, AsOfUtc = asOfUtc });

            var sql = baseQuery + " ORDER BY s.ProductId, s.WarehouseId OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var items = await _connection.QueryAsync<HistoricalBalanceDto>(
                sql, new { OrganizationId = organizationId, AsOfUtc = asOfUtc, Offset = (page - 1) * pageSize, PageSize = pageSize });

            return new PaginatedResult<HistoricalBalanceDto>(items.ToList(), total, page, pageSize);
        }

        public async Task<IEnumerable<BatchDto>> GetBatchesWithZeroRemainingAsync(Guid organizationId)
        {
            var sql = $"SELECT Id, ProductId, PurchaseItemId, OrganizationId, InitialQuantity, RemainingQuantity, UnitCost, CreatedAt FROM {TableBatches} WHERE OrganizationId = @OrganizationId AND RemainingQuantity = 0 ORDER BY CreatedAt DESC";
            return await _connection.QueryAsync<BatchDto>(sql, new { OrganizationId = organizationId });
        }

        // ---- Scalar aggregates ---------------------------------------------------------------

        public async Task<int> GetWarehouseCountAsync(Guid organizationId)
        {
            var sql = $"SELECT COUNT(*) FROM {TableWarehouses} WHERE OrganizationId = @OrganizationId";
            return await _connection.ExecuteScalarAsync<int>(sql, new { OrganizationId = organizationId });
        }

        public async Task<int> GetActiveWarehouseCountAsync(Guid organizationId)
        {
            var sql = $"SELECT COUNT(*) FROM {TableWarehouses} WHERE OrganizationId = @OrganizationId AND IsActive = 1";
            return await _connection.ExecuteScalarAsync<int>(sql, new { OrganizationId = organizationId });
        }

        public async Task<int> GetBatchCountAsync(Guid organizationId)
        {
            var sql = $"SELECT COUNT(*) FROM {TableBatches} WHERE OrganizationId = @OrganizationId";
            return await _connection.ExecuteScalarAsync<int>(sql, new { OrganizationId = organizationId });
        }

        public async Task<int> GetExhaustedBatchCountAsync(Guid organizationId)
        {
            var sql = $"SELECT COUNT(*) FROM {TableBatches} WHERE OrganizationId = @OrganizationId AND RemainingQuantity = 0";
            return await _connection.ExecuteScalarAsync<int>(sql, new { OrganizationId = organizationId });
        }

        public async Task<int> GetTotalUnitsInWarehousesAsync(Guid organizationId)
        {
            var sql = $"SELECT COALESCE(SUM(Quantity), 0) FROM {TableWarehouseBatches} WHERE OrganizationId = @OrganizationId";
            return await _connection.ExecuteScalarAsync<int>(sql, new { OrganizationId = organizationId });
        }

        public async Task<int> GetTotalUnallocatedUnitsAsync(Guid organizationId)
        {
            var sql = $"SELECT COALESCE(SUM(RemainingQuantity), 0) FROM {TableBatches} WHERE OrganizationId = @OrganizationId";
            return await _connection.ExecuteScalarAsync<int>(sql, new { OrganizationId = organizationId });
        }
    }
}
