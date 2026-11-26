using Catalog.Contracts.DTOs.Category;
using Catalog.Contracts.DTOs.Product;
using Catalog.Contracts.Services;
using Catalog.Contracts.UseCases.CategoryCases;
using Catalog.Contracts.UseCases.ProductCases;

namespace Catalog.Application.Services
{
    /// <summary>
    /// Public cross-module adapter: the single entry point to the Catalog module.
    /// Delegates each operation to its use-case contract implementation, which
    /// dispatches the MediatR pipeline (validation and unit-of-work included).
    /// Contains no business logic.
    /// </summary>
    public class CatalogServices : ICatalogServices
    {
        private readonly IProductCases _productCases;
        private readonly ICategoryCases _categoryCases;

        public CatalogServices(IProductCases productCases, ICategoryCases categoryCases)
        {
            _productCases = productCases;
            _categoryCases = categoryCases;
        }

        #region Products
        public Task<ProductDto?> RegisterProductAsync(RegisterProductCommand command, CancellationToken cancellationToken = default)
            => _productCases.RegisterProductAsync(command, cancellationToken);

        public Task<ProductDto> UpdateProductAsync(UpdateProductCommand command, CancellationToken cancellationToken = default)
            => _productCases.UpdateProductAsync(command, cancellationToken);

        public Task<ProductDto> ActivateProductAsync(ActivateProductCommand command, CancellationToken cancellationToken = default)
            => _productCases.ActivateProductAsync(command, cancellationToken);

        public Task<ProductDto> DeactivateProductAsync(DeactivateProductCommand command, CancellationToken cancellationToken = default)
            => _productCases.DeactivateProductAsync(command, cancellationToken);

        public Task<ProductDto> RetrieveProductAsync(Guid productId, CancellationToken cancellationToken = default)
            => _productCases.RetrieveProductAsync(new RetrieveProductQuery(productId), cancellationToken);

        public Task<IEnumerable<ProductDto>> RetrieveProductByNameAsync(string name, CancellationToken cancellationToken = default)
            => _productCases.RetrieveProductByNameAsync(new RetrieveProductByNameQuery(name), cancellationToken);

        public Task<IEnumerable<ProductDto>> ListProductsAsync(CancellationToken cancellationToken = default)
            => _productCases.ListProductsAsync(cancellationToken);

        public Task<IEnumerable<ProductDto>> GetProductsByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default)
            => _productCases.ListProductsByIdsAsync(new ListProductsByIdsQuery(ids), cancellationToken);
        #endregion

        #region Categories
        public Task<CategoryDto?> RegisterCategoryAsync(RegisterCategoryCommand command, CancellationToken cancellationToken = default)
            => _categoryCases.RegisterCategoryAsync(command, cancellationToken);

        public Task<CategoryDto> UpdateCategoryAsync(UpdateCategoryCommand command, CancellationToken cancellationToken = default)
            => _categoryCases.UpdateCategoryAsync(command, cancellationToken);

        public Task<CategoryDto> ActivateCategoryAsync(ActivateCategoryCommand command, CancellationToken cancellationToken = default)
            => _categoryCases.ActivateCategoryAsync(command, cancellationToken);

        public Task<CategoryDto> DeactivateCategoryAsync(DeactivateCategoryCommand command, CancellationToken cancellationToken = default)
            => _categoryCases.DeactivateCategoryAsync(command, cancellationToken);

        public Task<CategoryDto> RetrieveCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
            => _categoryCases.RetrieveCategoryAsync(new RetrieveCategoryQuery(categoryId), cancellationToken);

        public Task<CategoryDto> RetrieveCategoryByNameAsync(string name, CancellationToken cancellationToken = default)
            => _categoryCases.RetrieveCategoryByNameAsync(new RetrieveCategoryByNameQuery(name), cancellationToken);

        public Task<IEnumerable<CategoryDto>> ListCategoriesAsync(Guid organizationId = default, CancellationToken cancellationToken = default)
            => _categoryCases.ListCategoriesAsync(new ListCategoriesQuery { OrganizationId = organizationId }, cancellationToken);

        #endregion
    }
}
