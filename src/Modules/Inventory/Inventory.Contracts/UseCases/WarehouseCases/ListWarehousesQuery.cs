using BuildingBlocks.Application.Common.Models;
using Inventory.Contracts.DTOs.Warehouse;

namespace Inventory.Contracts.UseCases.WarehouseCases;

/// <summary>
/// Paged warehouse listing for the current Organization.
/// IsActive filters to a single activation state; null returns both.
/// Total-stock ranking uses WarehousesRankedByStockQuery (database-side aggregation).
/// </summary>
/// <remarks>
/// No dedicated validator: paging bounds are clamped defensively in the handler
/// (page â‰¥ 1, page size in [1, 100]) instead of failing the request.
/// </remarks>
public record ListWarehousesQuery
{
    /// <summary>Activation-state filter; null disables the filter.</summary>
    public bool? IsActive { get; init; }

    /// <summary>Requested page number (clamped to â‰¥ 1).</summary>
    public int Page { get; init; } = 1;

    /// <summary>Page size (clamped to [1, 100]).</summary>
    public int PageSize { get; init; } = 20;
}
