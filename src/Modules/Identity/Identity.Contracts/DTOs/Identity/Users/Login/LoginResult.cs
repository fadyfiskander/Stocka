using Identity.Contracts.DTOs;
using Organization.Contracts.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Contracts.DTOs.Identity.Users.Login
{
    public sealed record LoginResult(
        Guid UserId,
        Guid PartyId,
        string FirstName,
        string LastName,
        string Email,
        string UserName,
        IEnumerable<OrganizationAccessDto> Organizations);
}
