using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Bases;
using Inventory.Domain.Enums;
using Inventory.Primitives.Identifiers;
using System.Diagnostics.CodeAnalysis;

namespace Inventory.Domain.Entities
{
    public partial class Warehouse : AggregateRoot<WarehouseId>, IMultiTenantEntity
    {
        // Mirror the database column limits (NVARCHAR(50)/(100)/(600)) so domain guards fail first.
        private const int NameMaxLength = 50;
        private const int LocationMaxLength = 100;
        private const int DescriptionMaxLength = 600;

        public Guid OrganizationId { get; private set; }

        public string Name { get; private set; } = null!;

        public string Location { get; private set; } = null!;
        public string? Description { get; private set; }

        public bool IsActive { get; private set; }

        public WarehouseType Type { get; private set; }

        private readonly List<WarehouseBatch> _warehouseBatches = new();

        public virtual ICollection<WarehouseBatch> WarehouseBatches => _warehouseBatches.AsReadOnly();

        private Warehouse() { }

        [SetsRequiredMembers]
        private Warehouse(Guid organizationId, string name, WarehouseType type, string location, string? description = null)
        {
            Id = WarehouseId.New();
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Warehouse name cannot be empty.", nameof(name));

            if (name.Trim().Length > NameMaxLength)
                throw new ArgumentException($"Warehouse name cannot exceed {NameMaxLength} characters.", nameof(name));

            if (string.IsNullOrWhiteSpace(location))
                throw new ArgumentException("Warehouse location cannot be empty.", nameof(location));

            if (location.Trim().Length > LocationMaxLength)
                throw new ArgumentException($"Warehouse location cannot exceed {LocationMaxLength} characters.", nameof(location));

            if (description?.Trim().Length > DescriptionMaxLength)
                throw new ArgumentException($"Warehouse description cannot exceed {DescriptionMaxLength} characters.", nameof(description));

            OrganizationId = organizationId;
            Name = name.Trim();
            Location = location.Trim();
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            Type = type;
            IsActive = true;
        }

        public static Warehouse Create(Guid organizationId, string name, WarehouseType type, string location, string? description = null)
        {
            return new Warehouse(organizationId, name, type, location, description);
        }

        public void Rename(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Warehouse name cannot be empty.", nameof(newName));

            if (newName.Trim().Length > NameMaxLength)
                throw new ArgumentException($"Warehouse name cannot exceed {NameMaxLength} characters.", nameof(newName));

            Name = newName.Trim();
        }

        public void ChangeType(WarehouseType newType)
        {
            Type = newType;
        }

        public void UpdateLocation(string newLocation)
        {
            if (string.IsNullOrWhiteSpace(newLocation))
                throw new ArgumentException("Warehouse location cannot be empty.", nameof(newLocation));

            if (newLocation.Trim().Length > LocationMaxLength)
                throw new ArgumentException($"Warehouse location cannot exceed {LocationMaxLength} characters.", nameof(newLocation));

            Location = newLocation.Trim();
        }

        public void UpdateDescription(string? newDescription)
        {
            if (newDescription != null && newDescription.Trim().Length > DescriptionMaxLength)
                throw new ArgumentException($"Warehouse description cannot exceed {DescriptionMaxLength} characters.", nameof(newDescription));

            Description = string.IsNullOrWhiteSpace(newDescription)
                ? null
                : newDescription.Trim();
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void AddBatch(WarehouseBatch batch)
        {
            if (batch == null)
                throw new ArgumentNullException(nameof(batch));

            if (batch.WarehouseId != Id)
                throw new ArgumentException("Warehouse batch does not belong to this warehouse.");

            if (_warehouseBatches.Any(wb => wb.Id == batch.Id))
                throw new InvalidOperationException("Warehouse batch already added.");

            _warehouseBatches.Add(batch);
        }

        public WarehouseBatch AddBatch(BatchId batchId, Guid organizationId, int quantity)
        {
            var batch = new WarehouseBatch(Id, batchId, organizationId, quantity);
            AddBatch(batch);
            return batch;
        }

        public void UpdateBatchQuantity(WarehouseBatchId batchId, int newQuantity)
        {
            var batch = _warehouseBatches.FirstOrDefault(wb => wb.Id == batchId);
            if (batch == null)
                throw new ArgumentException("Warehouse batch not found.");

            batch.UpdateQuantity(newQuantity);
        }

        public void RemoveBatch(WarehouseBatchId batchId)
        {
            var batch = _warehouseBatches.FirstOrDefault(wb => wb.Id == batchId);
            if (batch == null)
                throw new ArgumentException("Warehouse batch not found.");

            _warehouseBatches.Remove(batch);
        }

        Guid IMultiTenantEntity.OrganizationId
        {
            get => OrganizationId;
            set => OrganizationId = value;
        }
    }
}
