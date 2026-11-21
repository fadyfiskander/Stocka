using BuildingBlocks.Application.Abstractions;
using Inventory.Application.Bindings.StockCases;
using Inventory.Application.Repositories.Commands;
using Inventory.Contracts.DTOs.Stock;
using Inventory.Contracts.UseCases.StockCases;
using Inventory.Domain.Batches;
using Inventory.Domain.StockMovements;
using Inventory.Domain.Warehouses;
using MediatR;

namespace Inventory.Application.UseCases.StockCases;

#region Handler
/// <summary>
/// Handles the physical-count correction: loads the batch with an organization guard, computes the delta
/// between the counted quantity and the current placement quantity, and records a single
/// adjustment movement in the derived direction; recounting the same value is a verified no-op
/// that creates no movement.
/// </summary>
public class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommandMediatR, AdjustmentResultDto>
{
    private readonly IBatchRepository _batchRepository;
    private readonly IWarehouseBatchRepository _warehouseBatchRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly ICurrentUserService _currentUser;

    public AdjustStockCommandHandler(
        IBatchRepository batchRepository,
        IWarehouseBatchRepository warehouseBatchRepository,
        IStockMovementRepository stockMovementRepository,
        ICurrentUserService currentUser)
    {
        _batchRepository = batchRepository;
        _warehouseBatchRepository = warehouseBatchRepository;
        _stockMovementRepository = stockMovementRepository;
        _currentUser = currentUser;
    }

    public async Task<AdjustmentResultDto> Handle(AdjustStockCommandMediatR request, CancellationToken cancellationToken)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            throw new ArgumentNullException("OrganizationId is null, OrganizationId cannot be null");

        var batch = await _batchRepository.GetByIdAsync(BatchId.From(request.Contract.BatchId))
            ?? throw new KeyNotFoundException(StockOperationMessages.BatchNotFound);
        if (batch.OrganizationId != organizationId) throw new KeyNotFoundException(StockOperationMessages.BatchNotFound);

        var warehouseBatch = await _warehouseBatchRepository.FindByWarehouseAndBatchAsync(
            WarehouseId.From(request.Contract.WarehouseId), batch.Id)
            ?? throw new InvalidOperationException(StockOperationMessages.BatchNotInWarehouse);

        var previousQuantity = warehouseBatch.Quantity;
        var delta = request.Contract.CountedQuantity - previousQuantity;

        // Idempotency: recounting the same value is a verified no-op — no duplicate movement is created.
        if (delta == 0)
            return new AdjustmentResultDto(batch.Id.Value, warehouseBatch.WarehouseId.Value, request.Contract.CountedQuantity, previousQuantity, 0, null);

        warehouseBatch.UpdateQuantity(request.Contract.CountedQuantity);

        StockMovement movement;
        if (delta > 0)
        {
            movement = new StockMovement(
                batch.ProductId, batch.Id, warehouseBatch.WarehouseId, organizationId.Value, delta,
                StockMovementType.AdjustmentIn, StockReferenceType.Adjustment, request.Contract.ReferenceId, request.Contract.Reason);
        }
        else
        {
            movement = new StockMovement(
                batch.ProductId, batch.Id, warehouseBatch.WarehouseId, organizationId.Value, -delta,
                StockMovementType.AdjustmentOut, StockReferenceType.Adjustment, request.Contract.ReferenceId, request.Contract.Reason);
        }

        await _warehouseBatchRepository.UpdateAsync(warehouseBatch);
        await _stockMovementRepository.AddAsync(movement);

        return new AdjustmentResultDto(
            batch.Id.Value,
            warehouseBatch.WarehouseId.Value,
            request.Contract.CountedQuantity,
            previousQuantity,
            delta,
            movement.Id.Value);
    }
}
#endregion
