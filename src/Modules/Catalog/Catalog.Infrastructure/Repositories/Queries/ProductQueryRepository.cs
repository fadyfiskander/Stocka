using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Models;
using Catalog.Application.Common.Interfaces;
using Catalog.Application.Repositories.Queries.Bases;
using Catalog.Application.Repositories.Queries.Product;
using Catalog.Contracts.DTOs.Product;
using Dapper;

internal sealed class ProductQueryRepository : QueryRepository, IProductQueryRepository
{
    private const string ProductColumns =
        """
        Id,
        Name,
        Barcode,
        CategoryId,
        SellingPrice AS [Price],
        IsActive,
        ImagePath
        """;

    public ProductQueryRepository(
        ICatalogDbConnection connection,
        ICurrentUserService currentUser)
        : base(connection, currentUser)
    {
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT {ProductColumns}
            FROM {TableProducts}
            WHERE OrganizationId = @OrganizationId
            ORDER BY Name, Id
            """;

        var command = new CommandDefinition(
            sql,
            new { OrganizationId },
            cancellationToken: cancellationToken);

        var result = await _connection.QueryAsync<ProductDto>(command);

        return result.ToList();
    }

    public async Task<IReadOnlyList<ProductDto>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids is null || ids.Count == 0)
            return [];

        var sql = $"""
            SELECT {ProductColumns}
            FROM {TableProducts}
            WHERE OrganizationId = @OrganizationId
              AND Id IN @Ids
            ORDER BY Name, Id
            """;

        var command = new CommandDefinition(
            sql,
            new { OrganizationId, Ids = ids },
            cancellationToken: cancellationToken);

        var result = await _connection.QueryAsync<ProductDto>(command);

        return result.ToList();
    }

    public async Task<PaginatedResult<ProductDto>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var offset = (long)(page - 1) * pageSize;

        var countSql = $"""
            SELECT COUNT_BIG(*)
            FROM {TableProducts}
            WHERE OrganizationId = @OrganizationId
            """;

        var countCommand = new CommandDefinition(
            countSql,
            new { OrganizationId },
            cancellationToken: cancellationToken);

        var total = await _connection.ExecuteScalarAsync<long>(countCommand);

        var querySql = $"""
            SELECT {ProductColumns}
            FROM {TableProducts}
            WHERE OrganizationId = @OrganizationId
            ORDER BY Name, Id
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY
            """;

        var queryCommand = new CommandDefinition(
            querySql,
            new { OrganizationId, Offset = offset, PageSize = pageSize },
            cancellationToken: cancellationToken);

        var items = await _connection.QueryAsync<ProductDto>(queryCommand);

        return new PaginatedResult<ProductDto>(
            items.ToList(),
            total,
            page,
            pageSize);
    }

    public async Task<ProductDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT {ProductColumns}
            FROM {TableProducts}
            WHERE OrganizationId = @OrganizationId
              AND Id = @Id
            """;

        var command = new CommandDefinition(
            sql,
            new { OrganizationId, Id = id },
            cancellationToken: cancellationToken);

        return await _connection.QueryFirstOrDefaultAsync<ProductDto>(command);
    }

    public async Task<IReadOnlyList<ProductDto>> GetByNameAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT {ProductColumns}
            FROM {TableProducts}
            WHERE OrganizationId = @OrganizationId
              AND Name = @Name
            ORDER BY Name, Id
            """;

        var command = new CommandDefinition(
            sql,
            new { OrganizationId, Name = name },
            cancellationToken: cancellationToken);

        var result = await _connection.QueryAsync<ProductDto>(command);

        return result.ToList();
    }

    public async Task<IReadOnlyList<ProductDto>> GetByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT {ProductColumns}
            FROM {TableProducts}
            WHERE OrganizationId = @OrganizationId
              AND CategoryId = @CategoryId
            ORDER BY Name, Id
            """;

        var command = new CommandDefinition(
            sql,
            new { OrganizationId, CategoryId = categoryId },
            cancellationToken: cancellationToken);

        var result = await _connection.QueryAsync<ProductDto>(command);

        return result.ToList();
    }

    public async Task<ProductDto?> GetByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT {ProductColumns}
            FROM {TableProducts}
            WHERE OrganizationId = @OrganizationId
              AND Barcode = @Barcode
            """;

        var command = new CommandDefinition(
            sql,
            new { OrganizationId, Barcode = barcode },
            cancellationToken: cancellationToken);

        return await _connection.QueryFirstOrDefaultAsync<ProductDto>(command);
    }
}
