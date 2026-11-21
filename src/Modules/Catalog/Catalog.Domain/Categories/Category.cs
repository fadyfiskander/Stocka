using BuildingBlocks.Domain.Bases;
using System.Diagnostics.CodeAnalysis;

namespace Catalog.Domain.Categories;

public partial class Category : AggregateRoot<CategoryId>
{
    public Guid OrganizationId { get; private set; }

    public string Name { get; private set; } = null!;

    public bool IsActive { get; private set; }

    private Category() { }

    [SetsRequiredMembers]
    public Category(Guid organizationId, string name)
    {
        Id = CategoryId.New();

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty.", nameof(name));

        OrganizationId = organizationId;
        Name = name.Trim();
        IsActive = true;
    }

    public static Category Create(Guid organizationId, string name)
    {
        return new Category(organizationId, name);
    }

    public void UpdateActive(bool value) => IsActive = value;

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Category name cannot be empty.", nameof(newName));

        Name = newName.Trim();
    }
}