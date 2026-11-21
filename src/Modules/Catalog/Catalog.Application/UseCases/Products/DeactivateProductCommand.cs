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
    /// Deactivates an existing product (non-destructive; history is kept).
    /// </summary>
    public class DeactivateProductCommandHandler : IRequestHandler<DeactivateProductCommandMediatR, ProductDto>
    {
        private readonly IProductRepository _productRepository;

        public DeactivateProductCommandHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<ProductDto> Handle(DeactivateProductCommandMediatR request, CancellationToken cancellationToken)
        {
            if (request.Contract.ProductId == Guid.Empty)
                throw new ArgumentException("Product ID is required.", nameof(request.Contract.ProductId));

            var product = await _productRepository.GetByIdAsync(ProductId.FromGuid(request.Contract.ProductId))
                ?? throw new KeyNotFoundException($"Product with id '{request.Contract.ProductId}' was not found.");

            product.Deactivate();

            await _productRepository.UpdateAsync(product);

            return product.ToDto();
        }
    }
}
