using BuildingBlocks.Application.Common.Models;
using Inventory.Contracts.DTOs.Batch;
using Inventory.Contracts.DTOs.Reports;
using Inventory.Contracts.DTOs.StockMovement;

namespace Inventory.Contracts.UseCases.ReportCases;

/// <summary>
/// Inventory report use case contract: the pure module boundary for every report read.
/// Implemented by the Inventory Application layer and consumed via DI. All results are scoped
/// to the current Organization by the application pipeline (tenant context).
/// </summary>
public interface IInventoryReportCases
{
    #region Warehouse reports
    /// <summary>Retrieves a per-warehouse stock rollup.</summary>
    Task<IEnumerable<WarehouseStockSummaryDto>> GetWarehouseStockSummaryAsync(WarehouseStockSummaryQuery query, CancellationToken cancellationToken = default);

    /// <summary>Retrieves warehouses ranked by current units, paginated.</summary>
    Task<PaginatedResult<WarehouseStockSummaryDto>> GetWarehousesRankedByStockAsync(WarehousesRankedByStockQuery query, CancellationToken cancellationToken = default);

    /// <summary>Retrieves warehouses ranked by total stock cost, paginated.</summary>
    Task<PaginatedResult<WarehouseStockSummaryDto>> GetWarehousesRankedByCostAsync(WarehousesRankedByCostQuery query, CancellationToken cancellationToken = default);

    /// <summary>Retrieves per-batch stock detail inside one warehouse, paginated.</summary>
    Task<PaginatedResult<WarehouseInventoryItemDto>> GetWarehouseInventoryAsync(WarehouseInventoryQuery query, CancellationToken cancellationToken = default);

    /// <summary>Retrieves warehouses currently holding zero units.</summary>
    Task<IEnumerable<WarehouseStockSummaryDto>> GetZeroStockWarehousesAsync(ZeroStockWarehousesQuery query, CancellationToken cancellationToken = default);

    /// <summary>Retrieves inactive warehouses that still hold stock (control report).</summary>
    Task<IEnumerable<WarehouseStockSummaryDto>> GetInactiveWarehousesWithStockAsync(InactiveWarehousesWithStockQuery query, CancellationToken cancellationToken = default);
    #endregion

    #region Stock quantity reports
    /// <summary>Retrieves products whose total units across the organization's warehouses are zero, paginated.</summary>
    Task<PaginatedResult<ZeroStockProductDto>> GetZeroStockProductsAsync(ZeroStockProductsQuery query, CancellationToken cancellationToken = default);

    /// <summary>Retrieves batches with no units left to allocate (RemainingQuantity = 0).</summary>
    Task<IEnumerable<BatchDto>> GetBatchesWithZeroRemainingAsync(BatchesWithZeroRemainingQuery query, CancellationToken cancellationToken = default);
    #endregion

    #region Movement reports
    /// <summary>Retrieves movement counts/quantities grouped by type within an optional date window.</summary>
    Task<IEnumerable<MovementTypeTotalsDto>> GetMovementTypeTotalsAsync(MovementTypeTotalsQuery query, CancellationToken cancellationToken = default);

    /// <summary>Retrieves inbound vs outbound totals across a required date window.</summary>
    Task<InboundOutboundSummaryDto> GetInboundOutboundTotalsAsync(InboundOutboundTotalsQuery query, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the paged transfer pair history within an optional date window.</summary>
    Task<PaginatedResult<TransferHistoryDto>> GetTransferHistoryAsync(TransferHistoryQuery query, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the paged adjustment movement history within an optional date window.</summary>
    Task<PaginatedResult<StockMovementDto>> GetAdjustmentHistoryAsync(AdjustmentHistoryQuery query, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the stock balance reconstructed from movement history as of a date.</summary>
    Task<PaginatedResult<HistoricalBalanceDto>> GetHistoricalBalanceAsync(HistoricalBalanceQuery query, CancellationToken cancellationToken = default);
    #endregion

    #region Summary
    /// <summary>Retrieves the organization-level scalar rollup backing the public Inventory report contract.</summary>
    Task<InventorySummaryDto> GetInventorySummaryAsync(InventorySummaryQuery query, CancellationToken cancellationToken = default);
    #endregion
}
