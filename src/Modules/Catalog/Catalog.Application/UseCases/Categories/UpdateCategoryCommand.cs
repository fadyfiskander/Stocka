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
    /// Renames an existing category.
    /// </summary>
    public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommandMediatR, CategoryDto>
    {
        private readonly ICategoryRepository _categoryRepository;

        public UpdateCategoryCommandHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<CategoryDto> Handle(UpdateCategoryCommandMediatR request, CancellationToken cancellationToken)
        {
            if (request.Contract.CategoryId == Guid.Empty)
                throw new ArgumentException("Category ID is required.", nameof(request.Contract.CategoryId));

            if (string.IsNullOrWhiteSpace(request.Contract.Name))
                throw new ArgumentException("A category name is required.", nameof(request.Contract.Name));

            var category = await _categoryRepository.GetByIdAsync(CategoryId.FromGuid(request.Contract.CategoryId))
                ?? throw new KeyNotFoundException($"Category with id '{request.Contract.CategoryId}' was not found.");

            category.UpdateName(request.Contract.Name);

            await _categoryRepository.UpdateAsync(category);

            return category.ToDto();
        }
    }
}
