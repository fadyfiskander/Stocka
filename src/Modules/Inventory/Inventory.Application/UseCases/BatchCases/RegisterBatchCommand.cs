using BuildingBlocks.Application.Abstractions;
using Inventory.Application.Bindings.BatchCases;
using Inventory.Application.Repositories.Commands;
using Inventory.Contracts.DTOs.Batch;
using Inventory.Contracts.UseCases.BatchCases;
using Inventory.Domain.Entities;
using MediatR;

namespace Inventory.Application.UseCases.BatchCases;

#region Handler
/// <summary>
/// Handles the registration: creates the batch entity through its constructor (which stamps the
/// current Organization) and returns the batch data transfer object. No stock placement happens here.
/// </summary>
public class RegisterBatchCommandHandler : IRequestHandler<RegisterBatchCommandMediatR, BatchDto>
{
    private readonly IBatchRepository _batchRepository;
    private readonly ICurrentUserService _currentUser;

    public RegisterBatchCommandHandler(IBatchRepository batchRepository, ICurrentUserService currentUser)
    {
        _batchRepository = batchRepository;
        _currentUser = currentUser;
    }

    public async Task<BatchDto> Handle(RegisterBatchCommandMediatR request, CancellationToken cancellationToken)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            throw new ArgumentNullException("OrganizationId is null, OrganizationId cannot be null");

        var batch = new Batch(
            request.Contract.PurchaseItemId,
            request.Contract.ProductId,
            organizationId.Value,
            request.Contract.InitialQuantity,
            request.Contract.UnitCost);

        await _batchRepository.AddAsync(batch);

        return new BatchDto(
            batch.Id.Value,
            batch.ProductId,
            batch.PurchaseItemId,
            batch.OrganizationId,
            batch.InitialQuantity,
            batch.RemainingQuantity,
            batch.UnitCost,
            batch.CreatedAt);
    }
}
#endregion
