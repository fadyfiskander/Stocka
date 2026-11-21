using BuildingBlocks.Application.Common.Models;
using Inventory.Contracts.DTOs.Batch;
using Inventory.Contracts.DTOs.Reports;
using Inventory.Contracts.DTOs.StockMovement;

namespace Inventory.Application.Repositories.Queries
{
    /// <summary>
    /// Read-side reporting repository (Dapper). All queries are scoped to the current user's
    /// organization, parameterized, database-aggregated and return untracked DTO projections.
    /// Never a write source of truth: current quantities always come from
    /// Inventory.WarehouseBatches / Inventory.Batches.
    /// </summary>
    public interface IInventoryReportQueryRepository
    {
        // ---- Warehouse reports ----
        Task<IEnumerable<WarehouseStockSummaryDto>> GetWarehouseStockSummaryAsync(
            CancellationToken cancellationToken = default);

        Task<PaginatedResult<WarehouseStockSummaryDto>> GetWarehousesRankedByStockAsync(
            bool descending,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<PaginatedResult<WarehouseInventoryItemDto>> GetWarehouseInventoryAsync(
            Guid warehouseId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<WarehouseStockSummaryDto>> GetZeroStockWarehousesAsync(
            CancellationToken cancellationToken = default);

        Task<IEnumerable<WarehouseStockSummaryDto>> GetInactiveWarehousesWithStockAsync(
            CancellationToken cancellationToken = default);

        // ---- Stock quantity reports ----
        Task<IEnumerable<ProductStockRowDto>> GetProductStockDistributionAsync(
            Guid productId,
            CancellationToken cancellationToken = default);

        Task<int> GetProductTotalUnitsAsync(
            Guid productId,
            CancellationToken cancellationToken = default);

        Task<int> GetProductUnallocatedUnitsAsync(
            Guid productId,
            CancellationToken cancellationToken = default);

        Task<PaginatedResult<ZeroStockProductDto>> GetZeroStockProductsAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        // ---- Valuation (recorded batch unit costs only — no valuation policy approved) ----
        Task<PaginatedResult<WarehouseStockSummaryDto>> GetWarehousesRankedByCostAsync(
            bool descending,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<decimal> GetTotalInventoryCostAsync(
            CancellationToken cancellationToken = default);

        // ---- Movement reports ----
        Task<IEnumerable<MovementTypeTotalsDto>> GetMovementTotalsByTypeAsync(
            DateTime? dateFromUtc,
            DateTime? dateToUtc,
            CancellationToken cancellationToken = default);

        Task<InboundOutboundSummaryDto> GetInboundOutboundTotalsAsync(
            DateTime dateFromUtc,
            DateTime dateToUtc,
            CancellationToken cancellationToken = default);

        /// <summary>Transfer operations, one row per transfer (TransferOut/TransferIn pair aggregated — no double counting).</summary>
        Task<PaginatedResult<TransferHistoryDto>> GetTransferHistoryAsync(
            int page,
            int pageSize,
            DateTime? dateFromUtc = null,
            DateTime? dateToUtc = null,
            CancellationToken cancellationToken = default);

        /// <summary>Adjustment-type movements (physical counts, damage write-offs, reversals) with recorded reasons.</summary>
        Task<PaginatedResult<StockMovementDto>> GetAdjustmentHistoryAsync(
            int page,
            int pageSize,
            DateTime? dateFromUtc = null,
            DateTime? dateToUtc = null,
            CancellationToken cancellationToken = default);

        // ---- Control / audit ----
        Task<PaginatedResult<HistoricalBalanceDto>> GetHistoricalBalanceAsync(
            DateTime asOfUtc,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<BatchDto>> GetBatchesWithZeroRemainingAsync(
            CancellationToken cancellationToken = default);

        // ---- Scalar aggregates for summaries ----
        Task<int> GetWarehouseCountAsync(
            CancellationToken cancellationToken = default);

        Task<int> GetActiveWarehouseCountAsync(
            CancellationToken cancellationToken = default);

        Task<int> GetBatchCountAsync(
            CancellationToken cancellationToken = default);

        Task<int> GetExhaustedBatchCountAsync(
            CancellationToken cancellationToken = default);

        Task<int> GetTotalUnitsInWarehousesAsync(
            CancellationToken cancellationToken = default);

        Task<int> GetTotalUnallocatedUnitsAsync(
            CancellationToken cancellationToken = default);
    }
}