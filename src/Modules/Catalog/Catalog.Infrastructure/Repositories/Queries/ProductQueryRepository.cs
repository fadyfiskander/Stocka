using Catalog.Application.Common.Interfaces;
using Catalog.Contracts.DTOs.Product;
using Catalog.Application.Repositories.Queries.Bases;
using Catalog.Application.Repositories.Queries.Product;
using Catalog.Infrastructure.Contexts;
using Dapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace Catalog.Infrastructure.Repositories.Queries
{
    internal class ProductQueryRepository : QueryRepository, IProductQueryRepository
    {
        public ProductQueryRepository(ICatalogDbConnection connection) : base(connection)
        {

        }

        public async Task<IEnumerable<ProductDto>> GetAllAsync(Guid organizationId)
        {
            var query = "SELECT " +
                        "Id, " +
                        "OrganizationId, " +
                        "Name, " +
                        "Barcode, " +
                        "CategoryId, " +
                        "SellingPrice AS [Price], " +
                        "IsActive, " +
                        "ImagePath " +
                       $"FROM {TableProducts} " +
                         "WHERE OrganizationId = @OrganizationId " +
                         "ORDER BY Name";
            var parameters = new { OrganizationId = organizationId };
            var result = await _connection.QueryAsync<ProductDto>(query, parameters);
            return result;
        }

        public async Task<IEnumerable<ProductDto>> GetByIdsAsync(
            IReadOnlyCollection<Guid> ids,
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            var query = "SELECT Id, OrganizationId, Name, Barcode, CategoryId, SellingPrice AS [Price], IsActive, ImagePath " +
                        $"FROM {TableProducts} WHERE OrganizationId = @OrganizationId AND Id IN @Ids ORDER BY Name";

            var command = new CommandDefinition(
                query,
                new { OrganizationId = organizationId, Ids = ids },
                cancellationToken: cancellationToken);

            return await _connection.QueryAsync<ProductDto>(command);
        }

        public async Task<BuildingBlocks.Application.Common.Models.PaginatedResult<ProductDto>> GetPagedAsync(Guid organizationId, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var countSql = $"SELECT COUNT(*) FROM {TableProducts} WHERE OrganizationId = @OrganizationId";
            var total = await _connection.ExecuteScalarAsync<int>(countSql, new { OrganizationId = organizationId });

            var offset = (page - 1) * pageSize;
            var query = "SELECT " +
                        "Id, OrganizationId, Name, Barcode, CategoryId, SellingPrice AS [Price], IsActive, ImagePath " +
                       $"FROM {TableProducts} WHERE OrganizationId = @OrganizationId ORDER BY Name OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var items = await _connection.QueryAsync<ProductDto>(query, new { OrganizationId = organizationId, Offset = offset, PageSize = pageSize });
            return new BuildingBlocks.Application.Common.Models.PaginatedResult<ProductDto>(items.ToList(), total, page, pageSize);
        }

        public async Task<ProductDto?> GetByIdAsync(Guid id)
        {
            var query = "SELECT " +
                        "Id, " +
                        "OrganizationId, " +
                        "Name, " +               
                        "Barcode, " +            
                        "CategoryId, " +        
                        "SellingPrice AS [Price], " +
                        "IsActive, " +          
                        "ImagePath " +           
                       $"FROM {TableProducts} " +
                        "WHERE Id = @Id";

            var parameters = new { Id = id };
            return await _connection.QueryFirstOrDefaultAsync<ProductDto>(query, parameters);
        }

        public async Task<ProductDto?> GetByNameAsync(string name)
        {
            var query = "SELECT " +
                        "Id, " +
                        "OrganizationId, " +
                        "Name, " +
                        "Barcode, " +
                        "CategoryId, " +
                        "SellingPrice AS [Price], " +
                        "IsActive, " +
                        "ImagePath " +
                       $"FROM {TableProducts} " +
                        "WHERE Name = @Name";
            var parameters = new { Name = name };
            var result = await _connection.QueryFirstOrDefaultAsync<ProductDto>(query, parameters);
            return result;
        }
    }
}
