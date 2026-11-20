using BuildingBlocks.Application.Abstractions;
using Inventory.Application.Bindings.BatchCases;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.Batch;
using Inventory.Contracts.UseCases.BatchCases;
using Inventory.Primitives.Identifiers;
using MediatR;

namespace Inventory.Application.UseCases.BatchCases;

#region Handler
/// <summary>
/// Handles the batch retrieval: loads the batch scoped to the current Organization, aggregates its
/// quantity currently placed in warehouses, and projects the full details including valuation
/// figures (initial/unallocated/in-warehouses quantities and their costs).
/// </summary>
public class RetrieveBatchQueryHandler : IRequestHandler<RetrieveBatchQueryMediatR, BatchDetailsDto>
{
    private readonly IBatchQueryRepository _batchQueryRepository;
    private readonly IWarehouseBatchQueryRepository _warehouseBatchQueryRepository;
    private readonly ICurrentUserService _currentUser;

    public RetrieveBatchQueryHandler(
        IBatchQueryRepository batchQueryRepository,
        IWarehouseBatchQueryRepository warehouseBatchQueryRepository,
        ICurrentUserService currentUser)
    {
        _batchQueryRepository = batchQueryRepository;
        _warehouseBatchQueryRepository = warehouseBatchQueryRepository;
        _currentUser = currentUser;
    }

    public async Task<BatchDetailsDto> Handle(RetrieveBatchQueryMediatR request, CancellationToken cancellationToken)
    {
        if (request.Contract.BatchId == Guid.Empty)
            throw new ArgumentException("Batch ID cannot be empty.", nameof(request.Contract.BatchId));

        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            throw new ArgumentNullException("OrganizationId is null, OrganizationId cannot be null");

        var batch = await _batchQueryRepository.GetByIdAsync(organizationId.Value, BatchId.From(request.Contract.BatchId))
            ?? throw new KeyNotFoundException($"Batch '{request.Contract.BatchId}' was not found.");

        var quantityInWarehouses = await _warehouseBatchQueryRepository.GetTotalQuantityInWarehousesAsync(
            organizationId.Value, BatchId.From(batch.Id));

        return new BatchDetailsDto(
            batch.Id,
            batch.ProductId,
            batch.PurchaseItemId,
            organizationId.Value,
            batch.InitialQuantity,
            batch.RemainingQuantity,
            batch.InitialQuantity - batch.RemainingQuantity,
            quantityInWarehouses,
            batch.RemainingQuantity == 0,
            batch.UnitCost,
            batch.InitialQuantity * batch.UnitCost,
            batch.RemainingQuantity * batch.UnitCost,
            batch.CreatedAt);
    }
}
#endregion
