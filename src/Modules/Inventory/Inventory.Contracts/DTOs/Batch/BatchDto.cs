using System;

namespace Inventory.Contracts.DTOs.Batch
{
    public record BatchDto(
        Guid Id,
        Guid ProductId,
        Guid PurchaseItemId,
        Guid OrganizationId,
        int InitialQuantity,
        int RemainingQuantity,
        decimal UnitCost,
        DateTime CreatedAt
    );
}

