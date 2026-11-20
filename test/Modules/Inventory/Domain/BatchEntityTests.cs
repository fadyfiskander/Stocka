using Inventory.Domain.Entities;

namespace Inventory.Domain.Tests.Entities
{
    public class BatchTests
    {
        private static Batch NewBatch(int initial = 100, decimal cost = 10m) =>
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), initial, cost);

        [Fact]
        public void Constructor_WithValidData_ShouldSetRemainingEqualToInitial()
        {
            var purchaseItemId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var batch = new Batch(purchaseItemId, productId, Guid.NewGuid(), 50, 7.5m);

            Assert.Equal(purchaseItemId, batch.PurchaseItemId);
            Assert.Equal(productId, batch.ProductId);
            Assert.Equal(50, batch.InitialQuantity);
            Assert.Equal(50, batch.RemainingQuantity);
            Assert.Equal(7.5m, batch.UnitCost);
            Assert.NotEqual(default, batch.Id);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Constructor_WithNonPositiveInitialQuantity_ShouldThrow(int quantity)
        {
            Assert.Throws<ArgumentException>(() => new Batch(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), quantity, 1m));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_WithNonPositiveUnitCost_ShouldThrow(decimal cost)
        {
            Assert.Throws<ArgumentException>(() => new Batch(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10, cost));
        }

        [Fact]
        public void Constructor_WithEmptyPurchaseItemId_ShouldThrow()
        {
            Assert.Throws<ArgumentException>(() => new Batch(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), 10, 1m));
        }

        [Fact]
        public void Constructor_WithEmptyProductId_ShouldThrow()
        {
            Assert.Throws<ArgumentException>(() => new Batch(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), 10, 1m));
        }

        [Fact]
        public void DeductQuantity_ReducesRemaining()
        {
            var batch = NewBatch(100);
            batch.DeductQuantity(30);

            Assert.Equal(70, batch.RemainingQuantity);
            Assert.Equal(100, batch.InitialQuantity); // never mutated
        }

        [Fact]
        public void DeductQuantity_MoreThanRemaining_ShouldThrow()
        {
            var batch = NewBatch(10);
            Assert.Throws<InvalidOperationException>(() => batch.DeductQuantity(11));
            Assert.Equal(10, batch.RemainingQuantity); // unchanged
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public void DeductQuantity_NonPositive_ShouldThrow(int amount)
        {
            var batch = NewBatch();
            Assert.Throws<ArgumentException>(() => batch.DeductQuantity(amount));
        }

        [Fact]
        public void AddQuantity_IncreasesRemaining()
        {
            var batch = NewBatch(10);
            batch.AddQuantity(5);
            Assert.Equal(15, batch.RemainingQuantity);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        public void AddQuantity_NonPositive_ShouldThrow(int amount)
        {
            var batch = NewBatch();
            Assert.Throws<ArgumentException>(() => batch.AddQuantity(amount));
        }

        [Fact]
        public void IsExhausted_TrueOnlyWhenNothingLeft()
        {
            var batch = NewBatch(5);
            Assert.False(batch.IsExhausted);

            batch.DeductQuantity(5);
            Assert.True(batch.IsExhausted);
        }

        [Fact]
        public void UpdateUnitCost_GuardsAndUpdates()
        {
            var batch = NewBatch(cost: 4m);
            batch.UpdateUnitCost(6m);
            Assert.Equal(6m, batch.UnitCost);

            Assert.Throws<ArgumentException>(() => batch.UpdateUnitCost(0m));
            Assert.Throws<ArgumentException>(() => batch.UpdateUnitCost(-1m));
        }

        [Fact]
        public void CostHelpers_ReflectRecordedUnitCost()
        {
            var batch = NewBatch(initial: 10, cost: 2m);
            batch.DeductQuantity(4);

            Assert.Equal(20m, batch.GetInitialTotalCost());
            Assert.Equal(12m, batch.GetRemainingValue());
        }
    }
}
