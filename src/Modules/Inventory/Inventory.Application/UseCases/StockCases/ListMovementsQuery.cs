using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Models;
using Inventory.Application.Bindings.StockCases;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.StockMovement;
using Inventory.Contracts.UseCases.StockCases;
using Inventory.Domain.Batches;
using Inventory.Domain.Warehouses;
using MediatR;

namespace Inventory.Application.UseCases.StockCases;

#region Handler
/// <summary>
/// Handles the movement history listing: maps the request filters onto
/// MovementQueryCriteria (dropping empty Guid filters) and delegates to the Dapper-backed paged
/// query scoped to the current Organization.
/// </summary>
public class ListMovementsQueryHandler : IRequestHandler<ListMovementsQueryMediatR, PaginatedResult<StockMovementDto>>
{
    private readonly IStockMovementQueryRepository _stockMovementQueryRepository;
    private readonly ICurrentUserService _currentUser;

    public ListMovementsQueryHandler(IStockMovementQueryRepository stockMovementQueryRepository, ICurrentUserService currentUser)
    {
        _stockMovementQueryRepository = stockMovementQueryRepository;
        _currentUser = currentUser;
    }

    public async Task<PaginatedResult<StockMovementDto>> Handle(ListMovementsQueryMediatR request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Contract.Page);
        var pageSize = Math.Clamp(request.Contract.PageSize, 1, 100);

        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            throw new ArgumentNullException("OrganizationId is null, OrganizationId cannot be null");

        var criteria = new MovementQueryCriteria
        {
            WarehouseId = request.Contract.WarehouseId.HasValue && request.Contract.WarehouseId.Value != Guid.Empty
                ? WarehouseId.From(request.Contract.WarehouseId.Value) : null,
            BatchId = request.Contract.BatchId.HasValue && request.Contract.BatchId.Value != Guid.Empty
                ? BatchId.From(request.Contract.BatchId.Value) : null,
            ProductId = request.Contract.ProductId.HasValue && request.Contract.ProductId.Value != Guid.Empty
                ? request.Contract.ProductId.Value : null,
            MovementTypes = request.Contract.MovementTypes,
            DateFromUtc = request.Contract.DateFromUtc,
            DateToUtc = request.Contract.DateToUtc,
            Page = page,
            PageSize = pageSize
        };

        return await _stockMovementQueryRepository.GetPagedAsync(organizationId.Value, criteria);
    }
}
#endregion
