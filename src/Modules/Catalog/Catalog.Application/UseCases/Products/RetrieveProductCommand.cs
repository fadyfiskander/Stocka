using Catalog.Application.Bindings.ProductCases;
using Catalog.Application.DTOs.Product;
using Catalog.Application.Repositories.Queries.Product;
using Catalog.Contracts.DTOs.Product;
using Catalog.Contracts.UseCases.ProductCases;
using MediatR;

namespace Catalog.Application.UseCases.Products
{
    /// <summary>
    /// Retrieves one product by ID; missing IDs raise KeyNotFoundException.
    /// </summary>
    public class RetrieveProductQueryHandler : IRequestHandler<RetrieveProductQueryMediatR, ProductDto>
    {
        private readonly IProductQueryRepository _productQuery;

        public RetrieveProductQueryHandler(IProductQueryRepository productQuery)
        {
            _productQuery = productQuery;
        }

        public async Task<ProductDto> Handle(RetrieveProductQueryMediatR request, CancellationToken cancellationToken)
        {
            if (request.Contract.ProductId == Guid.Empty)
                throw new ArgumentException("ProductId cannot be empty.", nameof(request.Contract.ProductId));

            var product = await _productQuery.GetByIdAsync(request.Contract.ProductId, cancellationToken);
            return product ?? throw new KeyNotFoundException($"Product '{request.Contract.ProductId}' is not found.");
        }
    }
}
