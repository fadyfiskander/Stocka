using BuildingBlocks.Application.Common.Models;
using Inventory.Contracts.DTOs.Batch;
using Inventory.Contracts.DTOs.Reports;
using Inventory.Contracts.DTOs.StockMovement;

namespace Inventory.Application.Repositories.Queries
{
    /// <summary>
    /// Read-side reporting repository (Dapper). All queries are organization-scoped, parameterized,
    /// database-aggregated and return untracked DTO projections. Never a write source of truth:
    /// current quantities always come from Inventory.WarehouseBatches / Inventory.Batches.
    /// </summary>
    public interface IInventoryReportQueryRepository
    {
        // ---- Warehouse reports ----
        Task<IEnumerable<WarehouseStockSummaryDto>> GetWarehouseStockSummaryAsync(Guid organizationId);
        Task<PaginatedResult<WarehouseStockSummaryDto>> GetWarehousesRankedByStockAsync(Guid organizationId, bool descending, int page, int pageSize);
        Task<PaginatedResult<WarehouseInventoryItemDto>> GetWarehouseInventoryAsync(Guid organizationId, Guid warehouseId, int page, int pageSize);
        Task<IEnumerable<WarehouseStockSummaryDto>> GetZeroStockWarehousesAsync(Guid organizationId);
        Task<IEnumerable<WarehouseStockSummaryDto>> GetInactiveWarehousesWithStockAsync(Guid organizationId);

        // ---- Stock quantity reports ----
        Task<IEnumerable<ProductStockRowDto>> GetProductStockDistributionAsync(Guid organizationId, Guid productId);
        Task<int> GetProductTotalUnitsAsync(Guid organizationId, Guid productId);
        Task<int> GetProductUnallocatedUnitsAsync(Guid organizationId, Guid productId);
        Task<PaginatedResult<ZeroStockProductDto>> GetZeroStockProductsAsync(Guid organizationId, int page, int pageSize);
        /// <summary>Warehouse/batch rows that still hold a zero quantity (batch fully consumed in that warehouse).</summary>
        Task<int> GetZeroStockWarehouseBatchRowsCountAsync(Guid organizationId);

        // ---- Valuation (recorded batch unit costs only â€” no valuation policy approved) ----
        Task<PaginatedResult<WarehouseStockSummaryDto>> GetWarehousesRankedByCostAsync(Guid organizationId, bool descending, int page, int pageSize);
        Task<decimal> GetTotalInventoryCostAsync(Guid organizationId);

        // ---- Movement reports ----
        Task<IEnumerable<MovementTypeTotalsDto>> GetMovementTotalsByTypeAsync(Guid organizationId, DateTime? dateFromUtc, DateTime? dateToUtc);
        Task<InboundOutboundSummaryDto> GetInboundOutboundTotalsAsync(Guid organizationId, DateTime dateFromUtc, DateTime dateToUtc);
        /// <summary>Transfer operations, one row per transfer (TransferOut/TransferIn pair aggregated â€” no double counting).</summary>
        Task<PaginatedResult<TransferHistoryDto>> GetTransferHistoryAsync(Guid organizationId, int page, int pageSize, DateTime? dateFromUtc = null, DateTime? dateToUtc = null);
        /// <summary>Adjustment-type movements (physical counts, damage write-offs, reversals) with recorded reasons.</summary>
        Task<PaginatedResult<StockMovementDto>> GetAdjustmentHistoryAsync(Guid organizationId, int page, int pageSize, DateTime? dateFromUtc = null, DateTime? dateToUtc = null);

        // ---- Control / audit ----
        Task<PaginatedResult<HistoricalBalanceDto>> GetHistoricalBalanceAsync(Guid organizationId, DateTime asOfUtc, int page, int pageSize);
        Task<IEnumerable<BatchDto>> GetBatchesWithZeroRemainingAsync(Guid organizationId);

        // ---- Scalar aggregates for summaries ----
        Task<int> GetWarehouseCountAsync(Guid organizationId);
        Task<int> GetActiveWarehouseCountAsync(Guid organizationId);
        Task<int> GetBatchCountAsync(Guid organizationId);
        Task<int> GetExhaustedBatchCountAsync(Guid organizationId);
        Task<int> GetTotalUnitsInWarehousesAsync(Guid organizationId);
        Task<int> GetTotalUnallocatedUnitsAsync(Guid organizationId);
    }
}
