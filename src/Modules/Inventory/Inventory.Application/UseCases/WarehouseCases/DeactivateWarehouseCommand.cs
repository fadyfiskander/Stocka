using BuildingBlocks.Application.Abstractions;
using Inventory.Application.Bindings.WarehouseCases;
using Inventory.Application.Repositories.Commands;
using Inventory.Contracts.DTOs.Warehouse;
using Inventory.Contracts.UseCases.WarehouseCases;
using Inventory.Domain.Warehouses;
using MediatR;

namespace Inventory.Application.UseCases.WarehouseCases;

#region Handler
/// <summary>
/// Handles the command to deactivate a warehouse and returns the updated warehouse data.
/// </summary>
public class DeactivateWarehouseCommandHandler : IRequestHandler<DeactivateWarehouseCommandMediatR, WarehouseDto>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ICurrentUserService _currentUser;

    public DeactivateWarehouseCommandHandler(IWarehouseRepository warehouseRepository, ICurrentUserService currentUser)
    {
        _warehouseRepository = warehouseRepository;
        _currentUser = currentUser;
    }

    public async Task<WarehouseDto> Handle(DeactivateWarehouseCommandMediatR request, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(WarehouseId.From(request.Contract.WarehouseId))
            ?? throw new KeyNotFoundException($"Warehouse '{request.Contract.WarehouseId}' was not found.");

        if (warehouse.OrganizationId != _currentUser.OrganizationId)
            throw new KeyNotFoundException($"Warehouse '{request.Contract.WarehouseId}' was not found.");

        warehouse.Deactivate();
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
