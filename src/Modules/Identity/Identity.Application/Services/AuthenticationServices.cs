using Identity.Application.Common.Interfaces;
using Identity.Application.DTOs.Identity.Users.Login;
using Identity.Application.UseCases.Authentication.Users;
using Identity.Contracts.DTOs;
using Identity.Contracts.DTOs.Identity.Users.Login;
using Identity.Contracts.Services;
using MediatR;

namespace Identity.Application.Services
{
    /// <summary>
    /// Public cross-module adapter for the Identity module: the single entry point answering
    /// <see cref="IAuthenticationServices"/>. Delegates each operation to the internal services
    /// (<see cref="IIdentityService"/> / <see cref="IUserServices"/>); the rich MVC login is
    /// dispatched through the MediatR pipeline (validation and unit-of-work behaviors included).
    /// Contains no business logic.
    /// </summary>
    public class AuthenticationServices : IAuthenticationServices
    {
        private readonly IMediator _mediator;

        public AuthenticationServices(IMediator mediator)
        {
            _mediator = mediator;
        }

        #region Authentication
        public async Task<LoginResult> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            return await _mediator.Send(new AuthenticateUserCommand(request), cancellationToken);
        }
        #endregion
    }
}