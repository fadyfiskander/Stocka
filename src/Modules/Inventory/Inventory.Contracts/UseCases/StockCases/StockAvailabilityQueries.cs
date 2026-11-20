using Inventory.Contracts.DTOs.Reports;

namespace Inventory.Contracts.UseCases.StockCases;

/// <summary>Current stock of a product: organization-wide total plus per-warehouse distribution.</summary>
/// <param name="ProductId">The product whose stock is queried.</param>
/// <param name="WarehouseId">Optional: restricts the returned distribution to one warehouse.</param>
/// <remarks>
/// No dedicated validator: an empty ProductId is rejected by the handler with an argument error.
/// </remarks>
public record GetProductStockQuery(Guid ProductId, Guid? WarehouseId = null);

/// <summary>Total units of one product still unallocated across all its batches.</summary>
/// <param name="ProductId">The product whose unallocated units are counted.</param>
public record GetProductUnallocatedUnitsQuery(Guid ProductId);

/// <summary>Current quantity held by one batch in one warehouse (0 when not allocated there).</summary>
/// <param name="BatchId">The batch to inspect.</param>
/// <param name="WarehouseId">The warehouse to inspect.</param>
public record GetBatchQuantityInWarehouseQuery(Guid BatchId, Guid WarehouseId);

/// <summary>
/// Total current units held in one warehouse. IMPORTANT: a raw unit count across products
/// whose units of measure may differ â€” not a normalized measure of warehouse capacity.
/// </summary>
public record GetWarehouseTotalUnitsQuery(Guid WarehouseId);
