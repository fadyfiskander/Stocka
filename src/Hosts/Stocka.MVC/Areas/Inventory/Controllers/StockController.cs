using Inventory.Contracts.DTOs.StockMovement;
using Inventory.Contracts.UseCases.StockCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stocka.MVC.Localization;

namespace Stocka.MVC.Areas.Inventory.Controllers
{
    /// <summary>
    /// Stock-changing operations and movement inspection. All state changes are POST with
    /// antiforgery; controllers stay thin — ALL rules live in the Application handlers.
    /// </summary>
    [Area("Inventory")]
    [Route("[area]/[controller]")]
    [Authorize(Roles = "Owner")]
    public class StockController : Controller
    {
        private readonly IStockCases _stockCases;

        public StockController(IStockCases stockCases)
        {
            _stockCases = stockCases;
        }

        // GET: /Inventory/Stock/Movements?warehouseId=&batchId=&productId=&from=&to=&page=
        [HttpGet("Movements")]
        public async Task<IActionResult> Movements(
            Guid? warehouseId, Guid? batchId, Guid? productId, DateTime? from, DateTime? to, int?[]? types,
            int page = 1, int pageSize = 20)
        {
            ViewBag.Title = Loc.T("StockMovements_Title");
            ViewBag.WarehouseId = warehouseId;
            ViewBag.BatchId = batchId;
            ViewBag.ProductId = productId;
            ViewBag.From = from;
            ViewBag.To = to;
            ViewBag.Types = types;

            // Selective, documented movement-type picker: empty => all types.
            var movementTypes = (types ?? Array.Empty<int?>()).Where(t => t.HasValue).Select(t => t!.Value).ToList();

            var result = await _stockCases.ListMovementsAsync(new ListMovementsQuery
            {
                WarehouseId = warehouseId,
                BatchId = batchId,
                ProductId = productId,
                MovementTypes = movementTypes.Count > 0 ? movementTypes : null,
                DateFromUtc = from,
                DateToUtc = to,
                Page = page,
                PageSize = pageSize
            });

            return View(result);
        }

        // POST: /Inventory/Stock/Allocate  (batch units -> warehouse placement + PurchaseIn movement)
        [HttpPost("Allocate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Allocate(AllocateStockCommand command)
        {
            try
            {
                var result = await _stockCases.AllocateStockAsync(command);
                TempData["Success"] = Loc.F("Msg_Allocated", result.QuantityInWarehouse, result.BatchRemainingQuantity);
            }
            catch (KeyNotFoundException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(BatchesController.Details), "Batches", new { id = command.BatchId });
        }

        // POST: /Inventory/Stock/Sale  (SaleOut movement, Order reference)
        [HttpPost("Sale")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sale(RecordSaleOutCommand command)
        {
            try
            {
                var movement = await _stockCases.RecordSaleOutAsync(command);
                TempData["Success"] = Loc.F("Msg_SaleRecorded", movement.Quantity);
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or ArgumentException)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(BatchesController.Details), "Batches", new { id = command.BatchId });
        }

        // POST: /Inventory/Stock/Transfer  (TransferOut + TransferIn, one atomic business operation)
        [HttpPost("Transfer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(TransferStockCommand command)
        {
            try
            {
                var result = await _stockCases.TransferStockAsync(command);
                TempData["Success"] = Loc.F("Msg_TransferCompleted", result.TransferReferenceId, result.QuantityInDestination, result.QuantityInSource);
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or ArgumentException)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(BatchesController.Details), "Batches", new { id = command.BatchId });
        }

        // POST: /Inventory/Stock/Adjust  (physical count correction)
        [HttpPost("Adjust")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjust(AdjustStockCommand command)
        {
            try
            {
                var result = await _stockCases.AdjustStockAsync(command);
                TempData["Success"] = result.QuantityChange == 0
                    ? Loc.T("Msg_AdjustNoChange")
                    : Loc.F("Msg_AdjustRecorded", (result.QuantityChange > 0 ? "+" : string.Empty) + result.QuantityChange);
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or ArgumentException)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(BatchesController.Details), "Batches", new { id = command.BatchId });
        }

        // POST: /Inventory/Stock/Damage  (damaged stock write-off)
        [HttpPost("Damage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Damage(WriteOffDamagedStockCommand command)
        {
            try
            {
                var movement = await _stockCases.WriteOffDamagedStockAsync(command);
                TempData["Success"] = Loc.F("Msg_DamageRecorded", movement.Quantity);
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or ArgumentException)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(BatchesController.Details), "Batches", new { id = command.BatchId });
        }

        // POST: /Inventory/Stock/Return  (customer return into warehouse)
        [HttpPost("Return")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(RecordCustomerReturnCommand command)
        {
            try
            {
                var movement = await _stockCases.RecordCustomerReturnAsync(command);
                TempData["Success"] = Loc.F("Msg_ReturnRecorded", movement.Quantity);
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or ArgumentException)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(BatchesController.Details), "Batches", new { id = command.BatchId });
        }

        // POST: /Inventory/Stock/ReverseMovement/{id}
        [HttpPost("ReverseMovement/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReverseMovement(Guid id, string reason, Guid batchId)
        {
            try
            {
                var reversal = await _stockCases.ReverseStockMovementAsync(new ReverseStockMovementCommand { MovementId = id, Reason = reason });
                TempData["Success"] = Loc.F("Msg_ReversalAppended", reversal.Id, id);
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or ArgumentException)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(BatchesController.Details), "Batches", new { id = batchId });
        }
    }
}
