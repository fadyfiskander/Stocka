namespace Inventory.Contracts.Requests
{
    /// <summary>Requests organization-level inventory rollup for the current tenant context.</summary>
    public record InventoryReportRequest(
        Guid? OrganizationId = null
    );

    /// <summary>Product availability lookup for other modules (e.g., Sales checkout).</summary>
    public record StockAvailabilityRequest(
        Guid ProductId,
        /// <summary>Null = totals across the whole Organization; non-empty = single warehouse.</summary>
        Guid? WarehouseId = null
    );
}
