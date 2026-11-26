using Catalog.Contracts.DTOs.Product;
using Catalog.Domain.Products;

namespace Catalog.Application.DTOs.Product
{
    /// <summary>
    /// Maps Catalog domain products onto the public contract DTO (Catalog.Contracts).
    /// </summary>
    public static class ProductMapper
    {
        public static ProductDto ToDto(this Catalog.Domain.Products.Product product)
            => new(
                product.Id.Value,
                product.Name,
                product.Barcode ?? string.Empty,
                product.CategoryId.Value,
                product.SellingPrice,
                product.IsActive,
                product.ImagePath ?? string.Empty);
    }
}
