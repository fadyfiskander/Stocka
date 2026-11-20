using System;

namespace Inventory.Contracts.DTOs.Warehouse
{
    public record WarehouseDto(
        Guid Id,
        Guid OrganizationId,
        string Name,
        string Location,
        string? Description,
        bool IsActive,
        int Type
    );
}

