using Inventory.Domain.Entities;
using Inventory.Primitives.Identifiers;

namespace Inventory.Domain.Tests.Entities
{
    public class WarehouseBatchTests
    {
        private static WarehouseBatch NewWb(int quantity = 10) =>
            new(WarehouseId.New(), BatchId.New(), Guid.NewGuid(), quantity);

        [Fact]
        public void Constructor_WithValidData_ShouldSetAllValues()
        {
            var warehouseId = WarehouseId.New();
            var batchId = BatchId.New();
            var organizationId = Guid.NewGuid();

            var wb = new WarehouseBatch(warehouseId, batchId, organizationId, 7);

            Assert.Equal(warehouseId, wb.WarehouseId);
            Assert.Equal(batchId, wb.BatchId);
            Assert.Equal(organizationId, wb.OrganizationId);
            Assert.Equal(7, wb.Quantity);
            Assert.NotEqual(default, wb.Id);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_WithNonPositiveInitialQuantity_ShouldThrow(int quantity)
        {
            Assert.Throws<ArgumentException>(() => new WarehouseBatch(WarehouseId.New(), BatchId.New(), Guid.NewGuid(), quantity));
        }

        [Fact]
        public void AddQuantity_IncreasesQuantity()
        {
            var wb = NewWb(5);
            wb.AddQuantity(3);
            Assert.Equal(8, wb.Quantity);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-4)]
        public void AddQuantity_NonPositive_ShouldThrow(int amount)
        {
            var wb = NewWb();
            Assert.Throws<ArgumentException>(() => wb.AddQuantity(amount));
        }

        [Fact]
        public void DeductQuantity_ReducesQuantity()
        {
            var wb = NewWb(10);
            wb.DeductQuantity(4);
            Assert.Equal(6, wb.Quantity);
        }

        [Fact]
        public void DeductQuantity_MoreThanAvailable_ShouldThrow_AndNotChangeQuantity()
        {
            var wb = NewWb(3);
            Assert.Throws<InvalidOperationException>(() => wb.DeductQuantity(4));
            Assert.Equal(3, wb.Quantity);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void DeductQuantity_NonPositive_ShouldThrow(int amount)
        {
            var wb = NewWb();
            Assert.Throws<ArgumentException>(() => wb.DeductQuantity(amount));
        }

        [Fact]
        public void UpdateQuantity_AllowsZeroButNotNegative()
        {
            var wb = NewWb(10);

            wb.UpdateQuantity(0);
            Assert.Equal(0, wb.Quantity);
            Assert.True(wb.IsEmpty);

            Assert.Throws<ArgumentException>(() => wb.UpdateQuantity(-1));
        }
    }
}
