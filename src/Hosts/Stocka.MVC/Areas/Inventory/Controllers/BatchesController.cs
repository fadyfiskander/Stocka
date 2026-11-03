using Inventory.Contracts.DTOs.Batch;
using Inventory.Contracts.UseCases.BatchCases;
using Inventory.Contracts.UseCases.WarehouseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Stocka.MVC.Areas.Inventory.Controllers
{
    [Area("Inventory")]
    [Route("[area]/[controller]")]
    [Authorize(Roles = "Owner")]
    public class BatchesController : Controller
    {
        private readonly IBatchCases _batchCases;
        private readonly IWarehouseCases _warehouseCases;

        public BatchesController(IBatchCases batchCases, IWarehouseCases warehouseCases)
        {
            _batchCases = batchCases;
            _warehouseCases = warehouseCases;
        }

        // GET: /Inventory/Batches?productId=&purchaseItemId=&warehouseId=
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(Guid? productId, Guid? purchaseItemId, Guid? warehouseId, int page = 1, int pageSize = 20)
        {
            ViewBag.Title = "Batches";
            ViewBag.ProductId = productId;
            ViewBag.PurchaseItemId = purchaseItemId;
            ViewBag.WarehouseId = warehouseId;

            var query = new ListBatchesQuery
            {
                ProductId = productId,
                PurchaseItemId = purchaseItemId,
                WarehouseId = warehouseId,
                Page = page,
                PageSize = pageSize
            };
            var batches = await _batchCases.ListBatchesAsync(query);
            return View(batches);
        }

        // GET: /Inventory/Batches/Create
        [HttpGet("Create")]
        public IActionResult Create()
        {
            ViewBag.Title = "Register Batch";
            return View(new RegisterBatchCommand());
        }

        // POST: /Inventory/Batches/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterBatchCommand command)
        {
            if (!ModelState.IsValid)
                return View(command);

            try
            {
                var result = await _batchCases.RegisterBatchAsync(command);
                TempData["Success"] = "Batch registered. Allocate it to a warehouse to make stock available.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(command);
            }
        }

        // GET: /Inventory/Batches/Details/{id}
        [HttpGet("Details/{id:guid}")]
        public async Task<IActionResult> Details(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["Error"] = "Batch ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                ViewBag.Title = "Batch Details";
                var batch = await _batchCases.RetrieveBatchAsync(new RetrieveBatchQuery(id));
                var movements = await _batchCases.RetrieveBatchMovementHistoryAsync(new RetrieveBatchMovementHistoryQuery(id));
                var activeWarehouses = await _warehouseCases.ListWarehousesAsync(new ListWarehousesQuery { IsActive = true, Page = 1, PageSize = 100 });
                ViewBag.Movements = movements;
                ViewBag.ActiveWarehouses = activeWarehouses;
                return View(batch);
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

        // GET: /Inventory/Batches/Unallocated?page=
        [HttpGet("Unallocated")]
        public async Task<IActionResult> Unallocated(int page = 1, int pageSize = 20)
        {
            ViewBag.Title = "Unallocated Batches";
            var batches = await _batchCases.ListUnallocatedBatchesAsync(new ListUnallocatedBatchesQuery(page, pageSize));
            return View("Index", batches);
        }
    }
}
