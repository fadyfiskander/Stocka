using Inventory.Contracts.DTOs.Warehouse;

namespace Inventory.Contracts.UseCases.WarehouseCases;

/// <summary>
/// Registers a new warehouse for the current Organization.
/// </summary>
/// <remarks>
/// Rules (including the type check) are enforced by the RegisterWarehouseCommandValidator;
/// the type value is additionally enforced by the domain factory.
/// </remarks>
public record RegisterWarehouseCommand
{
    /// <summary>Display name; must be unique within the Organization (database NVARCHAR(50)).</summary>
    public string Name { get; init; } = default!;

    /// <summary>Physical location of the warehouse (database NVARCHAR(100)).</summary>
    public string Location { get; init; } = default!;

    /// <summary>Optional free-text description (database NVARCHAR(600)).</summary>
    public string? Description { get; init; }

    /// <summary>Warehouse type as an integer WarehouseType value.</summary>
    public int Type { get; init; }
}
