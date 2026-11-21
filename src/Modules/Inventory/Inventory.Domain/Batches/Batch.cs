using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Bases;
using System.Diagnostics.CodeAnalysis;

namespace Inventory.Domain.Batches
{
    public partial class Batch : AggregateRoot<BatchId>, IMultiTenantEntity
    {
        public Guid PurchaseItemId { get; private set; }

        /// <summary>
        /// Soft reference to the Catalog product this batch contains.
        /// Modules stay decoupled: no FK to Catalog; doc-level references are validated only by convention.
        /// </summary>
        public Guid ProductId { get; private set; }
        public Guid OrganizationId { get; private set; }

        public int InitialQuantity { get; private set; }

        /// <summary>
        /// Quantity received but NOT yet placed into any warehouse (unallocated stock).
        /// Decremented by allocation; adjusted upward only by reversals of allocation.
        /// Distinct from units currently held in warehouses (see WarehouseBatch.Quantity).
        /// </summary>
        public int RemainingQuantity { get; private set; }
        public decimal UnitCost { get; private set; }

        /// <summary>
        /// EF-managed optimistic concurrency token (SQL rowversion).
        /// Guards concurrent quantity updates against lost updates.
        /// </summary>
        public byte[] RowVersion { get; private set; } = null!;

        public DateTime CreatedAt { get; private set; }

        private Batch() { }

        [SetsRequiredMembers]
        public Batch(Guid purchaseItemId, Guid productId, Guid organizationId, int initialQuantity, decimal unitCost)
        {
            Id = BatchId.New();

            if (purchaseItemId == Guid.Empty)
                throw new ArgumentException("Purchase item reference cannot be empty.", nameof(purchaseItemId));

            if (productId == Guid.Empty)
                throw new ArgumentException("Product reference cannot be empty.", nameof(productId));

            if (initialQuantity <= 0)
                throw new ArgumentException("Initial quantity must be greater than zero.", nameof(initialQuantity));

            if (unitCost <= 0)
                throw new ArgumentException("Unit cost must be greater than zero.", nameof(unitCost));

            PurchaseItemId = purchaseItemId;
            ProductId = productId;
            OrganizationId = organizationId;
            InitialQuantity = initialQuantity;
            RemainingQuantity = initialQuantity;
            UnitCost = unitCost;
            CreatedAt = DateTime.UtcNow;
        }

        public void DeductQuantity(int quantityToDeduct)
        {
            if (quantityToDeduct <= 0)
                throw new ArgumentException("Quantity to deduct must be greater than zero.", nameof(quantityToDeduct));

            if (quantityToDeduct > RemainingQuantity)
                throw new InvalidOperationException("Insufficient quantity in batch.");

            RemainingQuantity -= quantityToDeduct;
        }

        public void AddQuantity(int quantityToAdd)
        {
            if (quantityToAdd <= 0)
                throw new ArgumentException("Quantity to add must be greater than zero.", nameof(quantityToAdd));

            RemainingQuantity += quantityToAdd;
        }

        public void UpdateUnitCost(decimal newUnitCost)
        {
            if (newUnitCost <= 0)
                throw new ArgumentException("Unit cost must be greater than zero.", nameof(newUnitCost));

            UnitCost = newUnitCost;
        }

        public bool IsExhausted => RemainingQuantity == 0;

        /// <summary>Total cost of the full initial batch quantity.</summary>
        public decimal GetInitialTotalCost() => InitialQuantity * UnitCost;

        /// <summary>Value of what is currently left in the batch.</summary>
        public decimal GetRemainingValue() => RemainingQuantity * UnitCost;

        Guid IMultiTenantEntity.OrganizationId
        {
            get => OrganizationId;
            set => OrganizationId = value;
        }
    }
}
