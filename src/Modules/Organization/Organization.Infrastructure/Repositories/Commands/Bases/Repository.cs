using BuildingBlocks.Application.Common.Interfaces;
using BuildingBlocks.Domain.Bases;
using Microsoft.EntityFrameworkCore;
using Organization.Infrastructure.Contexts;

namespace Organization.Infrastructure.Repositories.Commands.Bases
{
    /// <summary>
    /// EF Core write-side repository base.
    /// <para>
    /// Concrete repositories inherit the standard CRUD lifetime here and may:
    /// <list type="bullet">
    ///   <item>override <c>GetByIdAsync</c> / <c>AddAsync</c> / ... when the aggregate needs
    ///   extra care (e.g. eager-loading navigations — see <c>OrganizationRepository</c>);</item>
    ///   <item>use <see cref="DbSet{TEntity}"/> for composed queries (Includes, projections)
    ///   that belong to the aggregate's own consistency.</item>
    /// </list>
    /// </para>
    /// </summary>
    internal class Repository<T, TKey> : IRepository<T, TKey> where T : Entity<TKey>
    {
        // protected so concrete repositories can add entity-specific EF queries.
        protected readonly DbSet<T> _set;

        public Repository(OrganizationDbContext context)
        {
            _set = context.Set<T>();
        }

        // `e.Id == id` is illegal on an unconstrained TKey (CS0019), and EqualityComparer<TKey>.Default.Equals is not
        // translatable by EF Core. Instance `.Equals(id)` IS translated correctly through the value converter.
        /// <summary>Virtual so aggregate repositories can override (e.g. to include aggregative navigations).</summary>
        public virtual async Task<T?> GetByIdAsync(TKey id)
            => await _set.FirstOrDefaultAsync(e => e.Id!.Equals(id));

        public virtual Task AddAsync(T model)
        {
            _set.Add(model);
            return Task.CompletedTask;
        }

        public virtual Task UpdateAsync(T model)
        {
            _set.Update(model);
            return Task.CompletedTask;
        }

        public virtual Task RemoveAsync(T model)
        {
            _set.Remove(model);
            return Task.CompletedTask;
        }
    }
}
