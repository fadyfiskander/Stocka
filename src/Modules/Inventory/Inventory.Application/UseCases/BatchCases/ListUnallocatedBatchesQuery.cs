using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Models;
using Inventory.Application.Bindings.BatchCases;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.Batch;
using MediatR;

namespace Inventory.Application.UseCases.BatchCases;

#region Handler
/// <summary>
/// Handles the unallocated batch listing: fetches all unallocated batches of the current Organization
/// and pages them in memory.
/// </summary>
public class ListUnallocatedBatchesQueryHandler(IBatchQueryRepository batchQuery, ICurrentUserService currentUser) : IRequestHandler<ListUnallocatedBatchesQueryMediatR, PaginatedResult<BatchDto>>
{
    public async Task<PaginatedResult<BatchDto>> Handle(ListUnallocatedBatchesQueryMediatR request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Contract.Page);
        var pageSize = Math.Clamp(request.Contract.PageSize, 1, 100);

        var organizationId = currentUser.OrganizationId;
        if (organizationId is null)
            throw new ArgumentNullException("OrganizationId is null, OrganizationId cannot be null");

        var batches = await batchQuery.GetUnallocatedAsync(organizationId.Value);
        return batches.ToPaginated(page, pageSize);
    }
}
#endregion
