using BuildingBlocks.Application.Abstractions;
using Catalog.Application.Bindings.ProductCases;
using Catalog.Application.DTOs.Product;
using Catalog.Application.Repositories.Commands;
using Catalog.Contracts.DTOs.Product;
using Catalog.Domain.Categories;
using Catalog.Domain.Entities;
using MediatR;

namespace Catalog.Application.UseCases.Products
{
    /// <summary>
    /// Creates the product through the domain factory (which enforces name/price invariants)
    /// and returns the created product data transfer object.
    /// </summary>
    public class RegisterProductCommandHandler : IRequestHandler<RegisterProductCommandMediatR, ProductDto?>
    {
        private readonly IProductRepository _productRepository;
        private readonly ICurrentUserService _currentUserContext;

        public RegisterProductCommandHandler(IProductRepository productRepository, ICurrentUserService currentUserContext)
        {
            _productRepository = productRepository;
            _currentUserContext = currentUserContext;
        }

        public async Task<ProductDto?> Handle(RegisterProductCommandMediatR request, CancellationToken cancellationToken)
        {
            // Business validations
            if (string.IsNullOrWhiteSpace(request.Contract.Name))
                throw new ArgumentException("Name cannot be empty.", nameof(request.Contract.Name));

            if (request.Contract.CategoryId == Guid.Empty)
                throw new ArgumentException("Category ID cannot be empty.", nameof(request.Contract.CategoryId));

            if (request.Contract.SellingPrice < 0)
                throw new ArgumentOutOfRangeException(nameof(request.Contract.SellingPrice), "Selling price cannot be negative.");

            var organizationId = _currentUserContext.OrganizationId;

            if (organizationId is null)
                throw new ArgumentNullException("organizationId is null, organizationId cannot be null");

            // Ensure basic business rules before creating entity
            var product = Product.Create(
                organizationId.Value,
                CategoryId.FromGuid(request.Contract.CategoryId),
                request.Contract.Name.Trim(),
                request.Contract.SellingPrice,
                string.IsNullOrWhiteSpace(request.Contract.Barcode) ? null : request.Contract.Barcode.Trim());

            await _productRepository.AddAsync(product);

            return product.ToDto();
        }
    }
}
