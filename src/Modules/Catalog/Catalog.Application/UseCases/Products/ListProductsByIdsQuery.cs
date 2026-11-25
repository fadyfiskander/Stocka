using BuildingBlocks.Application.Abstractions;
using Catalog.Application.Bindings.ProductCases;
using Catalog.Application.Repositories.Queries.Product;
using Catalog.Contracts.DTOs.Product;
using Catalog.Contracts.UseCases.ProductCases;
using MediatR;

namespace Catalog.Application.UseCases.Products;

public sealed class ListProductsByIdsQueryHandler : IRequestHandler<ListProductsByIdsQueryMediatR, IEnumerable<ProductDto>>
{
    private readonly IProductQueryRepository _productQueryRepository;
    private readonly ICurrentUserService _currentContext;

    public ListProductsByIdsQueryHandler(
        IProductQueryRepository productQueryRepository,
        ICurrentUserService currentContext)
    {
        _productQueryRepository = productQueryRepository;
        _currentContext = currentContext;
    }

    public async Task<IEnumerable<ProductDto>> Handle(
        ListProductsByIdsQueryMediatR request,
        CancellationToken cancellationToken)
    {
        var organizationId = _currentContext.OrganizationId
            ?? throw new ArgumentNullException("organizationId is null, organizationId cannot be null");

        var productIds = request.Contract.ProductIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (productIds.Length == 0)
            return Array.Empty<ProductDto>();

        return await _productQueryRepository.GetByIdsAsync(productIds, organizationId, cancellationToken);
    }
}