using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Models;
using Inventory.Application.Bindings.BatchCases;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.Batch;
using Inventory.Contracts.UseCases.BatchCases;
using Inventory.Primitives.Identifiers;
using MediatR;

namespace Inventory.Application.UseCases.BatchCases;

#region Handler
/// <summary>
/// Handles the batch listing: routes to the matching repository query â€” warehouse placement
/// (paged in memory), product filter, purchase-item filter, or the organization-wide paged list.
/// </summary>
public class ListBatchesQueryHandler(IBatchQueryRepository batchQuery, ICurrentUserService currentUser) : IRequestHandler<ListBatchesQueryMediatR, PaginatedResult<BatchDto>>
{
    public async Task<PaginatedResult<BatchDto>> Handle(ListBatchesQueryMediatR request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Contract.Page);
        var pageSize = Math.Clamp(request.Contract.PageSize, 1, 100);

        var organizationId = currentUser.OrganizationId;
        if (organizationId is null)
            throw new ArgumentNullException("OrganizationId is null, OrganizationId cannot be null");

        if (request.Contract.WarehouseId.HasValue && request.Contract.WarehouseId.Value != Guid.Empty)
        {
            var warehouseBatches = await batchQuery.GetByWarehouseAsync(organizationId.Value, WarehouseId.From(request.Contract.WarehouseId.Value));
            return warehouseBatches.ToPaginated(page, pageSize);
        }

        if (request.Contract.ProductId.HasValue && request.Contract.ProductId.Value != Guid.Empty)
            return await batchQuery.GetByProductAsync(organizationId.Value, request.Contract.ProductId.Value, page, pageSize);

        if (request.Contract.PurchaseItemId.HasValue && request.Contract.PurchaseItemId.Value != Guid.Empty)
            return await batchQuery.GetByPurchaseItemAsync(organizationId.Value, request.Contract.PurchaseItemId.Value, page, pageSize);

        return await batchQuery.GetAllPagedAsync(organizationId.Value, page, pageSize);
    }
}
#endregion
