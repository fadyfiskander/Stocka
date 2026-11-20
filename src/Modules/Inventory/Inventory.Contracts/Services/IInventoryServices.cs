using Inventory.Contracts.DTOs;
using Inventory.Contracts.Requests;

namespace Inventory.Contracts.Services
{
    /// <summary>
    /// Public Inventory module contract. Read-only cross-module surfaces; Organization isolation is
    /// enforced by the application pipeline (tenant context), never by trusting caller-supplied IDs.
    /// </summary>
    public interface IInventoryServices
    {
        #region Inventory Reports
        Task<InventoryReport> GetInventoryReportAsync(InventoryReportRequest request, CancellationToken cancellationToken = default);
        #endregion

        #region Stock Availability (for Sales and other modules)
        Task<StockAvailabilityResponse> GetStockAvailabilityAsync(StockAvailabilityRequest request, CancellationToken cancellationToken = default);
        #endregion

        #region Warehouse Dictionary (for destination pickers)
        Task<IReadOnlyList<InventoryWarehouseDto>> GetActiveWarehousesAsync(CancellationToken cancellationToken = default);
        #endregion

        #region Inventory Management

        #endregion
    }
}
