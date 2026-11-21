namespace Catalog.Domain.Products;

public readonly record struct ProductId(Guid Value)
{
    public static ProductId New() => new(Guid.NewGuid());

    public static ProductId FromGuid(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty.", nameof(value));

        return new(value);
    }

    public override string ToString() => Value.ToString();
}