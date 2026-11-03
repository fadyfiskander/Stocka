using Identity.Application.Common.Interfaces;
using Identity.Application.DTOs.Identity.Users.Login;
using Identity.Contracts.DTOs.Identity.Users.Login;
using MediatR;
using Organization.Contracts.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Application.UseCases.Authentication.Users
{
    #region AuthenticateUserCommand
    internal record AuthenticateUserCommand : IRequest<LoginResult>
    {
        public string Email = default!;
        public string Password = default!;

        public AuthenticateUserCommand(LoginRequest loginRequest)
        {
            Email = loginRequest.Email;
            Password = loginRequest.Password;
        }
    }
    #endregion

    #region AuthenticateUserCommandHandler
    internal class AuthenticateUserCommandHandler : IRequestHandler<AuthenticateUserCommand, LoginResult>
    {
        private readonly IUserServices _userServices;
        private readonly IOrganizationAuthService _organizationAuthService;

        public AuthenticateUserCommandHandler(IUserServices userServices, IOrganizationAuthService organizationAuthService)
        {
            _userServices = userServices;
            _organizationAuthService = organizationAuthService;
        }

        public async Task<LoginResult> Handle(AuthenticateUserCommand request, CancellationToken cancellationToken)
        {
            var result = await _userServices.GetAuthenticateResultAsync(request.Email, request.Password, cancellationToken);

            var organizations = await _organizationAuthService.OrganizationAccessesAsync(result.UserId, cancellationToken);


            var loginResult = new LoginResult(
                result.UserId,
                result.PartyId,
                result.FirstName,
                result.LastName,
                result.Email,
                result.UserName,
                organizations.ToList());

            return loginResult;
        }
    }
    #endregion
}