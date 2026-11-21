using Catalog.Application.Repositories.Commands;
using Catalog.Domain.Categories;
using Catalog.Infrastructure.Contexts;
using Catalog.Infrastructure.Repositories.Commands.Bases;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories.Commands
{
    internal class CategoryRepository : Repository<Category, CategoryId>, ICategoryRepository
    {
        private readonly CatalogDbContext _context;

        public CategoryRepository(CatalogDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken) > 0;
    }
}