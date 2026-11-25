using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Models;
using Dapper;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.Warehouse;
using Inventory.Domain.Warehouses;
using Inventory.Infrastructure.Repositories.Queries.Bases;

namespace Inventory.Infrastructure.Repositories.Queries
{
    internal class WarehouseQueryRepository : QueryRepository, IWarehouseQueryRepository
    {
        private const string BaseSelect =
            "SELECT Id, OrganizationId, Name, Location, Description, IsActive, Type FROM " + TableWarehouses;

        public WarehouseQueryRepository(IInventoryDbConnection connection, ICurrentUserService currentUser)
            : base(connection, currentUser)
        {
        }

        public async Task<PaginatedResult<WarehouseDto>> GetPagedAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return await GetPagedCoreAsync(null, page, pageSize, cancellationToken);
        }

        public async Task<PaginatedResult<WarehouseDto>> GetPagedAsync(
            bool? isActive,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return await GetPagedCoreAsync(isActive, page, pageSize, cancellationToken);
        }

        private async Task<PaginatedResult<WarehouseDto>> GetPagedCoreAsync(
            bool? isActive,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var isActiveFilter = isActive.HasValue ? " AND IsActive = @IsActive" : string.Empty;
            var dp = new DynamicParameters();
            dp.Add("OrganizationId", OrganizationId);
            if (isActive.HasValue)
                dp.Add("IsActive", isActive.Value);

            var countSql = $"SELECT COUNT(*) FROM {TableWarehouses} WHERE OrganizationId = @OrganizationId{isActiveFilter}";
            var countCommand = new CommandDefinition(
                countSql,
                dp,
                cancellationToken: cancellationToken);

            var total = await _connection.ExecuteScalarAsync<int>(countCommand);

            var offset = (page - 1) * pageSize;
            dp.Add("Offset", offset);
            dp.Add("PageSize", pageSize);

            var sql = $"{BaseSelect} WHERE OrganizationId = @OrganizationId{isActiveFilter} ORDER BY Name OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var itemsCommand = new CommandDefinition(
                sql,
                dp,
                cancellationToken: cancellationToken);

            var items = await _connection.QueryAsync<WarehouseDto>(itemsCommand);
            return new PaginatedResult<WarehouseDto>(items.ToList(), total, page, pageSize);
        }

        public async Task<WarehouseDto?> GetByIdAsync(
            WarehouseId id,
            CancellationToken cancellationToken = default)
        {
            var sql = $"{BaseSelect} WHERE Id = @Id AND OrganizationId = @OrganizationId";
            var command = new CommandDefinition(
                sql,
                new { Id = id.Value, OrganizationId },
                cancellationToken: cancellationToken);

            return await _connection.QueryFirstOrDefaultAsync<WarehouseDto>(command);
        }

        public async Task<IEnumerable<WarehouseDto>> GetByNameAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            // Parameterized contains-search; SQL wildcards from caller input are escaped.
            var pattern = "%" + EscapeLike(name) + "%";
            var sql = $"{BaseSelect} WHERE OrganizationId = @OrganizationId AND Name LIKE @Pattern ESCAPE '\\'";
            var command = new CommandDefinition(
                sql,
                new { OrganizationId, Pattern = pattern },
                cancellationToken: cancellationToken);

            return await _connection.QueryAsync<WarehouseDto>(command);
        }

        public async Task<WarehouseDto?> GetByExactNameAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            var sql = $"{BaseSelect} WHERE OrganizationId = @OrganizationId AND Name = @Name";
            var command = new CommandDefinition(
                sql,
                new { OrganizationId, Name = name },
                cancellationToken: cancellationToken);

            return await _connection.QueryFirstOrDefaultAsync<WarehouseDto>(command);
        }

        private static string EscapeLike(string value) =>
            value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
    }
}