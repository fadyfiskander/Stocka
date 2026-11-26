using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Models;
using Catalog.Application.Common.Interfaces;
using Catalog.Application.Repositories.Queries.Bases;
using Catalog.Application.Repositories.Queries.Category;
using Catalog.Contracts.DTOs.Category;
using Dapper;

namespace Catalog.Infrastructure.Repositories.Queries;

internal sealed class CategoryQueryRepository : QueryRepository, ICategoryQueryRepository
{
    private const string CategoryColumns =
        """
        Id,
        OrganizationId,
        Name,
        IsActive
        """;

    public CategoryQueryRepository(
        ICatalogDbConnection connection,
        ICurrentUserService currentUser)
        : base(connection, currentUser)
    {
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT {CategoryColumns}
            FROM {TableCategories}
            WHERE OrganizationId = @OrganizationId
            ORDER BY Name, Id
            """;

        var command = new CommandDefinition(
            sql,
            new { OrganizationId },
            cancellationToken: cancellationToken);

        var result = await _connection.QueryAsync<CategoryDto>(command);

        return result.ToList();
    }

    public async Task<PaginatedResult<CategoryDto>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var offset = (long)(page - 1) * pageSize;

        var countSql = $"""
            SELECT COUNT_BIG(*)
            FROM {TableCategories}
            WHERE OrganizationId = @OrganizationId
            """;

        var countCommand = new CommandDefinition(
            countSql,
            new { OrganizationId },
            cancellationToken: cancellationToken);

        var total = await _connection.ExecuteScalarAsync<long>(countCommand);

        var querySql = $"""
            SELECT {CategoryColumns}
            FROM {TableCategories}
            WHERE OrganizationId = @OrganizationId
            ORDER BY Name, Id
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY
            """;

        var queryCommand = new CommandDefinition(
            querySql,
            new { OrganizationId, Offset = offset, PageSize = pageSize },
            cancellationToken: cancellationToken);

        var items = await _connection.QueryAsync<CategoryDto>(queryCommand);

        return new PaginatedResult<CategoryDto>(
            items.ToList(),
            total,
            page,
            pageSize);
    }

    public async Task<CategoryDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT {CategoryColumns}
            FROM {TableCategories}
            WHERE OrganizationId = @OrganizationId
              AND Id = @Id
            """;

        var command = new CommandDefinition(
            sql,
            new { OrganizationId, Id = id },
            cancellationToken: cancellationToken);

        return await _connection.QueryFirstOrDefaultAsync<CategoryDto>(command);
    }

    public async Task<CategoryDto?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT {CategoryColumns}
            FROM {TableCategories}
            WHERE OrganizationId = @OrganizationId
              AND Name = @Name
            """;

        var command = new CommandDefinition(
            sql,
            new { OrganizationId, Name = name },
            cancellationToken: cancellationToken);

        return await _connection.QueryFirstOrDefaultAsync<CategoryDto>(command);
    }
}