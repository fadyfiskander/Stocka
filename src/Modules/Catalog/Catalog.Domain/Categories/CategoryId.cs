namespace Catalog.Domain.Categories;

public readonly record struct CategoryId(Guid Value)
{
    public static CategoryId New() => new(Guid.NewGuid());

    public static CategoryId FromGuid(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty.", nameof(value));

        return new(value);
    }

    public override string ToString() => Value.ToString();
}