using BuildingBlocks.Application.Abstractions;
using Inventory.Application.Bindings.StockCases;
using Inventory.Application.Repositories.Commands;
using Inventory.Contracts.DTOs.StockMovement;
using Inventory.Contracts.UseCases.StockCases;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Primitives.Identifiers;
using MediatR;

namespace Inventory.Application.UseCases.StockCases;

#region Handler
/// <summary>
/// Handles the reversal: appends the opposite-direction movement referencing the original,
/// rebalances the current warehouse stock in the corrected direction, and — for PurchaseIn
/// reversals — frees the units back into the batch's unallocated quantity.
/// </summary>
public class ReverseStockMovementCommandHandler : IRequestHandler<ReverseStockMovementCommandMediatR, StockMovementDto>
{
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IBatchRepository _batchRepository;
    private readonly IWarehouseBatchRepository _warehouseBatchRepository;
    private readonly ICurrentUserService _currentUser;

    public ReverseStockMovementCommandHandler(
        IStockMovementRepository stockMovementRepository,
        IBatchRepository batchRepository,
        IWarehouseBatchRepository warehouseBatchRepository,
        ICurrentUserService currentUser)
    {
        _stockMovementRepository = stockMovementRepository;
        _batchRepository = batchRepository;
        _warehouseBatchRepository = warehouseBatchRepository;
        _currentUser = currentUser;
    }

    public async Task<StockMovementDto> Handle(ReverseStockMovementCommandMediatR request, CancellationToken cancellationToken)
    {
        var organizationId = _currentUser.OrganizationId;

        var original = await _stockMovementRepository.GetByIdAsync(StockMovementId.From(request.Contract.MovementId))
            ?? throw new KeyNotFoundException("Stock movement not found.");
        if (original.OrganizationId != organizationId) throw new KeyNotFoundException("Stock movement not found.");

        if (original.MovementType is StockMovementType.TransferIn or StockMovementType.TransferOut)
            throw new InvalidOperationException(
                "Transfer legs cannot be reversed individually; record a counter-transfer instead (transfer pairs must stay consistent).");

        var reversalType = original.IsOutbound ? StockMovementType.AdjustmentIn : StockMovementType.AdjustmentOut;
        var referenceType = StockReferenceType.ReversalOfMovement;
        var referenceId = original.Id.Value;
        var reason = $"Reversal of movement {original.Id.Value}: {request.Contract.Reason!.Trim()}";

        var reversal = new StockMovement(
            original.ProductId,
            original.BatchId,
            original.WarehouseId,
            organizationId.Value,
            original.Quantity,
            reversalType,
            referenceType,
            referenceId,
            reason);

        // Balance the current warehouse stock against the corrected movement.
        var warehouseBatch = await _warehouseBatchRepository.FindByWarehouseAndBatchAsync(original.WarehouseId, original.BatchId)
            ?? throw new InvalidOperationException(StockOperationMessages.BatchNotInWarehouse);

        if (original.IsOutbound)
        {
            // Undo an outbound removal: units come back into current stock.
            warehouseBatch.AddQuantity(original.Quantity);
        }
        else
        {
            // Undo an inbound addition: units leave current stock.
            warehouseBatch.DeductQuantity(original.Quantity);
        }

        await _warehouseBatchRepository.UpdateAsync(warehouseBatch);

        if (original.MovementType == StockMovementType.PurchaseIn)
        {
            // An allocation reversal frees the units back into unallocated stock.
            var batch = await _batchRepository.GetByIdAsync(original.BatchId)
                ?? throw new KeyNotFoundException(StockOperationMessages.BatchNotFound);
            batch.AddQuantity(original.Quantity);
            await _batchRepository.UpdateAsync(batch);
        }

        await _stockMovementRepository.AddAsync(reversal);

        return RecordSaleOutCommandHandler.ToDto(reversal);
    }
}
#endregion
