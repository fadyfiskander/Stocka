using Identity.Contracts.DTOs;
using Identity.Contracts.DTOs.Identity.Users.Login;

namespace Identity.Contracts.Services
{
    /// <summary>
    /// Public Identity module contract: the single entry point for other modules and hosts.
    /// Every operation uses only <c>Identity.Contracts</c> types — never Application/Domain
    /// DTOs — so the boundary stays dependency-free.
    /// <para>
    /// Implemented by the Identity Application layer: the rich MVC login flows through the
    /// MediatR pipeline (validation + unit-of-work behaviors included); the remaining
    /// operations delegate to the module's internal services.
    /// </para>
    /// </summary>
    public interface IAuthenticationServices
    {
        #region Authentication
        Task<LoginResult> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default);
        #endregion
    }
}
