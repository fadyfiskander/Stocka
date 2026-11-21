using Inventory.Domain.Batches;
using Inventory.Domain.Warehouses;

namespace Inventory.Domain.Tests.Entities
{
    public class WarehouseTests
    {
        private static Guid Organization() => Guid.NewGuid();

        [Fact]
        public void Create_WithValidData_ShouldSetActiveActiveDefault()
        {
            var organizationId = Organization();
            var warehouse = Warehouse.Create(organizationId, "Main Store", WarehouseType.Shop, "Downtown", "Primary location");

            Assert.Equal(organizationId, warehouse.OrganizationId);
            Assert.Equal("Main Store", warehouse.Name);
            Assert.Equal("Downtown", warehouse.Location);
            Assert.Equal("Primary location", warehouse.Description);
            Assert.Equal(WarehouseType.Shop, warehouse.Type);
            Assert.True(warehouse.IsActive);
            Assert.NotEqual(default, warehouse.Id);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithNullOrWhitespaceName_ShouldThrow(string? name)
        {
            Assert.Throws<ArgumentException>(() => Warehouse.Create(Organization(), name!, WarehouseType.Shop, "Location"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithNullOrWhitespaceLocation_ShouldThrow(string? location)
        {
            Assert.Throws<ArgumentException>(() => Warehouse.Create(Organization(), "Name", WarehouseType.Shop, location!));
        }

        [Fact]
        public void Create_WithNameOver50Chars_ShouldThrow()
        {
            var name = new string('x', 51);
            Assert.Throws<ArgumentException>(() => Warehouse.Create(Organization(), name, WarehouseType.Shop, "Location"));
        }

        [Fact]
        public void Create_WithLocationOver100Chars_ShouldThrow()
        {
            var location = new string('x', 101);
            Assert.Throws<ArgumentException>(() => Warehouse.Create(Organization(), "Name", WarehouseType.Shop, location));
        }

        [Fact]
        public void Create_WithDescriptionOver600Chars_ShouldThrow()
        {
            var description = new string('x', 601);
            Assert.Throws<ArgumentException>(() => Warehouse.Create(Organization(), "Name", WarehouseType.Shop, "Location", description));
        }

        [Fact]
        public void Create_WithWhitespaceOnly_ShouldTrimValues()
        {
            var warehouse = Warehouse.Create(Organization(), "  Trailing Name  ", WarehouseType.Storage, "  Some Street  ", "  desc  ");
            Assert.Equal("Trailing Name", warehouse.Name);
            Assert.Equal("Some Street", warehouse.Location);
            Assert.Equal("desc", warehouse.Description);
        }

        [Fact]
        public void UpdateName_ChangesName_AndGuards()
        {
            var warehouse = Warehouse.Create(Organization(), "Old", WarehouseType.Shop, "Location");
            warehouse.Rename("New Name");
            Assert.Equal("New Name", warehouse.Name);

            Assert.Throws<ArgumentException>(() => warehouse.Rename("  "));
            var longName = new string('x', 51);
            Assert.Throws<ArgumentException>(() => warehouse.Rename(longName));
        }

        [Fact]
        public void UpdateLocation_AndDescription_ChangeValues()
        {
            var warehouse = Warehouse.Create(Organization(), "Name", WarehouseType.Shop, "Old");
            warehouse.UpdateLocation("New");
            warehouse.UpdateDescription(null);
            Assert.Equal("New", warehouse.Location);
            Assert.Null(warehouse.Description);

            warehouse.UpdateDescription("some");
            Assert.Equal("some", warehouse.Description);
        }

        [Fact]
        public void Activate_Deactivate_AreIdempotent()
        {
            var warehouse = Warehouse.Create(Organization(), "A warehouse name", WarehouseType.Shop, "Location");

            warehouse.Deactivate();
            Assert.False(warehouse.IsActive);
            warehouse.Deactivate();
            Assert.False(warehouse.IsActive);

            warehouse.Activate();
            Assert.True(warehouse.IsActive);
            warehouse.Activate();
            Assert.True(warehouse.IsActive);
        }

        [Fact]
        public void ChangeType_UpdatesType()
        {
            var warehouse = Warehouse.Create(Organization(), "A warehouse name", WarehouseType.Shop, "Location");
            warehouse.ChangeType(WarehouseType.Storage);
            Assert.Equal(WarehouseType.Storage, warehouse.Type);
        }

        [Fact]
        public void AddBatch_WithBatchFromAnotherWarehouse_ShouldThrow()
        {
            var warehouse = Warehouse.Create(Organization(), "A name", WarehouseType.Shop, "Location");
            var foreignWb = new WarehouseBatch(WarehouseId.New(), BatchId.New(), Organization(), 5);

            Assert.Throws<ArgumentException>(() => warehouse.AddBatch(foreignWb));
        }

        [Fact]
        public void AddBatch_Null_ShouldThrow()
        {
            var warehouse = Warehouse.Create(Organization(), "A name", WarehouseType.Shop, "Location");
            WarehouseBatch nullBatch = null!;
            Assert.Throws<ArgumentNullException>(() => warehouse.AddBatch(nullBatch));
        }

        [Fact]
        public void AddBatch_DuplicateId_ShouldThrow()
        {
            var warehouse = Warehouse.Create(Organization(), "A name", WarehouseType.Shop, "Location");
            var wb = warehouse.AddBatch(BatchId.New(), Organization(), 5);

            Assert.Throws<InvalidOperationException>(() => warehouse.AddBatch(wb));
        }

        [Fact]
        public void UpdateBatchQuantity_delegatesToWarehouseBatch()
        {
            var warehouse = Warehouse.Create(Organization(), "A name", WarehouseType.Shop, "Location");
            var batchId = BatchId.New();
            var wb = warehouse.AddBatch(batchId, Organization(), 5);

            warehouse.UpdateBatchQuantity(wb.Id, 2);
            Assert.Equal(2, wb.Quantity);
        }
    }
}
