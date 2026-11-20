using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Application.Primitives.Events
{
    public abstract class DomainEvent : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        protected DomainEvent() { }
    }
}
