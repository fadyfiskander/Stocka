using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Bases;
using Inventory.Domain.Enums;
using Inventory.Domain.Batches;
using Inventory.Domain.Entities;
using Inventory.Primitives.Identifiers;
using System.Diagnostics.CodeAnalysis;

namespace Inventory.Domain.Entities
{
    public class StockMovement : AggregateRoot<StockMovementId>, IMultiTenantEntity
    {
        public Guid ProductId { get; private set; }
        public BatchId BatchId { get; private set; }
        public WarehouseId WarehouseId { get; private set; }
        public Guid OrganizationId { get; private set; }

        public int Quantity { get; private set; }

        public StockMovementType MovementType { get; private set; }

        public StockReferenceType? ReferenceType { get; private set; }
        public Guid? ReferenceId { get; private set; }

        /// <summary>
        /// Free-text reason (e.g. physical-count corrections, damage description, reversal note).
        /// Optional: required for adjustment-type operations and reversals at the application level.
        /// Max length mirrors the database column (NVARCHAR(300)).
        /// </summary>
        public string? Reason { get; private set; }

        public DateTime CreatedAt { get; private set; }

        private StockMovement() { } // For EF

        [SetsRequiredMembers]
        public StockMovement(
            Guid productId,
            BatchId batchId,
            WarehouseId warehouseId,
            Guid organizationId,
            int quantity,
            StockMovementType movementType,
            StockReferenceType? referenceType = null,
            Guid? referenceId = null,
            string? reason = null)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

            if (batchId.Value == Guid.Empty)
                throw new ArgumentException("Batch reference cannot be empty.", nameof(batchId));

            if (warehouseId.Value == Guid.Empty)
                throw new ArgumentException("Warehouse reference cannot be empty.", nameof(warehouseId));

            if (!ReferencePolicy.IsAllowed(movementType, referenceType))
                throw new ArgumentException(
                    $"Reference type '{referenceType}' is not supported for movement type '{movementType}'.",
                    nameof(referenceType));

            if (referenceId == Guid.Empty)
                referenceId = null;

            if (!string.IsNullOrWhiteSpace(reason))
            {
                var trimmed = reason!.Trim();
                if (trimmed.Length > ReasonMaxLength)
                    throw new ArgumentException($"Reason cannot exceed {ReasonMaxLength} characters.", nameof(reason));
                Reason = trimmed;
            }

            Id = StockMovementId.New();

            ProductId = productId;
            BatchId = batchId;
            WarehouseId = warehouseId;
            OrganizationId = organizationId;

            Quantity = quantity;
            MovementType = movementType;

            ReferenceType = referenceType;
            ReferenceId = referenceId;

            CreatedAt = DateTime.UtcNow;
        }

        // ✅ اتجاه الحركة
        public bool IsInbound =>
            MovementType == StockMovementType.PurchaseIn ||
            MovementType == StockMovementType.TransferIn ||
            MovementType == StockMovementType.AdjustmentIn ||
            MovementType == StockMovementType.OrderReturn;

        public bool IsOutbound =>
            MovementType == StockMovementType.SaleOut ||
            MovementType == StockMovementType.AdjustmentOut ||
            MovementType == StockMovementType.TransferOut;

        /// <summary>Mirror of the database column limit (NVARCHAR(300)).</summary>
        public const int ReasonMaxLength = 300;

        /// <summary>
        /// Supported movement/reference combinations (domain policy).
        /// - AdjustmentX: Adjustment, or ReversalOfMovement (reversal corrections).
        /// - OrderReturn: Return (the original sale/order it came back from) — or Order when
        ///   the caller only knows the order document. Correction combos, per business agreement.
        /// </summary>
        public static class ReferencePolicy
        {
            private static readonly Dictionary<StockMovementType, StockReferenceType[]> _allowed = new()
            {
                [StockMovementType.PurchaseIn] = [StockReferenceType.Purchase],
                [StockMovementType.SaleOut] = [StockReferenceType.Order],
                [StockMovementType.TransferIn] = [StockReferenceType.Transfer],
                [StockMovementType.TransferOut] = [StockReferenceType.Transfer],
                [StockMovementType.AdjustmentIn] = [StockReferenceType.Adjustment, StockReferenceType.ReversalOfMovement],
                [StockMovementType.AdjustmentOut] = [StockReferenceType.Adjustment, StockReferenceType.ReversalOfMovement],
                [StockMovementType.OrderReturn] = [StockReferenceType.Return, StockReferenceType.Order],
            };

            public static bool IsAllowed(StockMovementType movementType, StockReferenceType? referenceType) =>
                referenceType is null || (_allowed.TryGetValue(movementType, out var allowed) && allowed.Contains(referenceType.Value));
        }

        public int SignedQuantity =>
            IsInbound ? Quantity : -Quantity;

        public void UpdateQuantity(int newQuantity)
        {
            if (newQuantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(newQuantity));

            Quantity = newQuantity;
        }

        public void UpdateReference(StockReferenceType? referenceType, Guid? referenceId)
        {
            ReferenceType = referenceType;
            ReferenceId = referenceId;
        }

        Guid IMultiTenantEntity.OrganizationId
        {
            get => OrganizationId;
            set => OrganizationId = value;
        }
    }
}
