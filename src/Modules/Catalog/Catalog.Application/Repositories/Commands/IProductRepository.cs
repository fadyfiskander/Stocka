using BuildingBlocks.Application.Common.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.Primitives.Identifiers;

namespace Catalog.Application.Repositories.Commands
{
    public interface IProductRepository : IRepository<Product, ProductId>
    {
        Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
