using Azure.Core;
using Identity.Application.Common.Interfaces;
using Identity.Application.DTOs.Identity.Users.Login;
using Identity.Contracts.DTOs;
using Identity.Contracts.Exceptions;
using Identity.Infrastructure.Exceptions;
using Identity.Infrastructure.Identity;
using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Services
{
    /// <summary>
    /// Natural implementation of <see cref="IUserServices"/>: raw email/password
    /// authentication through ASP.NET Identity's <see cref="UserManager{TUser}"/>, with the
    /// person fields (FirstName/LastName) resolved from the linked Party row — the same
    /// storage rules <see cref="IdentityService"/> follows — and roles materialized as
    /// contract <see cref="RoleDto"/>s (Id + RoleName) instead of bare names.
    /// <para>
    /// Failure semantics: a missing account raises <see cref="NotFoundException"/>, a wrong
    /// password raises <see cref="BadRequestException"/> — <see cref="AuthenticateResult"/>
    /// carries no failure channel, so an authenticated non-result has no meaningful shape.
    /// </para>
    /// </summary>
    public class UserServices : IUserServices
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly AppIdentityDbContext _context;

        public UserServices(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            AppIdentityDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<AuthenticateResult> GetAuthenticateResultAsync(string email, string password, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
                throw new InvalidCredentialsException();

            var isValid = await _userManager.CheckPasswordAsync(
                user,
                password
            );

            if (!isValid)
                throw new InvalidCredentialsException();


            // Person data lives on the Party aggregate — one flat projection, no tracking.
            var party = await _context.Parties
                .Where(p => p.Id == user.PartyId)
                .Select(p => new { p.Id, p.FirstName, p.LastName })
                .FirstOrDefaultAsync(cancellationToken);

            if (party is null)
                throw new NotFoundException("PARTY_NOT_FOUND");

            return new AuthenticateResult(
                user.Id,
                party.Id.Value,
                party.FirstName,
                party.LastName,
                user.Email ?? string.Empty,
                user.UserName ?? string.Empty);
        }

        /// <inheritdoc cref="IUserServices.GetUserRolesAsync"/>
        public async Task<IEnumerable<RoleDto>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                throw new NotFoundException("USER_NOT_FOUND");

            var roleNames = await _userManager.GetRolesAsync(user);
            if (roleNames.Count == 0)
                return [];

            // One batched lookup for the role Ids; a name whose role row vanished
            // (deleted out-of-band) is dropped rather than failing the request.
            var roles = await _roleManager.Roles
                .Where(r => roleNames.Contains(r.Name!))
                .Select(r => new RoleDto(r.Id, r.Name!))
                .ToListAsync(cancellationToken);

            return roles;
        }
    }
}