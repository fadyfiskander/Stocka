namespace Inventory.Domain.Warehouses;

public readonly record struct WarehouseBatchId(Guid Value)
{
    public static WarehouseBatchId New() => new(Guid.NewGuid());

    public static WarehouseBatchId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("WarehouseBatchId cannot be empty.", nameof(value));

        return new(value);
    }

    public override string ToString() => Value.ToString();
}