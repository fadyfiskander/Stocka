using System;

namespace Inventory.Contracts.DTOs.WarehouseBatch
{
    public record WarehouseBatchDto(
        Guid Id,
        Guid WarehouseId,
        Guid BatchId,
        int Quantity,
        Guid OrganizationId
    );
}

