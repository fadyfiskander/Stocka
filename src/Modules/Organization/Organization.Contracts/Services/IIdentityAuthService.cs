using Organization.Contracts.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Organization.Contracts.Services
{
    public interface IOrganizationAuthService
    {
        public Task<IEnumerable<OrganizationAccessDto>> OrganizationAccessesAsync(Guid UserId, CancellationToken cancellationToken);
    }
}
