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
            ViewBag.Title = (Loc.IsEn ? "Stock movements" : "حركات المخزون");
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
                TempData["Success"] = (Loc.IsEn ? $"Allocated. Batch now holds {result.QuantityInWarehouse} units in this warehouse ({result.BatchRemainingQuantity} units remain unallocated)." : $"تم التخصيص. تحتوي الدفعة الآن على {result.QuantityInWarehouse} وحدة في هذا المستودع ({result.BatchRemainingQuantity} وحدة متبقية غير مخصَّصة).");
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
                TempData["Success"] = (Loc.IsEn ? $"Sale recorded ({movement.Quantity} units out)." : $"تم تسجيل البيع ({movement.Quantity} وحدة خارجة).");
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
                TempData["Success"] = (Loc.IsEn ? $"Transfer {result.TransferReferenceId} completed: {result.QuantityInDestination} units now in destination, {result.QuantityInSource} remain in source." : $"اكتمل التحويل {result.TransferReferenceId}: {result.QuantityInDestination} وحدة الآن في الوجهة، {result.QuantityInSource} وحدة متبقية في المصدر.");
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
                    ? (Loc.IsEn ? "Physical count matches recorded quantity — no adjustment recorded." : "الجرد الفعلي يطابق الكمية المسجّلة — لم تُسجَّل أي تسوية.")
                    : (Loc.IsEn ? $"Adjustment recorded ({(result.QuantityChange > 0 ? "+" : string.Empty) + result.QuantityChange})." : $"تم تسجيل التسوية ({(result.QuantityChange > 0 ? "+" : string.Empty) + result.QuantityChange}).");
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
                TempData["Success"] = (Loc.IsEn ? $"Damage write-off recorded ({movement.Quantity} units out). History preserved." : $"تم تسجيل شطب التلف ({movement.Quantity} وحدة خارجة). حُفظ السجل.");
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
                TempData["Success"] = (Loc.IsEn ? $"Customer return recorded ({movement.Quantity} units in)." : $"تم تسجيل إرجاع العميل ({movement.Quantity} وحدة داخلة).");
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
                TempData["Success"] = (Loc.IsEn ? $"Correction movement {reversal.Id} appended (original movement {id} not modified)." : $"أُضيفت حركة التصحيح {reversal.Id} (لم تُعدَّل الحركة الأصلية {id}).");
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or ArgumentException)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(BatchesController.Details), "Batches", new { id = batchId });
        }
    }
}
