using BuildingBlocks.Application.Common.Models;
using Inventory.Contracts.DTOs.Warehouse;

namespace Inventory.Contracts.UseCases.WarehouseCases;

/// <summary>
/// Warehouse use case contract: the pure module boundary for every warehouse operation.
/// Implemented by the Inventory Application layer (which wires each call into its MediatR
/// pipeline — validation and unit-of-work included) and consumed via DI.
/// </summary>
public interface IWarehouseCases
{
    #region Commands
    /// <summary>Activates an existing warehouse.</summary>
    Task<WarehouseDto> ActivateWarehouseAsync(ActivateWarehouseCommand command, CancellationToken cancellationToken = default);

    /// <summary>Deactivates an existing warehouse (non-destructive; stock and history are kept).</summary>
    Task<WarehouseDto> DeactivateWarehouseAsync(DeactivateWarehouseCommand command, CancellationToken cancellationToken = default);

    /// <summary>Registers a new warehouse for the current Organization.</summary>
    Task<WarehouseDto> RegisterWarehouseAsync(RegisterWarehouseCommand command, CancellationToken cancellationToken = default);

    /// <summary>Applies a partial metadata update; only provided fields change.</summary>
    Task<WarehouseDto> UpdateWarehouseAsync(UpdateWarehouseCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region Queries
    /// <summary>Retrieves one warehouse by ID (cross-Organization IDs are indistinguishable from not-found).</summary>
    Task<WarehouseDto> RetrieveWarehouseAsync(RetrieveWarehouseQuery query, CancellationToken cancellationToken = default);

    /// <summary>Retrieves warehouses whose name contains the search value.</summary>
    Task<IEnumerable<WarehouseDto>> RetrieveWarehousesByNameAsync(RetrieveWarehousesByNameQuery query, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a paged warehouse listing, optionally filtered by activation state.</summary>
    Task<PaginatedResult<WarehouseDto>> ListWarehousesAsync(ListWarehousesQuery query, CancellationToken cancellationToken = default);
    #endregion
}
