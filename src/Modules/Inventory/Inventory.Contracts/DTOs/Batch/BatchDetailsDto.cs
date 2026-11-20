using BuildingBlocks.Application.Common.Models;
using System;

namespace Inventory.Contracts.DTOs.Batch
{
    /// <summary>
    /// Complete batch view with every quantity distinction preserved:
    /// - InitialQuantity: total received from the source document.
    /// - RemainingQuantity: units NOT yet allocated to a warehouse.
    /// - AllocatedQuantity: units ever placed into warehouses (Initial - Remaining).
    /// - QuantityInWarehouses: units currently held in warehouses (current stock â€” may have
    ///   moved out via sales/damage/adjustments since allocation).
    /// These values are intentionally NOT interchangeable.
    /// </summary>
    public record BatchDetailsDto(
        Guid Id,
        Guid ProductId,
        Guid PurchaseItemId,
        Guid OrganizationId,
        int InitialQuantity,
        int RemainingQuantity,
        int AllocatedQuantity,
        int QuantityInWarehouses,
        bool IsExhausted,
        decimal UnitCost,
        decimal InitialTotalCost,
        decimal RemainingValue,
        DateTime CreatedAt
    );

    public record BatchHistoryEntryDto(
        Guid MovementId,
        Guid ProductId,
        Guid WarehouseId,
        int Quantity,
        bool IsInbound,
        int MovementType,
        int? ReferenceType,
        Guid? ReferenceId,
        string? Reason,
        DateTime CreatedAt
    );

    public record BatchProductFilter
    {
        public Guid? ProductId { get; init; }
        public Guid? PurchaseItemId { get; init; }
        public Guid? WarehouseId { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    public record BatchPagedRequest
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}

