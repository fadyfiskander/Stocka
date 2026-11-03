using BuildingBlocks.Application.Common.Models;
using Dapper;
using Organization.Application.Common.Interfaces;
using Organization.Application.Repositories.Queries;
using Organization.Contracts.DTOs;
using Organization.Infrastructure.Repositories.Queries.Bases;

namespace Organization.Infrastructure.Repositories.Queries
{
    /// <summary>
    /// Read side over the <c>Organization</c> root table.
    /// Membership rows per organization are batch-loaded and attached, so an organization
    /// travels with its <c>MemberUserIds</c> exactly like the aggregate would.
    /// </summary>
    internal sealed class OrganizationQueryRepository : QueryRepository, IOrganizationQueryRepository
    {
        private const string BaseSelect =
            "SELECT O.Id, O.Name, O.Description, O.Slug, O.LogoUrl FROM " + TableOrganizations + " AS O";

        public OrganizationQueryRepository(IOrganizationDbConnection connection) : base(connection)
        {
        }

        public async Task<OrganizationDto?> GetByIdAsync(Guid id)
        {
            var sql = $"{BaseSelect} WHERE O.Id = @Id";
            var row = await _connection.QueryFirstOrDefaultAsync<OrganizationRow>(sql, new { Id = id });
            return row is null ? null : await AttachMembersAsync(row);
        }

        public async Task<OrganizationDto?> GetBySlugAsync(string slug)
        {
            var sql = $"{BaseSelect} WHERE O.Slug = @Slug";
            var row = await _connection.QueryFirstOrDefaultAsync<OrganizationRow>(sql, new { Slug = slug });
            return row is null ? null : await AttachMembersAsync(row);
        }

        public async Task<IEnumerable<OrganizationDto>> GetByNameAsync(string name)
        {
            // Parameterized contains-search; SQL wildcards from caller input are escaped.
            var pattern = "%" + EscapeLike(name) + "%";
            var sql = $"{BaseSelect} WHERE O.Name LIKE @Pattern ESCAPE '\\'";
            var rows = await _connection.QueryAsync<OrganizationRow>(sql, new { Pattern = pattern });
            return await AttachMembersAsync(rows.AsList());
        }

        public async Task<PaginatedResult<OrganizationDto>> GetPagedAsync(int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var countSql = $"SELECT COUNT(*) FROM {TableOrganizations}";
            var total = await _connection.ExecuteScalarAsync<int>(countSql);

            var offset = (page - 1) * pageSize;
            var sql = $"{BaseSelect} ORDER BY O.Name OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var rows = await _connection.QueryAsync<OrganizationRow>(sql, new { Offset = offset, PageSize = pageSize });

            return new PaginatedResult<OrganizationDto>(
                await AttachMembersAsync(rows.AsList()), total, page, pageSize);
        }

        /// <summary>Flat projection target — the DTO is assembled once memberships are attached.</summary>
        private sealed record OrganizationRow(Guid Id, string Name, string Description, string Slug, string? LogoUrl);

        /// <summary>Single-organization attach: one extra query for its member user ids.</summary>
        private async Task<OrganizationDto> AttachMembersAsync(OrganizationRow row)
        {
            var memberUserIds = (await _connection.QueryAsync<Guid>(
                $"SELECT UserId FROM {TableOrganizationMemberships} WHERE OrganizationId = @OrganizationId",
                new { OrganizationId = row.Id })).AsList();

            return new OrganizationDto(row.Id, row.Name, row.Description, row.Slug, row.LogoUrl, memberUserIds);
        }

        /// <summary>Batch attach for lists/pages: ONE extra query total, grouped in memory.</summary>
        private async Task<IReadOnlyList<OrganizationDto>> AttachMembersAsync(IReadOnlyList<OrganizationRow> rows)
        {
            if (rows.Count == 0)
                return [];

            var organizationIds = rows.Select(r => r.Id).ToArray();
            var memberships = await _connection.QueryAsync<(Guid OrganizationId, Guid UserId)>(
                $"SELECT OrganizationId, UserId FROM {TableOrganizationMemberships} WHERE OrganizationId IN @OrganizationIds",
                new { OrganizationIds = organizationIds });

            var membersByOrganization = memberships
                .GroupBy(m => m.OrganizationId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<Guid>)g.Select(m => m.UserId).ToList());

            return rows
                .Select(r => new OrganizationDto(
                    r.Id,
                    r.Name,
                    r.Description,
                    r.Slug,
                    r.LogoUrl,
                    membersByOrganization.GetValueOrDefault(r.Id, [])))
                .ToList();
        }
    }
}
