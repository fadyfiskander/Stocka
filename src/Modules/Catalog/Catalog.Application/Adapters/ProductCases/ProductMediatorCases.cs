using Catalog.Application.Bindings.ProductCases;
using Catalog.Contracts.DTOs.Product;
using Catalog.Contracts.UseCases.ProductCases;
using MediatR;

namespace Catalog.Application.Adapters.ProductCases;

/// <summary>
/// Implements the IProductCases contract by dispatching each pure contract record through the
/// application's MediatR pipeline (validation behavior, unit-of-work behavior, handler).
/// Registered in DI as the implementation of the module boundary contract.
/// </summary>
public class ProductMediatorCases : IProductCases
{
    private readonly IMediator _mediator;

    public ProductMediatorCases(IMediator mediator) => _mediator = mediator;

    public Task<ProductDto?> RegisterProductAsync(RegisterProductCommand command, CancellationToken cancellationToken = default)
        => _mediator.Send(new RegisterProductCommandMediatR(command), cancellationToken);

    public Task<ProductDto> UpdateProductAsync(UpdateProductCommand command, CancellationToken cancellationToken = default)
        => _mediator.Send(new UpdateProductCommandMediatR(command), cancellationToken);

    public Task<ProductDto> ActivateProductAsync(ActivateProductCommand command, CancellationToken cancellationToken = default)
        => _mediator.Send(new ActivateProductCommandMediatR(command), cancellationToken);

    public Task<ProductDto> DeactivateProductAsync(DeactivateProductCommand command, CancellationToken cancellationToken = default)
        => _mediator.Send(new DeactivateProductCommandMediatR(command), cancellationToken);

    public Task<ProductDto> RetrieveProductAsync(RetrieveProductQuery query, CancellationToken cancellationToken = default)
        => _mediator.Send(new RetrieveProductQueryMediatR(query), cancellationToken);

    public Task<ProductDto> RetrieveProductByNameAsync(RetrieveProductByNameQuery query, CancellationToken cancellationToken = default)
        => _mediator.Send(new RetrieveProductByNameQueryMediatR(query), cancellationToken);

    public Task<IEnumerable<ProductDto>> ListProductsAsync(ListProductsQuery query, CancellationToken cancellationToken = default)
        => _mediator.Send(new ListProductsQueryMediatR(query), cancellationToken);

    public Task<IEnumerable<ProductDto>> ListProductsByIdsAsync(ListProductsByIdsQuery query, CancellationToken cancellationToken = default)
        => _mediator.Send(new ListProductsByIdsQueryMediatR(query), cancellationToken);
}
