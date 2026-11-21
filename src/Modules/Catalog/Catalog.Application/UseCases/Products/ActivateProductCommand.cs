using Catalog.Application.Bindings.ProductCases;
using Catalog.Application.DTOs.Product;
using Catalog.Application.Repositories.Commands;
using Catalog.Contracts.DTOs.Product;
using Catalog.Contracts.UseCases.ProductCases;
using Catalog.Domain.Products;
using MediatR;

namespace Catalog.Application.UseCases.Products
{
    /// <summary>
    /// Activates an existing product.
    /// </summary>
    public class ActivateProductCommandHandler : IRequestHandler<ActivateProductCommandMediatR, ProductDto>
    {
        private readonly IProductRepository _productRepository;

        public ActivateProductCommandHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<ProductDto> Handle(ActivateProductCommandMediatR request, CancellationToken cancellationToken)
        {
            if (request.Contract.ProductId == Guid.Empty)
                throw new ArgumentException("Product ID is required.", nameof(request.Contract.ProductId));

            var product = await _productRepository.GetByIdAsync(ProductId.FromGuid(request.Contract.ProductId))
                ?? throw new KeyNotFoundException($"Product with id '{request.Contract.ProductId}' was not found.");

            product.Activate();

            await _productRepository.UpdateAsync(product);

            return product.ToDto();
        }
    }
}
