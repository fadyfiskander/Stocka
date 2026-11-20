using System;

namespace BuildingBlocks.Application.Abstractions
{
    public interface IMultiTenantEntity
    {
        Guid OrganizationId { get; set; }
    }
}
