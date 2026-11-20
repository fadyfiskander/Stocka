namespace Inventory.Contracts.DTOs
{
    /// <summary>
    /// Organization-level inventory rollup exposed cross-module.
    /// UnitTotals is a raw unit count (products may have different units of measure).
    /// TotalInventoryCost uses the recorded per-batch unit costs only — no inventory
    /// valuation policy (FIFO / weighted average) has been approved, so treat it as
    /// traceable cost data, not an approved financial valuation.
    /// </summary>
    public record InventoryReport(
        Guid OrganizationId,
        int TotalWarehouses,
        int ActiveWarehouses,
        int TotalBatches,
        int ExhaustedBatches,
        int UnitsInWarehouses,
        int UnallocatedUnits,
        decimal TotalInventoryCost,
        DateTime GeneratedAtUtc
    );

    /// <summary>Batch view for cross-module consumers.</summary>
    public record InventoryBatchReport(
        Guid BatchId,
        Guid ProductId,
        int InitialQuantity,
        int UnallocatedQuantity,
        int QuantityInWarehouses,
        decimal UnitCost,
        DateTime CreatedAt
    );
}
