using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Contracts.DTOs.Identity.Users.Login
{
    public sealed record LoginRequest(
        string Email,
        string Password
    );
}
