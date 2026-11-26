namespace Catalog.Contracts.UseCases.ProductCases;

/// <summary>
/// Lists products of the current Organization (or an explicitly requested one).
/// </summary>
/// <remarks>
/// The Organization override is resolved by the Application pipeline against the
/// tenant context; callers cannot escape their own Organization from the contract alone.
/// </remarks>
public record ListProductsQuery
{
}
