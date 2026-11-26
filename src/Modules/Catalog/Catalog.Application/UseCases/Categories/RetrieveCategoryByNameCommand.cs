using Catalog.Application.Bindings.CategoryCases;
using Catalog.Application.DTOs.Category;
using Catalog.Application.Repositories.Queries.Category;
using Catalog.Contracts.DTOs.Category;
using Catalog.Contracts.UseCases.CategoryCases;
using MediatR;

namespace Catalog.Application.UseCases.Categories
{
    /// <summary>
    /// Retrieves one category by its exact name; missing names raise KeyNotFoundException.
    /// </summary>
    public class RetrieveCategoryByNameQueryHandler : IRequestHandler<RetrieveCategoryByNameQueryMediatR, CategoryDto>
    {
        private readonly ICategoryQueryRepository _categoryQueryRepository;

        public RetrieveCategoryByNameQueryHandler(ICategoryQueryRepository categoryQueryRepository)
        {
            _categoryQueryRepository = categoryQueryRepository;
        }

        public async Task<CategoryDto> Handle(RetrieveCategoryByNameQueryMediatR request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Contract.Name))
                throw new ArgumentException("Category name is required.", nameof(request.Contract.Name));

            return await _categoryQueryRepository.GetByNameAsync(request.Contract.Name.Trim(), cancellationToken)
                ?? throw new KeyNotFoundException($"Category with name '{request.Contract.Name}' was not found.");
        }
    }
}
