using Inventory.Domain.Batches;
using Inventory.Domain.Entities;
using Inventory.Primitives.Identifiers;
using Inventory.Domain.Enums;

namespace Inventory.Domain.Tests.Entities
{
    public class StockMovementTests
    {
        private static (Guid ProductId, BatchId Batch, WarehouseId Warehouse, Guid OrganizationId) Ctx() =>
            (Guid.NewGuid(), BatchId.New(), WarehouseId.New(), Guid.NewGuid());

        private static StockMovement New(
            StockMovementType type,
            StockReferenceType? reference = null,
            Guid? referenceId = null,
            int quantity = 5,
            string? reason = null)
        {
            var (productId, batch, warehouse, organizationId) = Ctx();
            return new StockMovement(productId, batch, warehouse, organizationId, quantity, type, reference, referenceId, reason);
        }

        [Fact]
        public void Constructor_WithValidData_ShouldSetValues()
        {
            var m = New(StockMovementType.SaleOut, StockReferenceType.Order, Guid.NewGuid());

            Assert.Equal(5, m.Quantity);
            Assert.Equal(StockMovementType.SaleOut, m.MovementType);
            Assert.Equal(StockReferenceType.Order, m.ReferenceType);
            Assert.NotEqual(default, m.Id);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-7)]
        public void Constructor_WithNonPositiveQuantity_ShouldThrow(int quantity)
        {
            Assert.Throws<ArgumentException>(() => New(StockMovementType.AdjustmentIn, quantity: quantity));
        }

        [Fact]
        public void Constructor_WithEmptyBatchOrWarehouse_ShouldThrow()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var (productId, _, warehouse, organizationId) = Ctx();
                return new StockMovement(productId, new BatchId(Guid.Empty), warehouse, organizationId, 1, StockMovementType.AdjustmentIn);
            });

            Assert.Throws<ArgumentException>(() =>
            {
                var (productId, batch, _, organizationId) = Ctx();
                return new StockMovement(productId, batch, new WarehouseId(Guid.Empty), organizationId, 1, StockMovementType.AdjustmentIn);
            });
        }

        [Theory]
        [InlineData(StockMovementType.PurchaseIn, StockReferenceType.Adjustment)]
        [InlineData(StockMovementType.SaleOut, StockReferenceType.Purchase)]
        [InlineData(StockMovementType.TransferIn, StockReferenceType.Order)]
        [InlineData(StockMovementType.AdjustmentOut, StockReferenceType.Order)]
        [InlineData(StockMovementType.OrderReturn, StockReferenceType.Adjustment)]
        public void Constructor_WithInvalidReferenceCombo_ShouldThrow(StockMovementType movement, StockReferenceType reference)
        {
            Assert.Throws<ArgumentException>(() => New(movement, reference));
        }

        [Theory]
        [InlineData(StockMovementType.PurchaseIn, StockReferenceType.Purchase)]
        [InlineData(StockMovementType.SaleOut, StockReferenceType.Order)]
        [InlineData(StockMovementType.TransferIn, StockReferenceType.Transfer)]
        [InlineData(StockMovementType.TransferOut, StockReferenceType.Transfer)]
        [InlineData(StockMovementType.AdjustmentIn, StockReferenceType.Adjustment)]
        [InlineData(StockMovementType.AdjustmentIn, StockReferenceType.ReversalOfMovement)]
        [InlineData(StockMovementType.AdjustmentOut, StockReferenceType.Adjustment)]
        [InlineData(StockMovementType.AdjustmentOut, StockReferenceType.ReversalOfMovement)]
        [InlineData(StockMovementType.OrderReturn, StockReferenceType.Return)]
        [InlineData(StockMovementType.OrderReturn, StockReferenceType.Order)]
        public void Constructor_WithValidReferenceCombo_ShouldNotThrow(StockMovementType movement, StockReferenceType reference)
        {
            var m = New(movement, reference);
            Assert.Equal(reference, m.ReferenceType);
        }

        [Fact]
        public void Constructor_WithNullReference_ShouldNotThrow()
        {
            var m = New(StockMovementType.AdjustmentIn, reference: null);
            Assert.Null(m.ReferenceType);
        }

        [Fact]
        public void Reason_IsTrimmedAndStored()
        {
            var m = New(StockMovementType.AdjustmentIn, reason: "  physical count correction  ");
            Assert.Equal("physical count correction", m.Reason);
        }

        [Fact]
        public void Reason_Over300Chars_ShouldThrow()
        {
            var reason = new string('x', StockMovement.ReasonMaxLength + 1);
            Assert.Throws<ArgumentException>(() => New(StockMovementType.AdjustmentIn, reason: reason));
        }

        [Fact]
        public void Directions_AreConsistent()
        {
            Assert.True(New(StockMovementType.PurchaseIn).IsInbound);
            Assert.True(New(StockMovementType.TransferIn).IsInbound);
            Assert.True(New(StockMovementType.AdjustmentIn).IsInbound);
            Assert.True(New(StockMovementType.OrderReturn).IsInbound);

            Assert.True(New(StockMovementType.SaleOut).IsOutbound);
            Assert.True(New(StockMovementType.TransferOut).IsOutbound);
            Assert.True(New(StockMovementType.AdjustmentOut).IsOutbound);

            var purchase = New(StockMovementType.PurchaseIn);
            Assert.True(purchase.IsInbound);
            Assert.False(purchase.IsOutbound);
        }

        [Fact]
        public void SignedQuantity_PositiveForInboundNegativeForOutbound()
        {
            Assert.Equal(5, New(StockMovementType.PurchaseIn, quantity: 5).SignedQuantity);
            Assert.Equal(-5, New(StockMovementType.SaleOut, quantity: 5).SignedQuantity);
            Assert.Equal(-5, New(StockMovementType.AdjustmentOut, quantity: 5).SignedQuantity);
        }

        [Fact]
        public void UpdateQuantity_GuardsAndUpdates()
        {
            var m = New(StockMovementType.AdjustmentIn, quantity: 2);
            m.UpdateQuantity(9);
            Assert.Equal(9, m.Quantity);

            Assert.Throws<ArgumentException>(() => m.UpdateQuantity(0));
            Assert.Throws<ArgumentException>(() => m.UpdateQuantity(-1));
            Assert.Equal(9, m.Quantity);
        }
    }
}
