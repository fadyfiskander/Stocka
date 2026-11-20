using BuildingBlocks.Application.Abstractions;
using Inventory.Application.Bindings.BatchCases;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.StockMovement;
using Inventory.Contracts.UseCases.BatchCases;
using Inventory.Primitives.Identifiers;
using MediatR;

namespace Inventory.Application.UseCases.BatchCases;

#region Handler
/// <summary>
/// Handles the batch history retrieval: returns the batch's full chronological movement history
/// scoped to the current Organization (cross-Organization IDs are indistinguishable from not-found).
/// </summary>
public class RetrieveBatchMovementHistoryQueryHandler : IRequestHandler<RetrieveBatchMovementHistoryQueryMediatR, IEnumerable<StockMovementDto>>
{
    private readonly IStockMovementQueryRepository _stockMovementQueryRepository;
    private readonly ICurrentUserService _currentUser;

    public RetrieveBatchMovementHistoryQueryHandler(IStockMovementQueryRepository stockMovementQueryRepository, ICurrentUserService currentUser)
    {
        _stockMovementQueryRepository = stockMovementQueryRepository;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<StockMovementDto>> Handle(RetrieveBatchMovementHistoryQueryMediatR request, CancellationToken cancellationToken)
    {
        if (request.Contract.BatchId == Guid.Empty)
            throw new ArgumentException("Batch ID cannot be empty.", nameof(request.Contract.BatchId));

        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            throw new ArgumentNullException("OrganizationId is null, OrganizationId cannot be null");

        // Existence check scoped to the current Organization (cross-Organization => not found).
        return await _stockMovementQueryRepository.GetByBatchAsync(organizationId.Value, BatchId.From(request.Contract.BatchId));
    }
}
#endregion
