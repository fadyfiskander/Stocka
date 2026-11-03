using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Application.DTOs.Identity.Users.Login;

public sealed record AuthenticateResult(
    Guid UserId,
    Guid PartyId,
    string FirstName,
    string LastName,
    string Email,
    string UserName);
