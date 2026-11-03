using BuildingBlocks.Application.Common.Interfaces;
using BuildingBlocks.Domain.Enums;
using Identity.Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Stocka.MVC.Infrastructure;

/// <summary>
/// Host-side adapter of the shared contract (BuildingBlocks) — reads the user id and
/// active organization from the authenticated request claims. Until the Identity module
/// is wired in, claims are usually empty, so ActiveOrganizationId resolves to the
/// configured default (or Guid.Empty when none is set).
/// </summary>
public sealed class HttpUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Guid _fallbackOrganizationId;

    public HttpUserContext(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;

        var fallbackString = configuration["Organization:DefaultOrganizationId"];
        _fallbackOrganizationId = Guid.TryParse(fallbackString, out var g) ? g : Guid.Empty;
    }

    private System.Security.Claims.ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid UserId
    {
        get
        {
            // Claim names follow what the Host account controller writes at login
            // (aligned with Identity's ClaimNames) — "sub"/NameIdentifier as fallbacks.
            var id = User?.FindFirst(ClaimNames.UserId)?.Value
                     ?? User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                     ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(id, out var g) ? g : Guid.Empty;
        }
    }

    public Guid ActiveOrganizationId
    {
        get
        {
            var val = User?.FindFirst(ClaimNames.ActiveOrganizationId)?.Value
                      ?? User?.FindFirst("ActiveOrganizationId")?.Value
                      ?? User?.FindFirst("OrganizationId")?.Value;

            return Guid.TryParse(val, out var g) ? g : _fallbackOrganizationId;
        }
    }

    public Guid? ActiveBranchId
    {
        get
        {
            var val = User?.FindFirst(ClaimNames.ActiveBranchId)?.Value ?? User?.FindFirst("branch")?.Value;
            return Guid.TryParse(val, out var g) ? g : null;
        }
    }

    // Least-privileged role for unauthenticated/claim-less requests.
    public OrganizationRole Role
    {
        get
        {
            var val = User?.FindFirst(ClaimNames.OrganizationRole)?.Value ?? User?.FindFirst("OrganizationRole")?.Value;
            return Enum.TryParse<OrganizationRole>(val, out var role) ? role : OrganizationRole.Viewer;
        }
    }

    public bool IsOwner => Role == OrganizationRole.Owner;

    public bool CanAccessAllBranches => Role is OrganizationRole.Owner or OrganizationRole.OrganizationAdmin;
}
