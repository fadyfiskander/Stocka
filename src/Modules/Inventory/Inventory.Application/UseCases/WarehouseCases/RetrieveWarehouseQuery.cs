using BuildingBlocks.Application.Abstractions;
using Inventory.Application.Bindings.WarehouseCases;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.Warehouse;
using Inventory.Contracts.UseCases.WarehouseCases;
using Inventory.Primitives.Identifiers;
using MediatR;

namespace Inventory.Application.UseCases.WarehouseCases;

#region Handler
/// <summary>
/// Handles the retrieval of a warehouse: loads it through the query repository scoped to the
/// current Organization and returns the warehouse data transfer object, or not-found when absent.
/// </summary>
public class RetrieveWarehouseQueryHandler : IRequestHandler<RetrieveWarehouseQueryMediatR, WarehouseDto>
{
    private readonly IWarehouseQueryRepository _warehouseQueryRepository;
    private readonly ICurrentUserService _currentUser;

    public RetrieveWarehouseQueryHandler(IWarehouseQueryRepository warehouseQueryRepository, ICurrentUserService currentUser)
    {
        _warehouseQueryRepository = warehouseQueryRepository;
        _currentUser = currentUser;
    }

    public async Task<WarehouseDto> Handle(RetrieveWarehouseQueryMediatR request, CancellationToken cancellationToken)
    {
        if (request.Contract.WarehouseId == Guid.Empty)
            throw new ArgumentException("Warehouse ID cannot be empty.", nameof(request.Contract.WarehouseId));

        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            throw new ArgumentNullException("OrganizationId is null, OrganizationId cannot be null");

        var warehouse = await _warehouseQueryRepository.GetByIdAsync(organizationId.Value, WarehouseId.From(request.Contract.WarehouseId));
        return warehouse ?? throw new KeyNotFoundException($"Warehouse '{request.Contract.WarehouseId}' was not found.");
    }
}
#endregion
