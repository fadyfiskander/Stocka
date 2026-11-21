using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Domain.StockMovements
{
    public enum StockReferenceType
    {
        Purchase,
        Order,
        Transfer,
        Adjustment,
        Return,

        /// <summary>Points at another StockMovement (correction of a previously recorded movement).</summary>
        ReversalOfMovement
    }
}
