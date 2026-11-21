using Inventory.Contracts.DTOs.Warehouse;
using Inventory.Contracts.UseCases.ReportCases;
using Inventory.Contracts.UseCases.WarehouseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stocka.MVC.Localization;

namespace Stocka.MVC.Areas.Inventory.Controllers
{
    [Area("Inventory")]
    [Route("[area]/[controller]")]
    [Authorize(Roles = "Owner")]
    public class WarehousesController : Controller
    {
        private readonly IWarehouseCases _warehouseCases;
        private readonly IInventoryReportCases _reportCases;

        public WarehousesController(IWarehouseCases warehouseCases, IInventoryReportCases reportCases)
        {
            _warehouseCases = warehouseCases;
            _reportCases = reportCases;
        }

        // GET: /Inventory/Warehouses
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(bool? isActive, int page = 1, int pageSize = 20)
        {
            ViewBag.Title = Loc.T("Warehouses_Title");
            var query = new ListWarehousesQuery { IsActive = isActive, Page = page, PageSize = pageSize };
            var warehouses = await _warehouseCases.ListWarehousesAsync(query);
            return View(warehouses);
        }

        // GET: /Inventory/Warehouses/Details/{id}?inventoryPage=1
        [HttpGet("Details/{id:guid}")]
        public async Task<IActionResult> Details(Guid id, int inventoryPage = 1, int pageSize = 20)
        {
            if (id == Guid.Empty)
            {
                TempData["Error"] = Loc.T("Msg_WarehouseIdRequired");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                ViewBag.Title = Loc.T("Warehouses_DetailsTitle");
                var warehouse = await _warehouseCases.RetrieveWarehouseAsync(new RetrieveWarehouseQuery(id));
                var inventory = await _reportCases.GetWarehouseInventoryAsync(new WarehouseInventoryQuery(id, inventoryPage, pageSize));
                var rankedStock = await _reportCases.GetWarehousesRankedByStockAsync(new WarehousesRankedByStockQuery(Descending: true, Page: 1, PageSize: 100));
                ViewBag.TotalUnits = rankedStock.Items.FirstOrDefault(w => w.WarehouseId == id)?.TotalUnits ?? 0;
                ViewBag.InventoryPage = inventoryPage;
                ViewBag.Inventory = inventory;
                return View(warehouse);
            }
            catch (KeyNotFoundException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Inventory/Warehouses/Create
        [HttpGet("Create")]
        public IActionResult Create()
        {
            ViewBag.Title = Loc.T("Warehouses_CreateTitle");
            return View(new RegisterWarehouseCommand());
        }

        // POST: /Inventory/Warehouses/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterWarehouseCommand command)
        {
            if (!ModelState.IsValid)
                return View(command);

            try
            {
                var result = await _warehouseCases.RegisterWarehouseAsync(command);
                TempData["Success"] = Loc.F("Msg_WarehouseRegistered", result.Name);
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(command);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(command);
            }
        }

        // GET: /Inventory/Warehouses/Edit/{id}
        [HttpGet("Edit/{id:guid}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["Error"] = Loc.T("Msg_WarehouseIdRequired");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var warehouse = await _warehouseCases.RetrieveWarehouseAsync(new RetrieveWarehouseQuery(id));
                ViewBag.Title = Loc.T("Warehouses_EditTitle");
                return View(new UpdateWarehouseCommand
                {
                    WarehouseId = warehouse.Id,
                    Name = warehouse.Name,
                    Location = warehouse.Location,
                    Description = warehouse.Description,
                    Type = warehouse.Type
                });
            }
            catch (KeyNotFoundException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Inventory/Warehouses/Edit
        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateWarehouseCommand command)
        {
            if (!ModelState.IsValid)
                return View(command);

            try
            {
                var result = await _warehouseCases.UpdateWarehouseAsync(command);
                TempData["Success"] = Loc.F("Msg_WarehouseUpdated", result.Name);
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            catch (KeyNotFoundException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(command);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(command);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(command);
            }
        }

        // POST: /Inventory/Warehouses/Activate/{id}
        [HttpPost("Activate/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(Guid id)
        {
            try
            {
                var result = await _warehouseCases.ActivateWarehouseAsync(new ActivateWarehouseCommand(id));
                TempData["Success"] = Loc.F("Msg_WarehouseActivated", result.Name);
            }
            catch (Exception ex) when (ex is KeyNotFoundException || ex is ArgumentException || ex is InvalidOperationException)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Inventory/Warehouses/Deactivate/{id}
        [HttpPost("Deactivate/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            try
            {
                var result = await _warehouseCases.DeactivateWarehouseAsync(new DeactivateWarehouseCommand(id));
                TempData["Success"] = Loc.F("Msg_WarehouseDeactivated", result.Name);
            }
            catch (Exception ex) when (ex is KeyNotFoundException || ex is ArgumentException || ex is InvalidOperationException)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Inventory/Warehouses/SearchByName?name=...
        [HttpGet("SearchByName")]
        public async Task<IActionResult> SearchByName(string? name)
        {
            ViewBag.Title = Loc.T("Warehouses_SearchTitle");
            if (string.IsNullOrWhiteSpace(name))
                return View(Enumerable.Empty<WarehouseDto>());

            try
            {
                var warehouses = await _warehouseCases.RetrieveWarehousesByNameAsync(new RetrieveWarehousesByNameQuery(name));
                return View(warehouses);
            }
            catch (ArgumentException ex)
            {
                ViewBag.Error = ex.Message;
                return View(Enumerable.Empty<WarehouseDto>());
            }
        }
    }
}
