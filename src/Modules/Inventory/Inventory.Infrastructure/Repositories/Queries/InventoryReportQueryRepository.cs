using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Models;
using Dapper;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.Batch;
using Inventory.Contracts.DTOs.Reports;
using Inventory.Contracts.DTOs.StockMovement;
using Inventory.Infrastructure.Repositories.Queries.Bases;

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
        public InventoryReportQueryRepository(IInventoryDbConnection connection, ICurrentUserService currentUser)
            : base(connection, currentUser)
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

        public async Task<IEnumerable<WarehouseStockSummaryDto>> GetWarehouseStockSummaryAsync(
            CancellationToken cancellationToken = default)
        {
            var sql = WarehouseStockSummarySql + " ORDER BY w.Name";
            var command = new CommandDefinition(
                sql,
                new { OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.QueryAsync<WarehouseStockSummaryDto>(command);
        }

        public async Task<PaginatedResult<WarehouseStockSummaryDto>> GetWarehousesRankedByStockAsync(
            bool descending,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var (total, items) = await GetPagedRankedAsync(
                " ORDER BY TotalUnits " + (descending ? "DESC" : "ASC") + ", w.Name ASC",
                page,
                pageSize,
                cancellationToken);

            return new PaginatedResult<WarehouseStockSummaryDto>(items, total, Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
        }

        public async Task<PaginatedResult<WarehouseStockSummaryDto>> GetWarehousesRankedByCostAsync(
            bool descending,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var (total, items) = await GetPagedRankedAsync(
                " ORDER BY TotalCost " + (descending ? "DESC" : "ASC") + ", w.Name ASC",
                page,
                pageSize,
                cancellationToken);

            return new PaginatedResult<WarehouseStockSummaryDto>(items, total, Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
        }

        private async Task<(int total, List<WarehouseStockSummaryDto> items)> GetPagedRankedAsync(
            string orderBy,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var countSql = $"SELECT COUNT(*) FROM {TableWarehouses} WHERE OrganizationId = @OrganizationId";
            var countCommand = new CommandDefinition(
                countSql,
                new { OrganizationId },
                cancellationToken: cancellationToken);

            var total = await _connection.ExecuteScalarAsync<int>(countCommand);

            var sql = WarehouseStockSummarySql + orderBy + " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var dp = new DynamicParameters();
            dp.Add("OrganizationId", OrganizationId);
            dp.Add("Offset", (page - 1) * pageSize);
            dp.Add("PageSize", pageSize);

            var itemsCommand = new CommandDefinition(
                sql,
                dp,
                cancellationToken: cancellationToken);

            var items = (await _connection.QueryAsync<WarehouseStockSummaryDto>(itemsCommand)).ToList();
            return (total, items);
        }

        public async Task<PaginatedResult<WarehouseInventoryItemDto>> GetWarehouseInventoryAsync(
            Guid warehouseId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var whereParts = " WHERE wb.WarehouseId = @WarehouseId AND wb.OrganizationId = @OrganizationId";
            var countSql = $"""
                SELECT COUNT(*) FROM {TableWarehouseBatches} wb
                JOIN {TableBatches} b ON b.Id = wb.BatchId AND b.OrganizationId = wb.OrganizationId
                """ + whereParts;

            var countCommand = new CommandDefinition(
                countSql,
                new { WarehouseId = warehouseId, OrganizationId },
                cancellationToken: cancellationToken);

            var total = await _connection.ExecuteScalarAsync<int>(countCommand);

            var sql = $"""
                SELECT wb.Id AS WarehouseBatchId, wb.BatchId, b.ProductId, wb.Quantity,
                       b.UnitCost AS BatchUnitCost, (wb.Quantity * b.UnitCost) AS LineCost,
                       b.CreatedAt AS BatchCreatedAt
                FROM {TableWarehouseBatches} wb
                JOIN {TableBatches} b ON b.Id = wb.BatchId AND b.OrganizationId = wb.OrganizationId
                """ + whereParts +
                " ORDER BY b.ProductId, wb.BatchId OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var dp = new DynamicParameters();
            dp.Add("WarehouseId", warehouseId);
            dp.Add("OrganizationId", OrganizationId);
            dp.Add("Offset", (page - 1) * pageSize);
            dp.Add("PageSize", pageSize);

            var itemsCommand = new CommandDefinition(
                sql,
                dp,
                cancellationToken: cancellationToken);

            var items = await _connection.QueryAsync<WarehouseInventoryItemDto>(itemsCommand);
            return new PaginatedResult<WarehouseInventoryItemDto>(items.ToList(), total, page, pageSize);
        }

        public async Task<IEnumerable<WarehouseStockSummaryDto>> GetZeroStockWarehousesAsync(
            CancellationToken cancellationToken = default)
        {
            var sql = $"{WarehouseStockSummarySql} HAVING COALESCE(SUM(wb.Quantity), 0) = 0 ORDER BY w.Name";
            var command = new CommandDefinition(
                sql,
                new { OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.QueryAsync<WarehouseStockSummaryDto>(command);
        }

        public async Task<IEnumerable<WarehouseStockSummaryDto>> GetInactiveWarehousesWithStockAsync(
            CancellationToken cancellationToken = default)
        {
            var sql = $"{WarehouseStockSummarySql} HAVING COALESCE(SUM(wb.Quantity), 0) > 0 AND w.IsActive = 0 ORDER BY w.Name";
            var command = new CommandDefinition(
                sql,
                new { OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.QueryAsync<WarehouseStockSummaryDto>(command);
        }

        // ---- Stock quantity reports ------------------------------------------------------

        public async Task<IEnumerable<ProductStockRowDto>> GetProductStockDistributionAsync(
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            var sql = $"""
                SELECT wb.WarehouseId, COALESCE(SUM(wb.Quantity), 0) AS Quantity
                FROM {TableWarehouseBatches} wb
                JOIN {TableBatches} b ON b.Id = wb.BatchId AND b.OrganizationId = wb.OrganizationId
                WHERE b.ProductId = @ProductId AND wb.OrganizationId = @OrganizationId AND wb.Quantity > 0
                GROUP BY wb.WarehouseId
                ORDER BY Quantity DESC
                """;

            var command = new CommandDefinition(
                sql,
                new { ProductId = productId, OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.QueryAsync<ProductStockRowDto>(command);
        }

        public async Task<int> GetProductTotalUnitsAsync(
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            var sql = $"""
                SELECT COALESCE(SUM(wb.Quantity), 0)
                FROM {TableWarehouseBatches} wb
                JOIN {TableBatches} b ON b.Id = wb.BatchId AND b.OrganizationId = wb.OrganizationId
                WHERE b.ProductId = @ProductId AND wb.OrganizationId = @OrganizationId
                """;

            var command = new CommandDefinition(
                sql,
                new { ProductId = productId, OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.ExecuteScalarAsync<int>(command);
        }

        public async Task<int> GetProductUnallocatedUnitsAsync(
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT COALESCE(SUM(RemainingQuantity), 0) FROM {TableBatches} WHERE ProductId = @ProductId AND OrganizationId = @OrganizationId";
            var command = new CommandDefinition(
                sql,
                new { ProductId = productId, OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.ExecuteScalarAsync<int>(command);
        }

        public async Task<PaginatedResult<ZeroStockProductDto>> GetZeroStockProductsAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

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

            var countCommand = new CommandDefinition(
                $"SELECT COUNT(*) FROM ({baseQuery}) z",
                new { OrganizationId },
                cancellationToken: cancellationToken);

            var total = await _connection.ExecuteScalarAsync<int>(countCommand);

            var sql = baseQuery + " ORDER BY b.ProductId OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var dp = new DynamicParameters();
            dp.Add("OrganizationId", OrganizationId);
            dp.Add("Offset", (page - 1) * pageSize);
            dp.Add("PageSize", pageSize);

            var itemsCommand = new CommandDefinition(
                sql,
                dp,
                cancellationToken: cancellationToken);

            var items = await _connection.QueryAsync<ZeroStockProductDto>(itemsCommand);
            return new PaginatedResult<ZeroStockProductDto>(items.ToList(), total, page, pageSize);
        }

        // ---- Valuation (recorded batch unit costs only) ----------------------------------

        public async Task<decimal> GetTotalInventoryCostAsync(
            CancellationToken cancellationToken = default)
        {
            var sql = $"""
                SELECT COALESCE(SUM(wb.Quantity * b.UnitCost), 0)
                FROM {TableWarehouseBatches} wb
                JOIN {TableBatches} b ON b.Id = wb.BatchId AND b.OrganizationId = wb.OrganizationId
                WHERE wb.OrganizationId = @OrganizationId
                """;

            var command = new CommandDefinition(
                sql,
                new { OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.ExecuteScalarAsync<decimal>(command);
        }

        // ---- Movement reports -------------------------------------------------------------

        public async Task<IEnumerable<MovementTypeTotalsDto>> GetMovementTotalsByTypeAsync(
            DateTime? dateFromUtc,
            DateTime? dateToUtc,
            CancellationToken cancellationToken = default)
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

            var command = new CommandDefinition(
                sql,
                new { OrganizationId, DateFromUtc = dateFromUtc, DateToUtc = dateToUtc },
                cancellationToken: cancellationToken);

            return await _connection.QueryAsync<MovementTypeTotalsDto>(command);
        }

        public async Task<InboundOutboundSummaryDto> GetInboundOutboundTotalsAsync(
            DateTime dateFromUtc,
            DateTime dateToUtc,
            CancellationToken cancellationToken = default)
        {
            var sql = $"""
                SELECT
                    COALESCE(SUM(CASE WHEN MovementType IN (0, 4, 6) THEN Quantity ELSE 0 END), 0) AS InboundUnits,
                    COALESCE(SUM(CASE WHEN MovementType IN (1, 5) THEN Quantity ELSE 0 END), 0) AS OutboundUnits
                FROM {TableStockMovements}
                WHERE OrganizationId = @OrganizationId AND CreatedAt >= @DateFromUtc AND CreatedAt <= @DateToUtc
                """;

            var command = new CommandDefinition(
                sql,
                new { OrganizationId, DateFromUtc = dateFromUtc, DateToUtc = dateToUtc },
                cancellationToken: cancellationToken);

            var row = await _connection.QuerySingleAsync<(int InboundUnits, int OutboundUnits)>(command);
            return new InboundOutboundSummaryDto(dateFromUtc, dateToUtc, row.InboundUnits, row.OutboundUnits);
        }

        public async Task<PaginatedResult<TransferHistoryDto>> GetTransferHistoryAsync(
            int page,
            int pageSize,
            DateTime? dateFromUtc = null,
            DateTime? dateToUtc = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var dateFilter = string.Empty;
            if (dateFromUtc.HasValue) dateFilter += " AND s.CreatedAt >= @DateFromUtc";
            if (dateToUtc.HasValue) dateFilter += " AND s.CreatedAt <= @DateToUtc";

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

            var countCommand = new CommandDefinition(
                $"SELECT COUNT(*) FROM ({baseQuery}) t",
                new { OrganizationId, DateFromUtc = dateFromUtc, DateToUtc = dateToUtc },
                cancellationToken: cancellationToken);

            var total = await _connection.ExecuteScalarAsync<int>(countCommand);

            var sql = baseQuery + " ORDER BY MIN(s.CreatedAt) DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var dp = new DynamicParameters();
            dp.Add("OrganizationId", OrganizationId);
            dp.Add("DateFromUtc", dateFromUtc);
            dp.Add("DateToUtc", dateToUtc);
            dp.Add("Offset", (page - 1) * pageSize);
            dp.Add("PageSize", pageSize);

            var itemsCommand = new CommandDefinition(
                sql,
                dp,
                cancellationToken: cancellationToken);

            var items = await _connection.QueryAsync<TransferHistoryDto>(itemsCommand);
            return new PaginatedResult<TransferHistoryDto>(items.ToList(), total, page, pageSize);
        }

        public async Task<PaginatedResult<StockMovementDto>> GetAdjustmentHistoryAsync(
            int page,
            int pageSize,
            DateTime? dateFromUtc = null,
            DateTime? dateToUtc = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var dateFilter = string.Empty;
            if (dateFromUtc.HasValue) dateFilter += " AND s.CreatedAt >= @DateFromUtc";
            if (dateToUtc.HasValue) dateFilter += " AND s.CreatedAt <= @DateToUtc";

            var countCommand = new CommandDefinition(
                $"SELECT COUNT(*) FROM {TableStockMovements} s WHERE s.OrganizationId = @OrganizationId AND s.MovementType IN (4, 5){dateFilter}",
                new { OrganizationId, DateFromUtc = dateFromUtc, DateToUtc = dateToUtc },
                cancellationToken: cancellationToken);

            var total = await _connection.ExecuteScalarAsync<int>(countCommand);

            var sql = $"SELECT Id, ProductId, BatchId, WarehouseId, OrganizationId, Quantity, MovementType, ReferenceType, ReferenceId, CreatedAt, Reason FROM {TableStockMovements} s WHERE s.OrganizationId = @OrganizationId AND s.MovementType IN (4, 5){dateFilter} ORDER BY s.CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var dp = new DynamicParameters();
            dp.Add("OrganizationId", OrganizationId);
            dp.Add("DateFromUtc", dateFromUtc);
            dp.Add("DateToUtc", dateToUtc);
            dp.Add("Offset", (page - 1) * pageSize);
            dp.Add("PageSize", pageSize);

            var itemsCommand = new CommandDefinition(
                sql,
                dp,
                cancellationToken: cancellationToken);

            var items = await _connection.QueryAsync<StockMovementDto>(itemsCommand);
            return new PaginatedResult<StockMovementDto>(items.ToList(), total, page, pageSize);
        }

        // ---- Control / audit ----------------------------------------------------------------

        public async Task<PaginatedResult<HistoricalBalanceDto>> GetHistoricalBalanceAsync(
            DateTime asOfUtc,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            const string baseQuery = $"""
                SELECT s.ProductId, s.WarehouseId,
                       SUM(CASE WHEN s.MovementType IN (0, 2, 4, 6) THEN s.Quantity ELSE -s.Quantity END) AS Quantity,
                       COUNT(*) AS MovementsConsidered
                FROM {TableStockMovements} s
                WHERE s.OrganizationId = @OrganizationId AND s.CreatedAt <= @AsOfUtc
                GROUP BY s.ProductId, s.WarehouseId
                HAVING SUM(CASE WHEN s.MovementType IN (0, 2, 4, 6) THEN s.Quantity ELSE -s.Quantity END) <> 0
                """;

            var countCommand = new CommandDefinition(
                $"SELECT COUNT(*) FROM ({baseQuery}) h",
                new { OrganizationId, AsOfUtc = asOfUtc },
                cancellationToken: cancellationToken);

            var total = await _connection.ExecuteScalarAsync<int>(countCommand);

            var sql = baseQuery + " ORDER BY s.ProductId, s.WarehouseId OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var dp = new DynamicParameters();
            dp.Add("OrganizationId", OrganizationId);
            dp.Add("AsOfUtc", asOfUtc);
            dp.Add("Offset", (page - 1) * pageSize);
            dp.Add("PageSize", pageSize);

            var itemsCommand = new CommandDefinition(
                sql,
                dp,
                cancellationToken: cancellationToken);

            var items = await _connection.QueryAsync<HistoricalBalanceDto>(itemsCommand);
            return new PaginatedResult<HistoricalBalanceDto>(items.ToList(), total, page, pageSize);
        }

        public async Task<IEnumerable<BatchDto>> GetBatchesWithZeroRemainingAsync(
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT Id, ProductId, PurchaseItemId, OrganizationId, InitialQuantity, RemainingQuantity, UnitCost, CreatedAt FROM {TableBatches} WHERE OrganizationId = @OrganizationId AND RemainingQuantity = 0 ORDER BY CreatedAt DESC";
            var command = new CommandDefinition(
                sql,
                new { OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.QueryAsync<BatchDto>(command);
        }

        // ---- Scalar aggregates ---------------------------------------------------------------

        public async Task<int> GetWarehouseCountAsync(
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT COUNT(*) FROM {TableWarehouses} WHERE OrganizationId = @OrganizationId";
            var command = new CommandDefinition(
                sql,
                new { OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.ExecuteScalarAsync<int>(command);
        }

        public async Task<int> GetActiveWarehouseCountAsync(
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT COUNT(*) FROM {TableWarehouses} WHERE OrganizationId = @OrganizationId AND IsActive = 1";
            var command = new CommandDefinition(
                sql,
                new { OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.ExecuteScalarAsync<int>(command);
        }

        public async Task<int> GetBatchCountAsync(
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT COUNT(*) FROM {TableBatches} WHERE OrganizationId = @OrganizationId";
            var command = new CommandDefinition(
                sql,
                new { OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.ExecuteScalarAsync<int>(command);
        }

        public async Task<int> GetExhaustedBatchCountAsync(
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT COUNT(*) FROM {TableBatches} WHERE OrganizationId = @OrganizationId AND RemainingQuantity = 0";
            var command = new CommandDefinition(
                sql,
                new { OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.ExecuteScalarAsync<int>(command);
        }

        public async Task<int> GetTotalUnitsInWarehousesAsync(
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT COALESCE(SUM(Quantity), 0) FROM {TableWarehouseBatches} WHERE OrganizationId = @OrganizationId";
            var command = new CommandDefinition(
                sql,
                new { OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.ExecuteScalarAsync<int>(command);
        }

        public async Task<int> GetTotalUnallocatedUnitsAsync(
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT COALESCE(SUM(RemainingQuantity), 0) FROM {TableBatches} WHERE OrganizationId = @OrganizationId";
            var command = new CommandDefinition(
                sql,
                new { OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.ExecuteScalarAsync<int>(command);
        }
    }
}