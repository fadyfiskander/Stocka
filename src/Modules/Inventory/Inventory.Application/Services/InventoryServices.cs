using Inventory.Contracts.DTOs;
using Inventory.Contracts.Requests;
using Inventory.Contracts.Services;
using Inventory.Contracts.UseCases.ReportCases;
using Inventory.Contracts.UseCases.StockCases;
using Inventory.Contracts.UseCases.WarehouseCases;

namespace Inventory.Application.Services
{
    /// <summary>
    /// Public cross-module adapter: maps internal application results onto Contract DTOs.
    /// Contains no business logic — orchestration lives in the use case handlers.
    /// Each call is dispatched through the pure contract interfaces (implemented by the
    /// MediatR pipeline adapters), so validation and unit-of-work behaviors stay in place.
    /// </summary>
    public class InventoryServices : IInventoryServices
    {
        private readonly IInventoryReportCases _reports;
        private readonly IStockCases _stockCases;
        private readonly IWarehouseCases _warehouseCases;

        public InventoryServices(
            IInventoryReportCases reports,
            IStockCases stockCases,
            IWarehouseCases warehouseCases)
        {
            _reports = reports;
            _stockCases = stockCases;
            _warehouseCases = warehouseCases;
        }

        public async Task<InventoryReport> GetInventoryReportAsync(InventoryReportRequest request, CancellationToken cancellationToken = default)
        {
            var summary = await _reports.GetInventorySummaryAsync(new InventorySummaryQuery(), cancellationToken);
            return new InventoryReport(
                summary.OrganizationId,
                summary.TotalWarehouses,
                summary.ActiveWarehouses,
                summary.TotalBatches,
                summary.ExhaustedBatches,
                summary.UnitsInWarehouses,
                summary.UnallocatedUnits,
                summary.TotalInventoryCost,
                summary.GeneratedAtUtc);
        }

        public async Task<StockAvailabilityResponse> GetStockAvailabilityAsync(StockAvailabilityRequest request, CancellationToken cancellationToken = default)
        {
            if (request.ProductId == Guid.Empty)
                throw new ArgumentException("Product ID is required.", nameof(request.ProductId));

            var stock = await _stockCases.GetProductStockAsync(
                new GetProductStockQuery(request.ProductId, request.WarehouseId), cancellationToken);

            var unallocated = await _stockCases.GetProductUnallocatedUnitsAsync(
                new GetProductUnallocatedUnitsQuery(request.ProductId), cancellationToken);

            return new StockAvailabilityResponse(
                stock.ProductId,
                request.WarehouseId,
                stock.TotalQuantity,
                unallocated,
                stock.Distribution.Select(d => new StockAvailabilityRow(d.WarehouseId, d.Quantity)).ToList());
        }

        public async Task<IReadOnlyList<InventoryWarehouseDto>> GetActiveWarehousesAsync(CancellationToken cancellationToken = default)
        {
            var warehouses = await _warehouseCases.ListWarehousesAsync(new ListWarehousesQuery
            {
                IsActive = true,
                Page = 1,
                // Dictionary-style read: return the full active set (bounded by warehouse count per Organization).
                PageSize = 100
            }, cancellationToken);

            return warehouses.Items
                .Select(w => new InventoryWarehouseDto(w.Id, w.Name, w.Location, w.IsActive))
                .ToList();
        }
    }
}
