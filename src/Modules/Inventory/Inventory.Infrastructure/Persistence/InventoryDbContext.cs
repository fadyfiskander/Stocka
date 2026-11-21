using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Bases;
using Inventory.Application.Primitives.Events;
using Inventory.Domain.Batches;
using Inventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence
{
    public class InventoryDbContext : DbContext
    {
        private readonly IPublisher _publisher;
        private readonly ICurrentUserService _currentUser;

        public InventoryDbContext(
            DbContextOptions<InventoryDbContext> options,
            IPublisher publisher,
            ICurrentUserService currentUser)
            : base(options)
        {
            _publisher = publisher;
            _currentUser = currentUser;
        }

        // Read per query execution, NOT baked into the cached model - so each
        // DI scope (its own ICurrentUserService) gets correct organization isolation.
        private Guid CurrentOrganizationId
        {
            get
            {
                var organizationId = _currentUser.OrganizationId;
                if (organizationId is null)
                    throw new ArgumentNullException("OrganizationId is null, OrganizationId cannot be null");
                return organizationId.Value;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
            modelBuilder.HasDefaultSchema("Inventory");

            // Global multi-tenant query filters — every EF read/write query is
            // automatically scoped to the current organization.
            modelBuilder.Entity<Warehouse>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId);
            modelBuilder.Entity<Batch>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId);
            modelBuilder.Entity<WarehouseBatch>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId);
            modelBuilder.Entity<StockMovement>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId);
        }

        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Batch> Batches { get; set; }
        public DbSet<WarehouseBatch> WarehouseBatches { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Events are published AFTER a successful save, so asynchronous handlers
            // can observe persisted state. (A transactional outbox is a future improvement.)
            var result = await base.SaveChangesAsync(cancellationToken);
            await DispatchDomainEventsAsync(cancellationToken);
            return result;
        }

        public override int SaveChanges()
            => SaveChangesAsync().GetAwaiter().GetResult();

        private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
        {
            var aggregates = ChangeTracker.Entries<IAggregateRoot>()
                .Where(e => e.Entity.DomainEvents.Count > 0)
                .Select(e => e.Entity)
                .ToList();

            foreach (var aggregate in aggregates)
            {
                var events = aggregate.DomainEvents.ToList();
                aggregate.ClearDomainEvents();

                foreach (var domainEvent in events)
                {
                    var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
                    var notification = (INotification?)Activator.CreateInstance(notificationType, domainEvent);
                    if (notification != null)
                        await _publisher.Publish(notification, cancellationToken);
                }
            }
        }
    }
}
