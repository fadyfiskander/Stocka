using System;
using System.Collections.Generic;

namespace Organization.Contracts.DTOs
{
    /// <summary>
    /// Public Organization module representation of an organization.
    /// Carries only display identifiers (raw Guids) — never domain primitives
    /// or infrastructure concerns. Members come from the organization's
    /// <c>OrganizationMembership</c> rows.
    /// </summary>
    public record OrganizationDto(
        Guid Id,
        string Name,
        string Description,
        string Slug,
        string? LogoUrl,
        IReadOnlyList<Guid> MemberUserIds
    );
}
