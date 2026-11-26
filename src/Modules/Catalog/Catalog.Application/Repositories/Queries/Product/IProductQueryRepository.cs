using BuildingBlocks.Application.Common.Models;
using Catalog.Contracts.DTOs.Product;

namespace Catalog.Application.Repositories.Queries.Product
{
    public interface IProductQueryRepository
    {
        Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken);
        Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<IReadOnlyList<ProductDto>> GetByNameAsync(string name, CancellationToken cancellationToken);
        Task<IReadOnlyList<ProductDto>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
        Task<PaginatedResult<ProductDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken);

        Task<IReadOnlyList<ProductDto>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken);
        Task<ProductDto?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken);
    }
}