using BuildingBlocks.Application.Abstractions;
using Catalog.Application.Bindings.CategoryCases;
using Catalog.Application.Repositories.Queries.Category;
using Catalog.Contracts.DTOs.Category;
using MediatR;

namespace Catalog.Application.UseCases.Categories
{
    /// <summary>
    /// Lists categories of the current Organization (or an explicitly requested one).
    /// </summary>
    public class ListCategoriesQueryHandler(ICategoryQueryRepository categoryQuery)
        : IRequestHandler<ListCategoriesQueryMediatR, IEnumerable<CategoryDto>>
    {
        public async Task<IEnumerable<CategoryDto>> Handle(ListCategoriesQueryMediatR request, CancellationToken cancellationToken)
        {
            return await categoryQuery.GetAllAsync(cancellationToken);
        }
    }
}
