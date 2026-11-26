using BuildingBlocks.Application.Common.Models;
using Catalog.Contracts.DTOs.Category;

namespace Catalog.Application.Repositories.Queries.Category;

public interface ICategoryQueryRepository
{
    Task<CategoryDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<CategoryDto?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CategoryDto>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<PaginatedResult<CategoryDto>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}