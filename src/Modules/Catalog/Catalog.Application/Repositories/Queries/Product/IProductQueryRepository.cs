using BuildingBlocks.Application.Common.Models;
using Catalog.Contracts.DTOs.Product;

namespace Catalog.Application.Repositories.Queries.Product
{
    public interface IProductQueryRepository
    {
        Task<ProductDto?> GetByIdAsync(Guid id);
        Task<ProductDto?> GetByNameAsync(string name);
        Task<IEnumerable<ProductDto>> GetAllAsync(Guid organizationId);
        Task<PaginatedResult<ProductDto>> GetPagedAsync(Guid organizationId, int page, int pageSize);
    }
}