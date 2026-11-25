using BuildingBlocks.Application.Abstractions;
using Inventory.Application.Bindings.WarehouseCases;
using Inventory.Application.Repositories.Commands;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.Warehouse;
using Inventory.Contracts.UseCases.WarehouseCases;
using Inventory.Domain.Warehouses;
using MediatR;

namespace Inventory.Application.UseCases.WarehouseCases;

#region Handler
/// <summary>
/// Handles the partial update of a warehouse: enforces Organization ownership (cross-Organization IDs are
/// indistinguishable from not-found), name uniqueness within the Organization, and that at least one
/// field was provided, then returns the updated warehouse data transfer object.
/// </summary>
public class UpdateWarehouseCommandHandler : IRequestHandler<UpdateWarehouseCommandMediatR, WarehouseDto>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IWarehouseQueryRepository _warehouseQueryRepository;
    private readonly ICurrentUserService _currentUser;

    public UpdateWarehouseCommandHandler(
        IWarehouseRepository warehouseRepository,
        IWarehouseQueryRepository warehouseQueryRepository,
        ICurrentUserService currentUser)
    {
        _warehouseRepository = warehouseRepository;
        _warehouseQueryRepository = warehouseQueryRepository;
        _currentUser = currentUser;
    }

    public async Task<WarehouseDto> Handle(UpdateWarehouseCommandMediatR request, CancellationToken cancellationToken)
    {
        // Tenant filter applies on the tracked entity as well; cross-Organization IDs come back as not-found.
        var warehouse = await _warehouseRepository.GetByIdAsync(WarehouseId.From(request.Contract.WarehouseId))
            ?? throw new KeyNotFoundException($"Warehouse '{request.Contract.WarehouseId}' was not found.");

        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            throw new ArgumentNullException("OrganizationId is null, OrganizationId cannot be null");

        if (warehouse.OrganizationId != organizationId.Value)
            throw new KeyNotFoundException($"Warehouse '{request.Contract.WarehouseId}' was not found.");

        bool changeRequested = false;

        if (!string.IsNullOrWhiteSpace(request.Contract.Name))
        {
            var duplicate = await _warehouseQueryRepository.GetByExactNameAsync(request.Contract.Name.Trim());
            if (duplicate != null && duplicate.Id != warehouse.Id.Value)
                throw new InvalidOperationException($"A warehouse named '{request.Contract.Name.Trim()}' already exists.");

            warehouse.Rename(request.Contract.Name);
            changeRequested = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Contract.Location))
        {
            warehouse.UpdateLocation(request.Contract.Location);
            changeRequested = true;
        }

        if (request.Contract.Description is not null)
        {
            warehouse.UpdateDescription(request.Contract.Description);
            changeRequested = true;
        }

        if (request.Contract.Type.HasValue)
        {
            warehouse.ChangeType((WarehouseType)request.Contract.Type.Value);
            changeRequested = true;
        }

        if (!changeRequested)
            throw new ArgumentException("At least one property must be provided to update the warehouse.", nameof(request));

        await _warehouseRepository.UpdateAsync(warehouse);

        return new WarehouseDto(
            warehouse.Id.Value,
            warehouse.OrganizationId,
            warehouse.Name,
            warehouse.Location,
            warehouse.Description,
            warehouse.IsActive,
            (int)warehouse.Type);
    }
}
#endregion
