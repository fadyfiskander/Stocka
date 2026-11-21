using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Bases;
using Inventory.Domain.Batches;
using Inventory.Domain.Entities;
using Inventory.Primitives.Identifiers;
using System.Diagnostics.CodeAnalysis;

namespace Inventory.Domain.Entities;

public partial class WarehouseBatch : Entity<WarehouseBatchId>, IMultiTenantEntity
{
    public WarehouseId WarehouseId { get; private set; }
    public BatchId BatchId { get; private set; }
    public int Quantity { get; private set; }

    /// <summary>
    /// EF-managed optimistic concurrency token (SQL rowversion).
    /// Guards concurrent quantity updates against lost updates.
    /// </summary>
    public byte[] RowVersion { get; private set; } = null!;

    public Guid OrganizationId { get; private set; }

    public virtual Warehouse Warehouse { get; private set; } = null!;

    private WarehouseBatch() { }

    [SetsRequiredMembers]
    public WarehouseBatch(WarehouseId warehouseId, BatchId batchId, Guid organizationId, int quantity)
    {
        Id = WarehouseBatchId.New();

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        WarehouseId = warehouseId;
        BatchId = batchId;
        OrganizationId = organizationId;
        Quantity = quantity;
    }

    public void AddQuantity(int quantityToAdd)
    {
        if (quantityToAdd <= 0)
            throw new ArgumentException("Quantity to add must be greater than zero.", nameof(quantityToAdd));

        Quantity += quantityToAdd;
    }

    /// <summary>
    /// 0 is valid here (batch fully consumed in that warehouse) — intentionally different
    /// from the constructor, which requires a positive initial quantity.
    /// </summary>
    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity < 0)
            throw new ArgumentException("Quantity cannot be negative.", nameof(newQuantity));

        Quantity = newQuantity;
    }

    public void DeductQuantity(int quantityToDeduct)
    {
        if (quantityToDeduct <= 0)
            throw new ArgumentException("Quantity to deduct must be greater than zero.", nameof(quantityToDeduct));

        if (quantityToDeduct > Quantity)
            throw new InvalidOperationException("Insufficient quantity in warehouse batch.");

        Quantity -= quantityToDeduct;
    }

    public bool IsEmpty => Quantity == 0;

    Guid IMultiTenantEntity.OrganizationId
    {
        get => OrganizationId;
        set => OrganizationId = value;
    }
}
