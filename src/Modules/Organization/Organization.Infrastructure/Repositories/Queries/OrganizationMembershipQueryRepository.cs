using Dapper;
using Organization.Application.Common.Interfaces;
using Organization.Application.Repositories.Queries;
using Organization.Contracts.DTOs;
using Organization.Infrastructure.Repositories.Queries.Bases;

namespace Organization.Infrastructure.Repositories.Queries
{
    /// <summary>
    /// Read side over the <c>OrganizationMembership</c> rows — the (OrganizationId, UserId)
    /// join table resolving who belongs to which organization.
    /// </summary>
    internal sealed class OrganizationMembershipQueryRepository : QueryRepository, IOrganizationMembershipQueryRepository
    {
        private const string BaseQuery = $"SELECT om.OrganizationId, o.Name, om.Role FROM {TableOrganizationMemberships} om";
        public OrganizationMembershipQueryRepository(IOrganizationDbConnection connection) : base(connection)
        {
        }

        public async Task<IEnumerable<OrganizationMembershipDto>> GetByOrganizationAsync(Guid organizationId)
        {
            var sql = BaseQuery + " LEFT JOIN Organization.Organization o ON om.OrganizationId = o.Id" +
                      " WHERE OrganizationId = @OrganizationId ORDER BY UserId";
            return await _connection.QueryAsync<OrganizationMembershipDto>(
                sql, new { OrganizationId = organizationId });
        }

        public async Task<IEnumerable<OrganizationMembershipDto>> GetByUserAsync(Guid userId)
        {
            var sql = BaseQuery + " LEFT JOIN Organization.Organization o ON om.OrganizationId = o.Id" + 
                      " WHERE UserId = @UserId ORDER BY OrganizationId";
            return await _connection.QueryAsync<OrganizationMembershipDto>(sql, new { UserId = userId });
        }

        public async Task<bool> ExistsAsync(Guid organizationId, Guid userId)
        {
            var sql = $"SELECT CAST(CASE WHEN EXISTS (" +
                      $"SELECT 1 FROM {TableOrganizationMemberships} " +
                      "WHERE OrganizationId = @OrganizationId AND UserId = @UserId) " +
                      "THEN 1 ELSE 0 END AS BIT)";
            return await _connection.ExecuteScalarAsync<bool>(
                sql, new { OrganizationId = organizationId, UserId = userId });
        }
    }
}
