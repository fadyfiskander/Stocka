using BuildingBlocks.Application.Common.Models;
using Inventory.Application.Bindings.WarehouseCases;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.Warehouse;
using Inventory.Contracts.UseCases.WarehouseCases;
using Inventory.Domain.Warehouses;
using MediatR;

namespace Inventory.Application.Adapters.WarehouseCases;

/// <summary>
/// Implements the IWarehouseCases module boundary contract. Commands are dispatched through the
/// application's MediatR pipeline (validation behavior, unit-of-work behavior, handler); queries
/// hit the read-side repository directly, scoped to the current Organization.
/// Registered in DI as the implementation of the module boundary contract.
/// </summary>
public class WarehouseCases : IWarehouseCases
{
    private readonly IMediator _mediator;
    private readonly IWarehouseQueryRepository _warehouseQueryRepository;

    public WarehouseCases(IMediator mediator, IWarehouseQueryRepository warehouseQueryRepository)
    {
        _mediator = mediator;
        _warehouseQueryRepository = warehouseQueryRepository;
    }

    private static int Page(int page) => Math.Max(1, page);
    private static int Size(int pageSize) => Math.Clamp(pageSize, 1, 100);

    // ---- Commands (through the MediatR pipeline) ----

    public Task<WarehouseDto> ActivateWarehouseAsync(ActivateWarehouseCommand command, CancellationToken cancellationToken = default)
        => _mediator.Send(new ActivateWarehouseCommandMediatR(command), cancellationToken);

    public Task<WarehouseDto> DeactivateWarehouseAsync(DeactivateWarehouseCommand command, CancellationToken cancellationToken = default)
        => _mediator.Send(new DeactivateWarehouseCommandMediatR(command), cancellationToken);

    public Task<WarehouseDto> RegisterWarehouseAsync(RegisterWarehouseCommand command, CancellationToken cancellationToken = default)
        => _mediator.Send(new RegisterWarehouseCommandMediatR(command), cancellationToken);

    public Task<WarehouseDto> UpdateWarehouseAsync(UpdateWarehouseCommand command, CancellationToken cancellationToken = default)
        => _mediator.Send(new UpdateWarehouseCommandMediatR(command), cancellationToken);

    // ---- Queries (direct repository access) ----

    public async Task<WarehouseDto> RetrieveWarehouseAsync(RetrieveWarehouseQuery query, CancellationToken cancellationToken = default)
    {
        if (query.WarehouseId == Guid.Empty)
            throw new ArgumentException("Warehouse ID cannot be empty.", nameof(query.WarehouseId));

        var warehouse = await _warehouseQueryRepository.GetByIdAsync(WarehouseId.From(query.WarehouseId), cancellationToken);
        return warehouse ?? throw new KeyNotFoundException($"Warehouse '{query.WarehouseId}' was not found.");
    }

    public async Task<IEnumerable<WarehouseDto>> RetrieveWarehousesByNameAsync(RetrieveWarehousesByNameQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Name))
            throw new ArgumentException("Warehouse name search value cannot be empty.", nameof(query.Name));

        return await _warehouseQueryRepository.GetByNameAsync(query.Name.Trim(), cancellationToken);
    }

    public async Task<PaginatedResult<WarehouseDto>> ListWarehousesAsync(ListWarehousesQuery query, CancellationToken cancellationToken = default)
    {
        var page = Page(query.Page);
        var pageSize = Size(query.PageSize);

        return query.IsActive.HasValue
            ? await _warehouseQueryRepository.GetPagedAsync(query.IsActive.Value, page, pageSize, cancellationToken)
            : await _warehouseQueryRepository.GetPagedAsync(page, pageSize, cancellationToken);
    }
}
