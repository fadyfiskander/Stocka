namespace Catalog.Domain.Common.Abstractions;

public abstract class DomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    protected DomainEvent() { }
}
