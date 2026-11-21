namespace Inventory.Domain.Batches;

public readonly record struct BatchId(Guid Value)
{
    public static BatchId New() => new(Guid.NewGuid());

    public static BatchId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("BatchId cannot be empty.", nameof(value));

        return new(value);
    }

    public override string ToString() => Value.ToString();
}