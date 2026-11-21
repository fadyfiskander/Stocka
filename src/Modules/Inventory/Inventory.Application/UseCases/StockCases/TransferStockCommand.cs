using BuildingBlocks.Application.Abstractions;
using Inventory.Application.Bindings.StockCases;
using Inventory.Application.Repositories.Commands;
using Inventory.Contracts.DTOs.Stock;
using Inventory.Contracts.UseCases.StockCases;
using Inventory.Domain.Batches;
using Inventory.Domain.Entities;
using Inventory.Primitives.Identifiers;
using Inventory.Domain.Enums;
using MediatR;

namespace Inventory.Application.UseCases.StockCases;

#region Handler
/// <summary>
/// Handles the transfer: validates source ≠ destination, Organization ownership of batch and both
/// warehouses, and destination activity; deducts from the source placement, creates or increases
/// the destination placement, and records the TransferOut/TransferIn pair under one shared
/// transfer reference — all persisted by one SaveChanges via UnitOfWorkBehavior.
/// </summary>
public class TransferStockCommandHandler : IRequestHandler<TransferStockCommandMediatR, TransferStockResultDto>
{
    private readonly IBatchRepository _batchRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IWarehouseBatchRepository _warehouseBatchRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly ICurrentUserService _currentUser;

    public TransferStockCommandHandler(
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

    public async Task<TransferStockResultDto> Handle(TransferStockCommandMediatR request, CancellationToken cancellationToken)
    {
        if (request.Contract.SourceWarehouseId == request.Contract.DestinationWarehouseId)
            throw new InvalidOperationException(StockOperationMessages.SourceDestinationSame);

        var organizationId = _currentUser.OrganizationId;

        var batch = await _batchRepository.GetByIdAsync(BatchId.From(request.Contract.BatchId))
            ?? throw new KeyNotFoundException(StockOperationMessages.BatchNotFound);
        if (batch.OrganizationId != organizationId) throw new KeyNotFoundException(StockOperationMessages.BatchNotFound);

        var sourceWarehouse = await _warehouseRepository.GetByIdAsync(WarehouseId.From(request.Contract.SourceWarehouseId))
            ?? throw new KeyNotFoundException(StockOperationMessages.WarehouseNotFound);
        if (sourceWarehouse.OrganizationId != organizationId) throw new KeyNotFoundException(StockOperationMessages.WarehouseNotFound);

        var destinationWarehouse = await _warehouseRepository.GetByIdAsync(WarehouseId.From(request.Contract.DestinationWarehouseId))
            ?? throw new KeyNotFoundException(StockOperationMessages.WarehouseNotFound);
        if (destinationWarehouse.OrganizationId != organizationId) throw new KeyNotFoundException(StockOperationMessages.WarehouseNotFound);
        if (!destinationWarehouse.IsActive) throw new InvalidOperationException(StockOperationMessages.WarehouseInactive);

        var sourceWarehouseBatch = await _warehouseBatchRepository.FindByWarehouseAndBatchAsync(sourceWarehouse.Id, batch.Id)
            ?? throw new InvalidOperationException(StockOperationMessages.BatchNotInWarehouse);

        sourceWarehouseBatch.DeductQuantity(request.Contract.Quantity);

        var existingDestinationWarehouseBatch = await _warehouseBatchRepository.FindByWarehouseAndBatchAsync(destinationWarehouse.Id, batch.Id);
        WarehouseBatch destinationWarehouseBatch;
        if (existingDestinationWarehouseBatch == null)
        {
            destinationWarehouseBatch = new WarehouseBatch(destinationWarehouse.Id, batch.Id, organizationId.Value, request.Contract.Quantity);
            await _warehouseBatchRepository.AddAsync(destinationWarehouseBatch);
        }
        else
        {
            destinationWarehouseBatch = existingDestinationWarehouseBatch;
            destinationWarehouseBatch.AddQuantity(request.Contract.Quantity);
            await _warehouseBatchRepository.UpdateAsync(destinationWarehouseBatch);
        }

        // One transfer reference ties the pair together for reporting; both legs share it.
        var transferReferenceId = Guid.NewGuid();

        await _warehouseBatchRepository.UpdateAsync(sourceWarehouseBatch);
        await _stockMovementRepository.AddAsync(new StockMovement(
            batch.ProductId,
            batch.Id,
            sourceWarehouse.Id,
            organizationId.Value,
            request.Contract.Quantity,
            StockMovementType.TransferOut,
            StockReferenceType.Transfer,
            transferReferenceId,
            request.Contract.Reason));
        await _stockMovementRepository.AddAsync(new StockMovement(
            batch.ProductId,
            batch.Id,
            destinationWarehouse.Id,
            organizationId.Value,
            request.Contract.Quantity,
            StockMovementType.TransferIn,
            StockReferenceType.Transfer,
            transferReferenceId,
            request.Contract.Reason));

        return new TransferStockResultDto(
            transferReferenceId,
            batch.ProductId,
            batch.Id.Value,
            sourceWarehouse.Id.Value,
            destinationWarehouse.Id.Value,
            sourceWarehouseBatch.Quantity,
            destinationWarehouseBatch.Quantity);
    }
}
#endregion
