using System;
using System.Collections.Generic;

namespace Identity.Contracts.DTOs
{
    public sealed record TokenGenerationDto(
        Guid UserId,
        Guid OrganizationId,
        string Role
    );
}
