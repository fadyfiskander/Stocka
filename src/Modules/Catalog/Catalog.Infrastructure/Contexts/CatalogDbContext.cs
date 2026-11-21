using BuildingBlocks.Domain.Bases;
using Catalog.Domain.Categories;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Contexts
{
    public class CatalogDbContext : DbContext
    {
        public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
            modelBuilder.HasDefaultSchema("Catalog");
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            DispatchDomainEvents();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            DispatchDomainEvents();
            return base.SaveChanges();
        }

        private void DispatchDomainEvents()
        {
            var aggregates = ChangeTracker.Entries<IAggregateRoot>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();

            var domainEvents = aggregates.SelectMany(e => e.DomainEvents).ToList();

            // TODO: Publish via MediatR / Outbox. For now just clear to avoid memory leak.
            // In production, inject IMediator and publish: foreach(var ev in domainEvents) await mediator.Publish(ev)
            aggregates.ForEach(a => a.ClearDomainEvents());
        }
    }
}