using System;

namespace Organization.Contracts.DTOs
{
    /// <summary>
    /// One organization membership row (a user's membership in an organization).
    /// The pair (OrganizationId, UserId) is the composite key of the relationship.
    /// </summary>
    public record OrganizationMembershipDto(
        Guid OrganizationId,
        string Name,
        string Role
    );
}
