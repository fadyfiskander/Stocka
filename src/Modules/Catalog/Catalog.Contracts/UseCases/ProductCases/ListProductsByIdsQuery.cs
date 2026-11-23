namespace Catalog.Contracts.UseCases.ProductCases;

/// <summary>
/// Retrieves products by IDs for the current Organization.
/// </summary>
public record ListProductsByIdsQuery(IReadOnlyCollection<Guid> ProductIds);