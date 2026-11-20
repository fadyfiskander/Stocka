using BuildingBlocks.Application.Abstractions;
using Catalog.Application.Bindings.ProductCases;
using Catalog.Application.DTOs.Product;
using Catalog.Application.Repositories.Queries.Product;
using Catalog.Contracts.DTOs.Product;
using Catalog.Contracts.UseCases.ProductCases;
using MediatR;

namespace Catalog.Application.UseCases.Products
{
    /// <summary>
    /// Lists products of the current Organization (or an explicitly requested one).
    /// </summary>
    public class ListProductsQueryHandler : IRequestHandler<ListProductsQueryMediatR, IEnumerable<ProductDto>>
    {
        private readonly IProductQueryRepository _productQueryRepository;
        private readonly ICurrentUserService _currentContext;

        public ListProductsQueryHandler(IProductQueryRepository productQueryRepository, ICurrentUserService currentContext)
        {
            _productQueryRepository = productQueryRepository;
            _currentContext = currentContext;
        }

        public async Task<IEnumerable<ProductDto>> Handle(ListProductsQueryMediatR request, CancellationToken cancellationToken)
        {
            var organizationId = request.Contract.OrganizationId ?? _currentContext.OrganizationId;
            if(organizationId is null)
                throw new ArgumentNullException("organizationId is null, organizationId cannot be null");

            var products = await _productQueryRepository.GetAllAsync(organizationId.Value);
            return products;
        }
    }
}
