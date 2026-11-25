using Inventory.Application.Common.Interfaces;
using Inventory.Contracts.DTOs.Warehouse;
using Inventory.Contracts.UseCases.WarehouseCases;
using MediatR;

namespace Inventory.Application.Bindings.WarehouseCases;

public record ActivateWarehouseCommandMediatR(ActivateWarehouseCommand Contract) : ICommand<WarehouseDto>, IContractRequest
{
    object IContractRequest.Contract => Contract;
}
public record DeactivateWarehouseCommandMediatR(DeactivateWarehouseCommand Contract) : ICommand<WarehouseDto>, IContractRequest
{
    object IContractRequest.Contract => Contract;
}
public record RegisterWarehouseCommandMediatR(RegisterWarehouseCommand Contract) : ICommand<WarehouseDto>, IContractRequest
{
    object IContractRequest.Contract => Contract;
}
public record UpdateWarehouseCommandMediatR(UpdateWarehouseCommand Contract) : ICommand<WarehouseDto>, IContractRequest
{
    object IContractRequest.Contract => Contract;
}