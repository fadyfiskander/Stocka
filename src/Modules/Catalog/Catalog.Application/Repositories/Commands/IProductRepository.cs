using BuildingBlocks.Application.Common.Interfaces;
using Catalog.Domain.Products;

namespace Catalog.Application.Repositories.Commands
{
    public interface IProductRepository : IRepository<Product, ProductId>
    {
        Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
