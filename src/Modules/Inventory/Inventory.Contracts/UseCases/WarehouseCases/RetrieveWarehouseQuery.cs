using Inventory.Contracts.DTOs.Warehouse;

namespace Inventory.Contracts.UseCases.WarehouseCases;

/// <summary>
/// Retrieve one warehouse by ID. Cross-Organization IDs are indistinguishable from not-found.
/// </summary>
/// <param name="WarehouseId">The unique identifier of the warehouse to retrieve.</param>
public record RetrieveWarehouseQuery(Guid WarehouseId);
