using BuildingBlocks.Domain.Bases;
using Catalog.Domain.Categories;
using Catalog.Domain.Primitives;
using Catalog.Domain.Primitives.Identifiers;
using System.Diagnostics.CodeAnalysis;

namespace Catalog.Domain.Entities
{
    public partial class Product : AggregateRoot<ProductId>
    {
        private Product()
        {
            // Required by EF Core
        }

        [SetsRequiredMembers]
        private Product(
            Guid organizationId,
            CategoryId categoryId,
            string name,
            decimal sellingPrice,
            string? barcode)
        {
            Id = ProductId.New();

            ChangeOrganization(organizationId);
            ChangeCategory(categoryId);
            Rename(name);
            ChangeSellingPrice(sellingPrice);
            ChangeBarcode(barcode);

            IsActive = true;
        }

        #region Properties

        public Guid OrganizationId { get; private set; } = default!;

        public CategoryId CategoryId { get; private set; } = default!;

        public string Name { get; private set; } = string.Empty;

        public decimal SellingPrice { get; private set; }

        public string? Barcode { get; private set; }

        public string? ImagePath { get; private set; }

        public bool IsActive { get; private set; }

        #endregion

        #region Navigation Properties

        #endregion

        #region Factory

        public static Product Create(
            Guid organizationId,
            CategoryId categoryId,
            string name,
            decimal sellingPrice,
            string? barcode = null)
        {
            return new Product(
                organizationId,
                categoryId,
                name,
                sellingPrice,
                barcode);
        }

        #endregion

        #region Business Methods

        public void Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Product name cannot be empty.", nameof(name));
            //AddDomainEvent(new ProductRenamedEvent(Id, Name, name.Trim()));
            Name = name.Trim();
        }

        public void ChangeSellingPrice(decimal sellingPrice)
        {
            if (sellingPrice < 0)
                throw new ArgumentOutOfRangeException(nameof(sellingPrice));

            SellingPrice = sellingPrice;
        }

        public void ChangeOrganization(Guid organizationId)
        {
            OrganizationId = organizationId;
        }

        public void ChangeCategory(CategoryId categoryId)
        {
            CategoryId = categoryId;
        }

        public void ChangeBarcode(string? barcode)
        {
            Barcode = string.IsNullOrWhiteSpace(barcode)
                ? null
                : barcode.Trim();
        }

        public void ChangeImage(string? imagePath)
        {
            ImagePath = string.IsNullOrWhiteSpace(imagePath)
                ? null
                : imagePath.Trim();
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        #endregion
    }
}