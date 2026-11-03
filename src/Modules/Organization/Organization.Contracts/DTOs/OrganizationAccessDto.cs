using System;
using System.Collections.Generic;
using System.Text;

namespace Organization.Contracts.DTOs
{
    public sealed record OrganizationAccessDto(
        Guid OrganizationId,
        string OrganizationName,
        string Role
    );
}