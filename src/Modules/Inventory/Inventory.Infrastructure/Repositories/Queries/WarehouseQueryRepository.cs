using Dapper;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.Warehouse;
using Inventory.Infrastructure.Repositories.Queries.Bases;
using Inventory.Domain.Warehouses;
using BuildingBlocks.Application.Common.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Repositories.Queries
{
    internal class WarehouseQueryRepository : QueryRepository, IWarehouseQueryRepository
    {
        private const string BaseSelect =
            "SELECT Id, OrganizationId, Name, Location, Description, IsActive, Type FROM " + TableWarehouses;

        public WarehouseQueryRepository(IInventoryDbConnection connection) : base(connection)
        {
        }

        public async Task<IEnumerable<WarehouseDto>> GetAllAsync(Guid organizationId)
        {
            var sql = $"{BaseSelect} WHERE OrganizationId = @OrganizationId ORDER BY Name";
            return await _connection.QueryAsync<WarehouseDto>(sql, new { OrganizationId = organizationId });
        }

        public async Task<PaginatedResult<WarehouseDto>> GetPagedAsync(Guid organizationId, int page, int pageSize)
        {
            return await GetPagedCoreAsync(organizationId, null, page, pageSize);
        }

        public async Task<PaginatedResult<WarehouseDto>> GetPagedAsync(Guid organizationId, bool? isActive, int page, int pageSize)
        {
            return await GetPagedCoreAsync(organizationId, isActive, page, pageSize);
        }

        private async Task<PaginatedResult<WarehouseDto>> GetPagedCoreAsync(Guid organizationId, bool? isActive, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var isActiveFilter = isActive.HasValue ? " AND IsActive = @IsActive" : string.Empty;
            var countParams = new { OrganizationId = organizationId, IsActive = isActive };

            var countSql = $"SELECT COUNT(*) FROM {TableWarehouses} WHERE OrganizationId = @OrganizationId{isActiveFilter}";
            var total = await _connection.ExecuteScalarAsync<int>(countSql, countParams);

            var offset = (page - 1) * pageSize;
            var sql = $"{BaseSelect} WHERE OrganizationId = @OrganizationId{isActiveFilter} ORDER BY Name OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var items = await _connection.QueryAsync<WarehouseDto>(sql, new { OrganizationId = organizationId, IsActive = isActive, Offset = offset, PageSize = pageSize });

            return new PaginatedResult<WarehouseDto>(items.ToList(), total, page, pageSize);
        }

        public async Task<WarehouseDto?> GetByIdAsync(Guid organizationId, WarehouseId id)
        {
            var sql = $"{BaseSelect} WHERE Id = @Id AND OrganizationId = @OrganizationId";
            return await _connection.QueryFirstOrDefaultAsync<WarehouseDto>(sql, new { Id = id.Value, OrganizationId = organizationId });
        }

        public async Task<IEnumerable<WarehouseDto>> GetByNameAsync(Guid organizationId, string name)
        {
            // Parameterized contains-search; SQL wildcards from caller input are escaped.
            var pattern = "%" + EscapeLike(name) + "%";
            var sql = $"{BaseSelect} WHERE OrganizationId = @OrganizationId AND Name LIKE @Pattern ESCAPE '\\'";
            return await _connection.QueryAsync<WarehouseDto>(sql, new { OrganizationId = organizationId, Pattern = pattern });
        }

        public async Task<WarehouseDto?> GetByExactNameAsync(Guid organizationId, string name)
        {
            var sql = $"{BaseSelect} WHERE OrganizationId = @OrganizationId AND Name = @Name";
            return await _connection.QueryFirstOrDefaultAsync<WarehouseDto>(sql, new { OrganizationId = organizationId, Name = name });
        }

        private static string EscapeLike(string value) =>
            value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
    }
}
