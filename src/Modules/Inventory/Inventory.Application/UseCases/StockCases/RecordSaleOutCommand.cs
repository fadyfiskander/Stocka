using BuildingBlocks.Application.Abstractions;
using Inventory.Application.Bindings.StockCases;
using Inventory.Application.Repositories.Commands;
using Inventory.Contracts.DTOs.Stock;
using Inventory.Contracts.DTOs.StockMovement;
using Inventory.Contracts.UseCases.StockCases;
using Inventory.Domain.Batches;
using Inventory.Domain.StockMovements;
using Inventory.Domain.Warehouses;
using MediatR;

namespace Inventory.Application.UseCases.StockCases;

#region Handler
/// <summary>
/// Handles the sale issue: loads the batch with an organization guard, deducts the units from the
/// warehouse placement, and records the SaleOut movement with the Order reference. Also hosts
/// the shared movement-to-DTO projector used by the other stock-operation handlers.
/// </summary>
public class RecordSaleOutCommandHandler : IRequestHandler<RecordSaleOutCommandMediatR, StockMovementDto>
{
    private readonly IBatchRepository _batchRepository;
    private readonly IWarehouseBatchRepository _warehouseBatchRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly ICurrentUserService _currentUser;

    public RecordSaleOutCommandHandler(
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

    public async Task<StockMovementDto> Handle(RecordSaleOutCommandMediatR request, CancellationToken cancellationToken)
    {
        var organizationId = _currentUser.OrganizationId;

        var batch = await _batchRepository.GetByIdAsync(BatchId.From(request.Contract.BatchId))
            ?? throw new KeyNotFoundException(StockOperationMessages.BatchNotFound);
        if (batch.OrganizationId != organizationId) throw new KeyNotFoundException(StockOperationMessages.BatchNotFound);

        var warehouseBatch = await _warehouseBatchRepository.FindByWarehouseAndBatchAsync(
            WarehouseId.From(request.Contract.WarehouseId), batch.Id)
            ?? throw new InvalidOperationException(StockOperationMessages.BatchNotInWarehouse);

        warehouseBatch.DeductQuantity(request.Contract.Quantity);

        var movement = new StockMovement(
            batch.ProductId,
            batch.Id,
            warehouseBatch.WarehouseId,
            organizationId.Value,
            request.Contract.Quantity,
            StockMovementType.SaleOut,
            StockReferenceType.Order,
            request.Contract.OrderReferenceId,
            request.Contract.Reason);

        await _warehouseBatchRepository.UpdateAsync(warehouseBatch);
        await _stockMovementRepository.AddAsync(movement);

        return ToDto(movement);
    }

    internal static StockMovementDto ToDto(StockMovement movement) =>
        new(
            movement.Id.Value,
            movement.ProductId,
            movement.BatchId.Value,
            movement.WarehouseId.Value,
            movement.OrganizationId,
            movement.Quantity,
            (int)movement.MovementType,
            movement.ReferenceType.HasValue ? (int)movement.ReferenceType.Value : null,
            movement.ReferenceId,
            movement.CreatedAt,
            movement.Reason);
}
#endregion
