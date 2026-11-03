using BuildingBlocks.Application.Common.Interfaces;
using BuildingBlocks.Domain.Bases;
using Catalog.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories.Commands.Bases
{
    internal class Repository<T, TKey> : IRepository<T, TKey> where T : Entity<TKey>

    {
        private readonly DbSet<T> _context;

        public Repository(CatalogDbContext context)
        {
            _context = context.Set<T>();
        }

        // `e.Id == id` is illegal on an unconstrained TKey (CS0019), and EqualityComparer<TKey>.Default.Equals is not
        // translatable by EF Core. Instance `.Equals(id)` IS translated correctly through the value converter.
        public async Task<T?> GetByIdAsync(TKey id)
            => await _context.FirstOrDefaultAsync(e => e.Id!.Equals(id));

        public async Task AddAsync(T model)
        {
            _context.Add(model);
        }

        public async Task RemoveAsync(T model)
        {
            _context.Remove(model);
        }

        public async Task UpdateAsync(T model)
        {
            _context.Update(model);
        }
    }
}