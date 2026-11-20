using BuildingBlocks.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Identity.Infrastructure.Services
{
    public class CurrentUserService(IHttpContextAccessor httpContext) : ICurrentUserService
    {
        private const string OrganizationIdClaim = "OrganizationId";

        public Guid? UserId
        {
            get
            {
                var user = httpContext.HttpContext?.User;
                if (user is null)
                    return null;

                var userId = user.FindFirst(IdentityClaimTypes.UserId)?.Value
                    ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return Guid.TryParse(userId, out var id)
                    ? id
                    : null;
            }
        }

        public Guid? OrganizationId 
        {
            get
            {
                var user = httpContext.HttpContext?.User;
                if (user is null)
                    return null;

                // org_id is the canonical claim (JWT / cookie); the legacy literal is kept
                // as a fallback for already-issued cookies.
                var organizationId = user.FindFirst(IdentityClaimTypes.OrganizationId)?.Value
                    ?? user.FindFirst(OrganizationIdClaim)?.Value;

                return Guid.TryParse(organizationId, out var id)
                    ? id
                    : null;
            }
        }

        public string? Role
        {
            get
            {
                var user = httpContext.HttpContext?.User;
                if (user is null)
                    return null;

                // ClaimTypes.Role is what [Authorize(Roles = ...)] reads on the cookie flow.
                var role = user.FindFirst(IdentityClaimTypes.Role)?.Value
                    ?? user.FindFirst(ClaimTypes.Role)?.Value;

                return string.IsNullOrEmpty(role)
                    ? null
                    : role;
            }
        }
    }
}
