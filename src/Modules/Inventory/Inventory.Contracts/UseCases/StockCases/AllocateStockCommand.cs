using Inventory.Contracts.DTOs.Stock;

namespace Inventory.Contracts.UseCases.StockCases;

/// <summary>
/// Places units of a batch that are NOT yet allocated (RemainingQuantity) into a warehouse.
/// Effects (single transaction):
///   1. batch.RemainingQuantity -= quantity   (allocation bookkeeping)
///   2. WarehouseBatch created or increased   (current stock)
///   3. PurchaseIn movement recorded (Purchase reference = the batch's purchase item),
///      representing stock actually entering the warehouse.
/// Allocation is NOT a new purchase receipt: the batch already exists from a receipt.
/// Cannot exceed the batch's allocatable (unallocated) quantity.
/// </summary>
public record AllocateStockCommand
{
    /// <summary>The batch whose unallocated units are being placed.</summary>
    public Guid BatchId { get; init; }

    /// <summary>The destination warehouse (must belong to the Organization and be active).</summary>
    public Guid WarehouseId { get; init; }

    /// <summary>Number of units to allocate; must not exceed the batch's RemainingQuantity.</summary>
    public int Quantity { get; init; }
}
