namespace Inventory.Domain.Warehouses;

public readonly record struct WarehouseId(Guid Value)
{
    public static WarehouseId New() => new(Guid.NewGuid());

    public static WarehouseId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("WarehouseId cannot be empty.", nameof(value));

        return new(value);
    }

    public override string ToString() => Value.ToString();
}