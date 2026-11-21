using Catalog.Application.Bindings.CategoryCases;
using Catalog.Application.DTOs.Category;
using Catalog.Application.Repositories.Commands;
using Catalog.Contracts.DTOs.Category;
using Catalog.Contracts.UseCases.CategoryCases;
using Catalog.Domain.Categories;
using MediatR;

namespace Catalog.Application.UseCases.Categories
{
    /// <summary>
    /// Activates an existing category.
    /// </summary>
    public class ActivateCategoryCommandHandler : IRequestHandler<ActivateCategoryCommandMediatR, CategoryDto>
    {
        private readonly ICategoryRepository _categoryRepository;

        public ActivateCategoryCommandHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<CategoryDto> Handle(ActivateCategoryCommandMediatR request, CancellationToken cancellationToken)
        {
            if (request.Contract.CategoryId == Guid.Empty)
                throw new ArgumentException("Category ID is required.", nameof(request.Contract.CategoryId));

            var category = await _categoryRepository.GetByIdAsync(CategoryId.FromGuid(request.Contract.CategoryId))
                ?? throw new KeyNotFoundException($"Category with id '{request.Contract.CategoryId}' was not found.");

            category.UpdateActive(true);

            await _categoryRepository.UpdateAsync(category);

            return category.ToDto();
        }
    }
}
