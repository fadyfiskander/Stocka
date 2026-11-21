using BuildingBlocks.Application.Abstractions;
using Inventory.Application.Bindings.StockCases;
using Inventory.Application.Repositories.Commands;
using Inventory.Contracts.DTOs.StockMovement;
using Inventory.Contracts.UseCases.StockCases;
using Inventory.Domain.Batches;
using Inventory.Domain.StockMovements;
using Inventory.Domain.Warehouses;
using MediatR;

namespace Inventory.Application.UseCases.StockCases;

#region Handler
/// <summary>
/// Handles the customer return: loads the batch with an organization guard, adds the units back into the
/// same batch/warehouse placement, and records the OrderReturn movement with the Return
/// reference — allocation bookkeeping (RemainingQuantity) is deliberately left unchanged.
/// </summary>
public class RecordCustomerReturnCommandHandler : IRequestHandler<RecordCustomerReturnCommandMediatR, StockMovementDto>
{
    private readonly IBatchRepository _batchRepository;
    private readonly IWarehouseBatchRepository _warehouseBatchRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly ICurrentUserService _currentUser;

    public RecordCustomerReturnCommandHandler(
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

    public async Task<StockMovementDto> Handle(RecordCustomerReturnCommandMediatR request, CancellationToken cancellationToken)
    {
        var organizationId = _currentUser.OrganizationId;

        var batch = await _batchRepository.GetByIdAsync(BatchId.From(request.Contract.BatchId))
            ?? throw new KeyNotFoundException(StockOperationMessages.BatchNotFound);
        if (batch.OrganizationId != organizationId) throw new KeyNotFoundException(StockOperationMessages.BatchNotFound);

        var warehouseBatch = await _warehouseBatchRepository.FindByWarehouseAndBatchAsync(
            WarehouseId.From(request.Contract.WarehouseId), batch.Id)
            ?? throw new InvalidOperationException(StockOperationMessages.BatchNotInWarehouse);

        warehouseBatch.AddQuantity(request.Contract.Quantity);

        var movement = new StockMovement(
            batch.ProductId,
            batch.Id,
            warehouseBatch.WarehouseId,
            organizationId.Value,
            request.Contract.Quantity,
            StockMovementType.OrderReturn,
            StockReferenceType.Return,
            request.Contract.OrderReferenceId,
            request.Contract.Reason);

        await _warehouseBatchRepository.UpdateAsync(warehouseBatch);
        await _stockMovementRepository.AddAsync(movement);

        return RecordSaleOutCommandHandler.ToDto(movement);
    }
}
#endregion
