namespace Inventory.Domain.StockMovements;

public readonly record struct StockMovementId(Guid Value)
{
    public static StockMovementId New() => new(Guid.NewGuid());

    public static StockMovementId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("StockMovementId cannot be empty.", nameof(value));

        return new(value);
    }

    public override string ToString() => Value.ToString();
}