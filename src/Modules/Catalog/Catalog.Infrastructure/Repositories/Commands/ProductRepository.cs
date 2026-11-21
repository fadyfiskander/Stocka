using Catalog.Application.Repositories.Commands;
using Catalog.Domain.Products;
using Catalog.Infrastructure.Contexts;
using Catalog.Infrastructure.Repositories.Commands.Bases;

namespace Catalog.Infrastructure.Repositories.Commands
{
    internal class ProductRepository : Repository<Product, ProductId>, IProductRepository
    {
        private readonly CatalogDbContext _context;

        public ProductRepository(CatalogDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken) > 0;
        
    }
}
