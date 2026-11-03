using Inventory.Contracts.DTOs.Reports;
using Inventory.Contracts.UseCases.ReportCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Stocka.MVC.Areas.Inventory.Controllers
{
    /// <summary>Read-only reporting pages. All data is brand-scoped, database-aggregated, and paginated.</summary>
    [Area("Inventory")]
    [Route("[area]/[controller]")]
    [Authorize(Roles = "Owner")]
    public class ReportsController : Controller
    {
        private readonly IInventoryReportCases _reportCases;

        public ReportsController(IInventoryReportCases reportCases)
        {
            _reportCases = reportCases;
        }

        // GET: /Inventory/Reports
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Inventory Reports";
            var summary = await _reportCases.GetInventorySummaryAsync(new InventorySummaryQuery());
            var movementTotals = await _reportCases.GetMovementTypeTotalsAsync(new MovementTypeTotalsQuery());
            ViewBag.MovementTotals = movementTotals;
            return View(summary);
        }

        // GET: /Inventory/Reports/Warehouses?order=stock|cost&asc=&page=
        [HttpGet("Warehouses")]
        public async Task<IActionResult> Warehouses(string order = "stock", bool asc = false, int page = 1, int pageSize = 20)
        {
            ViewBag.Title = "Warehouse Inventory Reports";
            ViewBag.Order = order;
            ViewBag.Ascending = asc;

            ViewBag.RankedByStock = await _reportCases.GetWarehousesRankedByStockAsync(new WarehousesRankedByStockQuery(asc, page, pageSize));
            ViewBag.RankedByCost = await _reportCases.GetWarehousesRankedByCostAsync(new WarehousesRankedByCostQuery(asc, page, pageSize));
            ViewBag.ZeroStock = await _reportCases.GetZeroStockWarehousesAsync(new ZeroStockWarehousesQuery());
            ViewBag.InactiveWithStock = await _reportCases.GetInactiveWarehousesWithStockAsync(new InactiveWarehousesWithStockQuery());
            return View();
        }

        // GET: /Inventory/Reports/Products?page=
        [HttpGet("Products")]
        public async Task<IActionResult> Products(int page = 1, int pageSize = 20)
        {
            ViewBag.Title = "Zero-Stock Products";
            var result = await _reportCases.GetZeroStockProductsAsync(new ZeroStockProductsQuery(page, pageSize));
            return View(result);
        }

        // GET: /Inventory/Reports/Movements?from=&to=
        [HttpGet("Movements")]
        public async Task<IActionResult> Movements(DateTime? from, DateTime? to, int page = 1, int pageSize = 20)
        {
            ViewBag.Title = "Stock Movement Reports";
            ViewBag.From = from;
            ViewBag.To = to;

            var totals = await _reportCases.GetMovementTypeTotalsAsync(new MovementTypeTotalsQuery(from, to));

            if (from.HasValue && to.HasValue && from <= to)
            {
                ViewBag.InboundOutbound = await _reportCases.GetInboundOutboundTotalsAsync(new InboundOutboundTotalsQuery(from.Value, to.Value));
            }

            ViewBag.TransferHistory = await _reportCases.GetTransferHistoryAsync(new TransferHistoryQuery(page, pageSize, from, to));
            ViewBag.AdjustmentHistory = await _reportCases.GetAdjustmentHistoryAsync(new AdjustmentHistoryQuery(page, pageSize, from, to));
            return View(totals);
        }

        /// <summary>
        /// Stock balance reconstructed from the movement audit trail as of a date.
        /// Reliable from the first recorded movement onward — movements are never deleted or rewritten.
        /// </summary>
        // GET: /Inventory/Reports/Balance?asOf=&page=
        [HttpGet("Balance")]
        public async Task<IActionResult> Balance(DateTime? asOf, int page = 1, int pageSize = 20)
        {
            ViewBag.Title = "Historical Balance";
            ViewBag.AsOf = asOf ?? DateTime.UtcNow;
            var result = await _reportCases.GetHistoricalBalanceAsync(new HistoricalBalanceQuery(ViewBag.AsOf, page, pageSize));
            return View(result);
        }
    }
}
