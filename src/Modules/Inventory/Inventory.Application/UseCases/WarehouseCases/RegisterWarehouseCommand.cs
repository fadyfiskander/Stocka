using BuildingBlocks.Application.Abstractions;
using Inventory.Application.Bindings.WarehouseCases;
using Inventory.Application.Repositories.Commands;
using Inventory.Contracts.DTOs.Warehouse;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using MediatR;

namespace Inventory.Application.UseCases.WarehouseCases;

#region Handler
/// <summary>
/// Creates the warehouse through the domain factory (which enforces name and type invariants)
/// and returns the created warehouse data transfer object.
/// </summary>
public class RegisterWarehouseCommandHandler : IRequestHandler<RegisterWarehouseCommandMediatR, WarehouseDto>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ICurrentUserService _currentUser;

    public RegisterWarehouseCommandHandler(IWarehouseRepository warehouseRepository, ICurrentUserService currentUser)
    {
        _warehouseRepository = warehouseRepository;
        _currentUser = currentUser;
    }

    public async Task<WarehouseDto> Handle(RegisterWarehouseCommandMediatR request, CancellationToken cancellationToken)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            throw new ArgumentNullException("OrganizationId is null, OrganizationId cannot be null");

        var warehouse = Warehouse.Create(
            organizationId.Value,
            request.Contract.Name,
            (WarehouseType)request.Contract.Type,
            request.Contract.Location,
            request.Contract.Description);

        await _warehouseRepository.AddAsync(warehouse);

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