using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Application.Primitives.Events
{
    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
    }
}
