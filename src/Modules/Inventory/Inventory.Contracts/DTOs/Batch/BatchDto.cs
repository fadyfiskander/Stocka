using System;
using System.Text.Json.Serialization;

namespace Inventory.Contracts.DTOs.Batch
{
    public record BatchDto(
        Guid Id,
        string ProductName,
        Guid PurchaseItemId,
        Guid OrganizationId,
        int InitialQuantity,
        int RemainingQuantity,
        decimal UnitCost,
        DateTime CreatedAt
    )
    {
        [JsonIgnore]
        public Guid ProductId { get; init; }
    }

    public record BatchProductGuidDto(
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

