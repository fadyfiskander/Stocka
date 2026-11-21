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
/// Handles the allocation: validates the batch's unallocated quantity and the warehouse's Organization/activity,
/// creates or increases the WarehouseBatch placement, deducts the batch's RemainingQuantity, and
/// records the PurchaseIn movement — all persisted by one SaveChanges via UnitOfWorkBehavior.
/// </summary>
public class AllocateStockCommandHandler : IRequestHandler<AllocateStockCommandMediatR, AllocateStockResultDto>
{
    private readonly IBatchRepository _batchRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IWarehouseBatchRepository _warehouseBatchRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly ICurrentUserService _currentUser;

    public AllocateStockCommandHandler(
        IBatchRepository batchRepository,
        IWarehouseRepository warehouseRepository,
        IWarehouseBatchRepository warehouseBatchRepository,
        IStockMovementRepository stockMovementRepository,
        ICurrentUserService currentUser)
    {
        _batchRepository = batchRepository;
        _warehouseRepository = warehouseRepository;
        _warehouseBatchRepository = warehouseBatchRepository;
        _stockMovementRepository = stockMovementRepository;
        _currentUser = currentUser;
    }

    public async Task<AllocateStockResultDto> Handle(AllocateStockCommandMediatR request, CancellationToken cancellationToken)
    {
        var organizationId = _currentUser.OrganizationId;

        var batch = await _batchRepository.GetByIdAsync(BatchId.From(request.Contract.BatchId))
            ?? throw new KeyNotFoundException(StockOperationMessages.BatchNotFound);
        if (batch.OrganizationId != organizationId) throw new KeyNotFoundException(StockOperationMessages.BatchNotFound);

        if (request.Contract.Quantity > batch.RemainingQuantity)
            throw new InvalidOperationException(
                $"Cannot allocate {request.Contract.Quantity} units: batch has only {batch.RemainingQuantity} unallocated units left.");

        var warehouse = await _warehouseRepository.GetByIdAsync(WarehouseId.From(request.Contract.WarehouseId))
            ?? throw new KeyNotFoundException(StockOperationMessages.WarehouseNotFound);
        if (warehouse.OrganizationId != organizationId) throw new KeyNotFoundException(StockOperationMessages.WarehouseNotFound);
        if (!warehouse.IsActive) throw new InvalidOperationException(StockOperationMessages.WarehouseInactive);

        var existingWarehouseBatch = await _warehouseBatchRepository.FindByWarehouseAndBatchAsync(warehouse.Id, batch.Id);
        WarehouseBatch warehouseBatch;
        if (existingWarehouseBatch == null)
        {
            warehouseBatch = new WarehouseBatch(warehouse.Id, batch.Id, organizationId.Value, request.Contract.Quantity);
            await _warehouseBatchRepository.AddAsync(warehouseBatch);
        }
        else
        {
            warehouseBatch = existingWarehouseBatch;
            warehouseBatch.AddQuantity(request.Contract.Quantity);
            await _warehouseBatchRepository.UpdateAsync(warehouseBatch);
        }

        batch.DeductQuantity(request.Contract.Quantity);

        await _batchRepository.UpdateAsync(batch);
        await _stockMovementRepository.AddAsync(new StockMovement(
            batch.ProductId,
            batch.Id,
            warehouse.Id,
            organizationId.Value,
            request.Contract.Quantity,
            StockMovementType.PurchaseIn,
            StockReferenceType.Purchase,
            batch.PurchaseItemId));

        return new AllocateStockResultDto(
            batch.Id.Value,
            warehouse.Id.Value,
            warehouseBatch.Quantity,
            batch.RemainingQuantity);
    }
}
#endregion
