using Catalog.Application.Bindings.CategoryCases;
using Catalog.Application.DTOs.Category;
using Catalog.Application.Repositories.Queries.Category;
using Catalog.Contracts.DTOs.Category;
using Catalog.Contracts.UseCases.CategoryCases;
using MediatR;

namespace Catalog.Application.UseCases.Categories
{
    /// <summary>
    /// Retrieves one category by ID; missing IDs raise KeyNotFoundException.
    /// </summary>
    public class RetrieveCategoryQueryHandler : IRequestHandler<RetrieveCategoryQueryMediatR, CategoryDto>
    {
        private readonly ICategoryQueryRepository _categoryQueryRepository;

        public RetrieveCategoryQueryHandler(ICategoryQueryRepository categoryQueryRepository)
        {
            _categoryQueryRepository = categoryQueryRepository;
        }

        public async Task<CategoryDto> Handle(RetrieveCategoryQueryMediatR request, CancellationToken cancellationToken)
        {
            if (request.Contract.CategoryId == Guid.Empty)
                throw new ArgumentException("Category ID is required.", nameof(request.Contract.CategoryId));

            return await _categoryQueryRepository.GetByIdAsync(request.Contract.CategoryId, cancellationToken)
                ?? throw new KeyNotFoundException($"Category with id '{request.Contract.CategoryId}' was not found.");
        }
    }
}
