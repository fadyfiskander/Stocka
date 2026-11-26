using Catalog.Contracts.DTOs.Category;
using Catalog.Contracts.DTOs.Product;
using Catalog.Contracts.UseCases.CategoryCases;
using Catalog.Contracts.UseCases.ProductCases;

namespace Catalog.Contracts.Services
{
    /// <summary>
    /// Public Catalog module contract: the single entry point for interacting with the module.
    /// Every operation is dispatched through the product/category use-case contracts, so the
    /// MediatR pipeline (validation and unit-of-work behaviors) always runs.
    /// </summary>
    public interface ICatalogServices
    {
        #region Products
        /// <summary>Registers a new product for the current Organization.</summary>
        Task<ProductDto?> RegisterProductAsync(RegisterProductCommand command, CancellationToken cancellationToken = default);

        /// <summary>Applies a partial update; only provided fields change.</summary>
        Task<ProductDto> UpdateProductAsync(UpdateProductCommand command, CancellationToken cancellationToken = default);

        /// <summary>Activates an existing product.</summary>
        Task<ProductDto> ActivateProductAsync(ActivateProductCommand command, CancellationToken cancellationToken = default);

        /// <summary>Deactivates an existing product (non-destructive).</summary>
        Task<ProductDto> DeactivateProductAsync(DeactivateProductCommand command, CancellationToken cancellationToken = default);

        /// <summary>Retrieves one product by ID.</summary>
        Task<ProductDto> RetrieveProductAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>Retrieves one product by its exact name.</summary>
        Task<IEnumerable<ProductDto>> RetrieveProductByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>Retrieves the product listing for the current (or explicitly requested) Organization.</summary>
        Task<IEnumerable<ProductDto>> ListProductsAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<ProductDto>> GetProductsByIdsAsync(List<Guid> Ids, CancellationToken cancellationToken = default);
        #endregion

        #region Categories
        /// <summary>Registers a new category for the current Organization.</summary>
        Task<CategoryDto?> RegisterCategoryAsync(RegisterCategoryCommand command, CancellationToken cancellationToken = default);

        /// <summary>Renames an existing category.</summary>
        Task<CategoryDto> UpdateCategoryAsync(UpdateCategoryCommand command, CancellationToken cancellationToken = default);

        /// <summary>Activates an existing category.</summary>
        Task<CategoryDto> ActivateCategoryAsync(ActivateCategoryCommand command, CancellationToken cancellationToken = default);

        /// <summary>Deactivates an existing category (non-destructive).</summary>
        Task<CategoryDto> DeactivateCategoryAsync(DeactivateCategoryCommand command, CancellationToken cancellationToken = default);

        /// <summary>Retrieves one category by ID.</summary>
        Task<CategoryDto> RetrieveCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

        /// <summary>Retrieves one category by its exact name.</summary>
        Task<CategoryDto> RetrieveCategoryByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>Retrieves the category listing for the current (or explicitly requested) Organization.</summary>
        Task<IEnumerable<CategoryDto>> ListCategoriesAsync(Guid organizationId = default, CancellationToken cancellationToken = default);
        #endregion
    }
}
