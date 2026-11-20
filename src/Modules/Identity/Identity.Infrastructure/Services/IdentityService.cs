using Identity.Application.Common.Interfaces;
using Identity.Application.Repositories.Commands;
using Identity.Contracts.DTOs;
using Identity.Domain.Parties;
using Identity.Infrastructure.Authentication;
using Identity.Infrastructure.Exceptions;
using Identity.Infrastructure.Identity;
using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;

namespace Identity.Infrastructure.Services
{
    /// <summary>
    /// ASP.NET Identity operations on UserManager/RoleManager (Infrastructure capability).
    /// <para>
    /// Design notes:
    /// <list type="bullet">
    /// <item>Person data (FirstName/LastName/PhoneNumber) lives on the linked Party row;
    /// the ApplicationUser carries only identity fields (username/email).</item>
    /// <item>Creating a user atomically creates its person Party — the tracked Party rows
    /// are flushed by the UserManager store save, so both inserts share one SaveChanges
    /// and the 1:1 FK (<see cref="ApplicationUser.PartyId"/>) is always satisfied.</item>
    /// <item>Organization membership is resolved through PartyOrganizations (multi-org per
    /// person); the legacy per-user brand-membership table has no equivalent anymore.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class IdentityService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ITokenGenerator tokenGenerator,
            IPartyRepository partyRepository,
            IIdentityUnitOfWork unitOfWork,
            AppIdentityDbContext context) : IIdentityService
    {

        #region Authentication (JWT issuance)

        /// <inheritdoc cref="IIdentityService.RegisterAsync(RegisterModel, Guid)"/>
        public async Task<AuthModel> RegisterAsync(RegisterModel model, Guid organizationId)
        {
            var authModel = new AuthModel { IsAuthenticated = false };

            if (await userManager.FindByEmailAsync(model.Email) is not null)
            {
                authModel.Message = "Email is already registered";
                return authModel;
            }

            if (!await IsUniqueUserNameAsync(model.UserName))
            {
                authModel.Message = "Username is already taken";
                return authModel;
            }

            try
            {
                await CreateUserAsync(new CreateUserDto(
                    organizationId,
                    model.FirstName,
                    model.LastName,
                    model.PhoneNumber,
                    model.UserName,
                    model.Email,
                    model.Password,
                    [])); // roles are granted later by an administrator, never at registration

                var user = await userManager.FindByNameAsync(model.UserName);
                return await BuildAuthModelAsync(user!, "Account created successfully", organizationId);
            }
            catch (IdentityOperationException ex)
            {
                // Registration is a user-facing flow: identity rule violations come back
                // as messages instead of exceptions.
                authModel.Message = ex.Message;
                return authModel;
            }
        }

        public async Task<AuthModel> LoginAsync(LoginModel model)
        {
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user is null)
                return new AuthModel { IsAuthenticated = false, Message = "There is no account with this email" };

            if (!await userManager.CheckPasswordAsync(user, model.Password))
                return new AuthModel { IsAuthenticated = false, Message = "Incorrect email or password" };

            return await BuildAuthModelAsync(user, "You are logged in successfully");
        }

        public async Task<AuthModelMVC> LoginMvcAsync(TokenRequestModel model)
        {
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user is null)
                return new AuthModelMVC { IsAuthenticated = false, Message = "There is no account with this email" };

            if (!await userManager.CheckPasswordAsync(user, model.Password))
                return new AuthModelMVC { IsAuthenticated = false, Message = "Incorrect email or password" };

            return await BuildAuthModelMVCAsync(user, "You are logged in successfully");
        }

        #endregion

        #region Users

        public async Task<UserRegistrationResponseDto> CreateUserAsync(CreateUserDto userDto)
        {
            // 1:1 invariant: the person Party is born together with the user,
            // inside the organization that owns the registration.
            var party = Party.Create(
                userDto.OrganizationId,
                userDto.FirstName,
                userDto.LastName,
                userDto.PhoneNumber);
            await partyRepository.AddAsync(party);

            var user = new ApplicationUser
            {
                UserName = userDto.UserName,
                Email = userDto.Email,
                PartyId = party.Id
            };

            var result = await userManager.CreateAsync(user, userDto.Password);
            if (!result.Succeeded)
                throw new IdentityOperationException(result.Errors);

            if (userDto.Roles is not null && userDto.Roles.Any())
            {
                var addUserRole = await userManager.AddToRolesAsync(user, userDto.Roles);
                if (!addUserRole.Succeeded)
                    throw new IdentityOperationException(addUserRole.Errors);
            }

            return new UserRegistrationResponseDto(result.Succeeded, user.Id, party.Id.Value);
        }

        public async Task<bool> SignInUserAsync(string userName, string password)
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user is null) return false;

            return await userManager.CheckPasswordAsync(user, password);
        }

        public async Task<Guid> GetUserIdAsync(string userName)
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user is null)
                throw new NotFoundException("USER_NOT_FOUND");

            return user.Id;
        }

        public async Task<UserDetailsDto> GetUserDetailsAsync(Guid userId)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                throw new NotFoundException("USER_NOT_FOUND");

            return await BuildUserDetailsAsync(user);
        }

        public async Task<UserDetailsDto> GetUserDetailsByUserNameAsync(string userName)
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user is null)
                throw new NotFoundException("USER_NOT_FOUND");

            return await BuildUserDetailsAsync(user);
        }

        public async Task<string> GetUserNameAsync(Guid userId)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                throw new NotFoundException("USER_NOT_FOUND");

            return user.UserName ?? string.Empty;
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                throw new NotFoundException("USER_NOT_FOUND");

            if (user.UserName is "system" or "admin")
                throw new BadRequestException("You can not delete system or admin user");

            // The person Party row intentionally survives: the same Party may be a
            // Customer/Employee/Supplier — only the identity-account half goes away.
            var result = await userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> IsUniqueUserNameAsync(string userName)
        {
            var user = await userManager.FindByNameAsync(userName);
            return user is null;
        }

        public async Task<List<UserSummaryDto>> GetAllUsersAsync()
        {
            var users = await userManager.Users
                .Select(u => new
                {
                    u.Id,
                    u.PartyId,
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty
                })
                .ToListAsync();

            // Names live on Party now — one batched join instead of a per-user roundtrip.
            var namesByParty = await context.Parties
                .Where(p => users.Select(u => u.PartyId).Contains(p.Id))
                .Select(p => new { p.Id, p.FirstName, p.LastName })
                .ToDictionaryAsync(p => p.Id);

            return users
                .Select(u =>
                {
                    var name = namesByParty.GetValueOrDefault(u.PartyId);
                    return new UserSummaryDto(
                        u.Id,
                        name?.FirstName ?? string.Empty,
                        name?.LastName ?? string.Empty,
                        u.UserName,
                        u.Email);
                })
                .ToList();
        }

        public async Task<List<UserDetailsDto>> GetAllUsersDetailsAsync()
        {
            var users = await userManager.Users.ToListAsync();
            var partyIds = users.Select(u => u.PartyId).ToArray();

            // One batched query per dependency instead of per user (no N+1).
            var memberships = await context.PartyOrganizations
                .Where(po => partyIds.Contains(po.PartyId))
                .Select(po => new { po.PartyId, po.Organization })
                .ToListAsync();

            var organizationsByParty = memberships
                .GroupBy(m => m.PartyId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<Guid>)g.Select(m => m.Organization).ToList());

            var namesByParty = await context.Parties
                .Where(p => partyIds.Contains(p.Id))
                .Select(p => new { p.Id, p.FirstName, p.LastName })
                .ToDictionaryAsync(p => p.Id);

            var userDetails = new List<UserDetailsDto>(users.Count);
            foreach (var user in users)
            {
                var nameRow = namesByParty.TryGetValue(user.PartyId, out var found)
                    ? new PartyNameRow(found.Id, found.FirstName, found.LastName)
                    : null;

                userDetails.Add(await BuildUserDetailsAsync(
                    user,
                    organizationsByParty.GetValueOrDefault(user.PartyId, []),
                    nameRow));
            }

            return userDetails;
        }

        public async Task<bool> UpdateUserProfile(UpdateUserProfileDto profileDto)
        {
            var user = await userManager.FindByIdAsync(profileDto.Id.ToString());
            if (user is null)
                throw new NotFoundException("USER_NOT_FOUND");

            // Names are person data → aggregate boundary is the Party; email stays on the user.
            var party = await partyRepository.GetWithOrganizationsAsync(user.PartyId);
            if (party is null)
                throw new NotFoundException("PARTY_NOT_FOUND");

            party.UpdateName(profileDto.FirstName, profileDto.LastName);
            await unitOfWork.SaveChangesAsync(); // aggregate mutations flush explicitly

            user.Email = profileDto.Email;
            var result = await userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<IReadOnlyList<Guid>> GetUserOrganizationIdsAsync(Guid userId)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                throw new NotFoundException("USER_NOT_FOUND");

            return await context.PartyOrganizations
                .Where(po => po.PartyId == user.PartyId)
                .Select(po => po.Organization)
                .ToListAsync();
        }

        #endregion

        #region Roles

        public async Task<bool> CreateRoleAsync(string roleName)
        {
            var result = await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            if (!result.Succeeded)
                throw new IdentityOperationException(result.Errors);

            return result.Succeeded;
        }

        public async Task<bool> DeleteRoleAsync(Guid roleId)
        {
            var role = await roleManager.FindByIdAsync(roleId.ToString());
            if (role is null)
                throw new NotFoundException("ROLE_NOT_FOUND");

            if (role.Name == "Administrator")
                throw new BadRequestException("You cannot delete Administrator Role");

            var result = await roleManager.DeleteAsync(role);
            if (!result.Succeeded)
                throw new IdentityOperationException(result.Errors);

            return result.Succeeded;
        }

        public async Task<List<RoleDto>> GetRolesAsync()
        {
            return await roleManager.Roles
                .Select(role => new RoleDto(role.Id, role.Name ?? string.Empty))
                .ToListAsync();
        }

        public async Task<RoleDto> GetRoleByIdAsync(Guid id)
        {
            var role = await roleManager.FindByIdAsync(id.ToString());
            if (role is null)
                throw new NotFoundException("ROLE_NOT_FOUND");

            return new RoleDto(role.Id, role.Name ?? string.Empty);
        }

        public async Task<RoleDto> GetRoleByNameAsync(string name)
        {
            var role = await roleManager.FindByNameAsync(name);
            if (role is null)
                throw new NotFoundException("ROLE_NOT_FOUND");

            return new RoleDto(role.Id, role.Name ?? name);
        }

        public async Task<bool> RoleIsExistAsync(string name)
        {
            var role = await roleManager.FindByNameAsync(name);
            return role is not null;
        }

        public async Task<bool> UpdateRole(RoleDto roleDto)
        {
            var role = await roleManager.FindByIdAsync(roleDto.Id.ToString());
            if (role is null)
                throw new NotFoundException("ROLE_NOT_FOUND");

            role.Name = roleDto.RoleName;
            var result = await roleManager.UpdateAsync(role);
            return result.Succeeded;
        }

        #endregion

        #region Users' Roles

        public async Task<bool> IsInRoleAsync(Guid userId, string role)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                throw new NotFoundException("USER_NOT_FOUND");

            return await userManager.IsInRoleAsync(user, role);
        }

        public async Task<List<string>> GetUserRolesAsync(Guid userId)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                throw new NotFoundException("USER_NOT_FOUND");

            var roles = await userManager.GetRolesAsync(user);
            return roles.ToList();
        }

        public async Task<bool> AssignUserToRole(string userName, IList<string> roles)
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user is null)
                throw new NotFoundException("USER_NOT_FOUND");

            var result = await userManager.AddToRolesAsync(user, roles);
            return result.Succeeded;
        }

        public async Task<bool> UpdateUsersRole(string userName, IList<string> usersRole)
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user is null)
                throw new NotFoundException("USER_NOT_FOUND");

            var existingRoles = await userManager.GetRolesAsync(user);
            var removeResult = await userManager.RemoveFromRolesAsync(user, existingRoles);
            if (!removeResult.Succeeded)
                return false;

            var addResult = await userManager.AddToRolesAsync(user, usersRole);
            return addResult.Succeeded;
        }

        #endregion

        #region Private Helpers

        /// <summary>Flat projection target for the batched Party-name lookup.</summary>
        private sealed record PartyNameRow(PartyId Id, string FirstName, string LastName);

        /// <summary>
        /// Assembles the detail DTO: person fields from the linked Party, memberships from
        /// PartyOrganizations, roles from the UserManager store. Every dependency may be
        /// pre-supplied by callers that already batch-loaded them (<see cref="GetAllUsersDetailsAsync"/>).
        /// </summary>
        private async Task<UserDetailsDto> BuildUserDetailsAsync(
            ApplicationUser user,
            IReadOnlyList<Guid>? organizationIds = null,
            PartyNameRow? partyName = null)
        {
            organizationIds ??= await context.PartyOrganizations
                .Where(po => po.PartyId == user.PartyId)
                .Select(po => po.Organization)
                .ToListAsync();

            partyName ??= await context.Parties
                .Where(p => p.Id == user.PartyId)
                .Select(p => new PartyNameRow(p.Id, p.FirstName, p.LastName))
                .FirstOrDefaultAsync();

            var roles = await userManager.GetRolesAsync(user);

            return new UserDetailsDto(
                user.Id,
                organizationIds,
                partyName?.FirstName ?? string.Empty,
                partyName?.LastName ?? string.Empty,
                user.UserName ?? string.Empty,
                user.Email ?? string.Empty,
                roles.ToList());
        }

        /// <summary>Standard AuthModel for an authenticated user, token included.</summary>
        private async Task<AuthModel> BuildAuthModelAsync(ApplicationUser user, string message, Guid? organizationId = null)
        {
            var roles = await userManager.GetRolesAsync(user);

            // Tenancy is resolved from the user's memberships (never from the current
            // request: at login time the caller is still anonymous).
            organizationId ??= (await GetUserOrganizationIdsAsync(user.Id)).FirstOrDefault();

            if (organizationId is null || organizationId == Guid.Empty)
                throw new InvalidOperationException("The user is not linked to any organization.");

            var token = tokenGenerator.GenerateToken(
                new TokenGenerationDto(user.Id, organizationId.Value, roles.FirstOrDefault() ?? string.Empty));

            return new AuthModel
            {
                Message = message,
                IsAuthenticated = true,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList(),
                Token = token,
                // Read the expiry from the token itself — one single source of truth.
                Expiration = new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo
            };
        }

        /// <summary><see cref="AuthModelMVC"/> carries the same data plus the raw user Id.</summary>
        private async Task<AuthModelMVC> BuildAuthModelMVCAsync(ApplicationUser user, string message)
        {
            var authModel = await BuildAuthModelAsync(user, message);

            return new AuthModelMVC
            {
                Message = authModel.Message,
                IsAuthenticated = authModel.IsAuthenticated,
                UserName = authModel.UserName,
                Email = authModel.Email,
                Roles = authModel.Roles,
                UserId = user.Id,
                Token = authModel.Token,
                Expiration = authModel.Expiration
            };
        }

        #endregion
    }
}
