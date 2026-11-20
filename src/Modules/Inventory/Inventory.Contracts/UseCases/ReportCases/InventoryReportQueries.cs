using BuildingBlocks.Application.Common.Models;
using Inventory.Contracts.DTOs.Batch;
using Inventory.Contracts.DTOs.Reports;
using Inventory.Contracts.DTOs.StockMovement;

namespace Inventory.Contracts.UseCases.ReportCases;

#region Warehouse reports
/// <summary>Per-warehouse stock rollup (units/cost share the WarehouseStockSummaryDto caveats).</summary>
public record WarehouseStockSummaryQuery;

/// <summary>Warehouses ranked by current units (descending by default), paginated.</summary>
/// <param name="Descending">Ranking direction; true = biggest stock first.</param>
/// <param name="Page">Requested page number (clamped to â‰¥ 1).</param>
/// <param name="PageSize">Page size (clamped to [1, 100]).</param>
public record WarehousesRankedByStockQuery(bool Descending = true, int Page = 1, int PageSize = 20);

/// <summary>Warehouses ranked by total stock cost (descending by default), paginated.</summary>
/// <param name="Descending">Ranking direction; true = highest cost first.</param>
/// <param name="Page">Requested page number (clamped to â‰¥ 1).</param>
/// <param name="PageSize">Page size (clamped to [1, 100]).</param>
public record WarehousesRankedByCostQuery(bool Descending = true, int Page = 1, int PageSize = 20);

/// <summary>Per-batch stock detail inside one warehouse, paginated.</summary>
/// <param name="WarehouseId">The warehouse to inspect.</param>
/// <param name="Page">Requested page number (clamped to â‰¥ 1).</param>
/// <param name="PageSize">Page size (clamped to [1, 100]).</param>
public record WarehouseInventoryQuery(Guid WarehouseId, int Page = 1, int PageSize = 20);

/// <summary>Warehouses currently holding zero units.</summary>
public record ZeroStockWarehousesQuery;

/// <summary>Inactive warehouses that still hold stock â€” surfaced by the control report.</summary>
public record InactiveWarehousesWithStockQuery;
#endregion

#region Stock quantity reports
/// <summary>Products whose total units across the organization's warehouses are zero, paginated.</summary>
/// <param name="Page">Requested page number (clamped to â‰¥ 1).</param>
/// <param name="PageSize">Page size (clamped to [1, 100]).</param>
public record ZeroStockProductsQuery(int Page = 1, int PageSize = 20);

/// <summary>Batches with no units left in their placement quantity (RemainingQuantity = 0).</summary>
public record BatchesWithZeroRemainingQuery;
#endregion

#region Movement reports
/// <summary>Movement counts/quantities grouped by type within an optional date window (UTC).</summary>
/// <param name="DateFromUtc">Inclusive lower bound; null disables the bound.</param>
/// <param name="DateToUtc">Inclusive upper bound; null disables the bound.</param>
public record MovementTypeTotalsQuery(DateTime? DateFromUtc = null, DateTime? DateToUtc = null);

/// <summary>Inbound vs outbound totals across a required date window (UTC).</summary>
/// <param name="DateFromUtc">Inclusive lower bound; must not be after DateToUtc.</param>
/// <param name="DateToUtc">Inclusive upper bound.</param>
public record InboundOutboundTotalsQuery(DateTime DateFromUtc, DateTime DateToUtc);

/// <summary>Paged transfer pair history within an optional date window (UTC).</summary>
/// <param name="Page">Requested page number (clamped to â‰¥ 1).</param>
/// <param name="PageSize">Page size (clamped to [1, 100]).</param>
/// <param name="DateFromUtc">Inclusive lower bound; null disables the bound.</param>
/// <param name="DateToUtc">Inclusive upper bound; null disables the bound.</param>
public record TransferHistoryQuery(int Page = 1, int PageSize = 20, DateTime? DateFromUtc = null, DateTime? DateToUtc = null);

/// <summary>Paged adjustment movement history within an optional date window (UTC).</summary>
/// <param name="Page">Requested page number (clamped to â‰¥ 1).</param>
/// <param name="PageSize">Page size (clamped to [1, 100]).</param>
/// <param name="DateFromUtc">Inclusive lower bound; null disables the bound.</param>
/// <param name="DateToUtc">Inclusive upper bound; null disables the bound.</param>
public record AdjustmentHistoryQuery(int Page = 1, int PageSize = 20, DateTime? DateFromUtc = null, DateTime? DateToUtc = null);

/// <summary>Stock balance reconstructed from movement history as of a date (inclusive).</summary>
/// <param name="AsOfUtc">The date to accumulate movements up to (inclusive, UTC).</param>
/// <param name="Page">Requested page number (clamped to â‰¥ 1).</param>
/// <param name="PageSize">Page size (clamped to [1, 100]).</param>
public record HistoricalBalanceQuery(DateTime AsOfUtc, int Page = 1, int PageSize = 20);
#endregion

#region Summary
/// <summary>Organization-level scalar rollup backing the public Inventory report contract.</summary>
public record InventorySummaryQuery;
#endregion
