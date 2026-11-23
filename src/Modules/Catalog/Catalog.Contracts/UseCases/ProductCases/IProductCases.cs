using Catalog.Contracts.DTOs.Product;

namespace Catalog.Contracts.UseCases.ProductCases;

/// <summary>
/// Product use case contract: the pure module boundary for every product operation.
/// Implemented by the Catalog Application layer (which wires each call into its MediatR
/// pipeline — validation and unit-of-work included) and consumed via DI.
/// </summary>
public interface IProductCases
{
    #region Commands
    /// <summary>Registers a new product for the current Organization.</summary>
    Task<ProductDto?> RegisterProductAsync(RegisterProductCommand command, CancellationToken cancellationToken = default);

    /// <summary>Applies a partial update; only provided fields change.</summary>
    Task<ProductDto> UpdateProductAsync(UpdateProductCommand command, CancellationToken cancellationToken = default);

    /// <summary>Activates an existing product.</summary>
    Task<ProductDto> ActivateProductAsync(ActivateProductCommand command, CancellationToken cancellationToken = default);

    /// <summary>Deactivates an existing product (non-destructive; history is kept).</summary>
    Task<ProductDto> DeactivateProductAsync(DeactivateProductCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region Queries
    /// <summary>Retrieves one product by ID (cross-Organization IDs are indistinguishable from not-found).</summary>
    Task<ProductDto> RetrieveProductAsync(RetrieveProductQuery query, CancellationToken cancellationToken = default);

    /// <summary>Retrieves one product by its exact name.</summary>
    Task<ProductDto> RetrieveProductByNameAsync(RetrieveProductByNameQuery query, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the product listing for the current (or explicitly requested) Organization.</summary>
    Task<IEnumerable<ProductDto>> ListProductsAsync(ListProductsQuery query, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a set of products by ID for the current Organization.</summary>
    Task<IEnumerable<ProductDto>> ListProductsByIdsAsync(ListProductsByIdsQuery query, CancellationToken cancellationToken = default);
    #endregion
}
