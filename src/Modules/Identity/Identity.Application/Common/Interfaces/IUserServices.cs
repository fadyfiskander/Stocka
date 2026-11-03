using Identity.Application.DTOs.Identity.Users.Login;
using Identity.Contracts.DTOs;

namespace Identity.Application.Common.Interfaces
{
    public interface IUserServices
    {
        Task<AuthenticateResult> GetAuthenticateResultAsync(string email, string password, CancellationToken cancellationToken);

        Task<IEnumerable<RoleDto>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken);
    }
}