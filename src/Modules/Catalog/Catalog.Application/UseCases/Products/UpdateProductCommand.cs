using Catalog.Application.Bindings.ProductCases;
using Catalog.Application.DTOs.Product;
using Catalog.Application.Repositories.Commands;
using Catalog.Application.Repositories.Queries.Category;
using Catalog.Contracts.DTOs.Product;
using Catalog.Contracts.UseCases.ProductCases;
using Catalog.Domain.Categories;
using Catalog.Domain.Entities;
using Catalog.Domain.Primitives.Identifiers;
using MediatR;

namespace Catalog.Application.UseCases.Products
{
    /// <summary>
    /// Applies a partial update of an existing product: only provided (non-null)
    /// contract fields change.
    /// </summary>
    public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommandMediatR, ProductDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryQueryRepository _categoryQueryRepository;

        public UpdateProductCommandHandler(
            IProductRepository productRepository,
            ICategoryQueryRepository categoryQueryRepository)
        {
            _productRepository = productRepository;
            _categoryQueryRepository = categoryQueryRepository;
        }

        public async Task<ProductDto> Handle(UpdateProductCommandMediatR request, CancellationToken cancellationToken)
        {
            if (request.Contract.ProductId == Guid.Empty)
                throw new ArgumentException("Product ID is required.", nameof(request.Contract.ProductId));

            var product = await _productRepository.GetByIdAsync(ProductId.FromGuid(request.Contract.ProductId))
                ?? throw new KeyNotFoundException($"Product with id '{request.Contract.ProductId}' was not found.");

            bool changeRequested = false;

            if (!string.IsNullOrWhiteSpace(request.Contract.Name))
            {
                product.Rename(request.Contract.Name);
                changeRequested = true;
            }

            if (request.Contract.SellingPrice.HasValue)
            {
                product.ChangeSellingPrice(request.Contract.SellingPrice.Value);
                changeRequested = true;
            }

            if (request.Contract.CategoryId.HasValue)
            {
                if (request.Contract.CategoryId.Value == Guid.Empty)
                    throw new ArgumentException("Category ID must be a valid value.", nameof(request.Contract.CategoryId));

                var categoryExists = await _categoryQueryRepository.GetByIdAsync(request.Contract.CategoryId.Value);
                if (categoryExists is null)
                    throw new KeyNotFoundException($"Category with id '{request.Contract.CategoryId}' was not found.");

                product.ChangeCategory(CategoryId.FromGuid(request.Contract.CategoryId.Value));
                changeRequested = true;
            }

            if (request.Contract.Barcode is not null)
            {
                product.ChangeBarcode(request.Contract.Barcode);
                changeRequested = true;
            }

            if (request.Contract.ImagePath is not null)
            {
                product.ChangeImage(request.Contract.ImagePath);
                changeRequested = true;
            }

            if (!changeRequested)
                throw new ArgumentException("At least one property must be provided to update the product.", nameof(request));

            await _productRepository.UpdateAsync(product);

            return product.ToDto();
        }
    }
}
