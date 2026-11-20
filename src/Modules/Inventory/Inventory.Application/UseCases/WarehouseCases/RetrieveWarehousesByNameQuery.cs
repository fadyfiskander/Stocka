using BuildingBlocks.Application.Abstractions;
using Inventory.Application.Bindings.WarehouseCases;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.Warehouse;
using Inventory.Contracts.UseCases.WarehouseCases;
using MediatR;

namespace Inventory.Application.UseCases.WarehouseCases;

#region Handler
/// <summary>
/// Handles the name search: returns the warehouses of the current Organization whose name contains the
/// trimmed search value.
/// </summary>
public class RetrieveWarehousesByNameQueryHandler : IRequestHandler<RetrieveWarehousesByNameQueryMediatR, IEnumerable<WarehouseDto>>
{
    private readonly IWarehouseQueryRepository _warehouseQueryRepository;
    private readonly ICurrentUserService _currentUser;

    public RetrieveWarehousesByNameQueryHandler(IWarehouseQueryRepository warehouseQueryRepository, ICurrentUserService currentUser)
    {
        _warehouseQueryRepository = warehouseQueryRepository;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<WarehouseDto>> Handle(RetrieveWarehousesByNameQueryMediatR request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Contract.Name))
            throw new ArgumentException("Warehouse name search value cannot be empty.", nameof(request.Contract.Name));

        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            throw new ArgumentNullException("OrganizationId is null, OrganizationId cannot be null");

        return await _warehouseQueryRepository.GetByNameAsync(organizationId.Value, request.Contract.Name.Trim());
    }
}
#endregion
