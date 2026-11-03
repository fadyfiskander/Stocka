using BuildingBlocks.Domain.Bases;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Organization.Application.Primitives.Events;
using Organization.Domain.OrganizationMemberships;

namespace Organization.Infrastructure.Contexts
{
    // Alias: the aggregate type clashes with the Organization root namespace.
    using OrganizationAggregate = Organization.Domain.Organizations.Organization;

    public class OrganizationDbContext : DbContext
    {
        private readonly IPublisher _publisher;

        public OrganizationDbContext(
            DbContextOptions<OrganizationDbContext> options,
            IPublisher publisher)
            : base(options)
        {
            _publisher = publisher;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrganizationDbContext).Assembly);
            modelBuilder.HasDefaultSchema("Organization");
        }

        public DbSet<OrganizationAggregate> Organizations { get; set; }
        public DbSet<OrganizationMembership> OrganizationMemberships { get; set; }

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
