using System;

namespace Catalog.Contracts.DTOs.Product
{
    /// <summary>
    /// Public Catalog module representation of a product.
    /// Carries only display identifiers (raw Guids) — never domain primitives
    /// or infrastructure concerns.
    /// </summary>
    public record ProductDto(
        Guid Id,
        string Name,
        string Barcode,
        Guid CategoryId,
        decimal Price,
        bool IsActive,
        string ImagePath
    );
}
