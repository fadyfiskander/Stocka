using BuildingBlocks.Application.Abstractions;
using Catalog.Application.Bindings.CategoryCases;
using Catalog.Application.DTOs.Category;
using Catalog.Application.Repositories.Commands;
using Catalog.Contracts.DTOs.Category;
using Catalog.Contracts.UseCases.CategoryCases;
using Catalog.Domain.Entities;
using MediatR;

namespace Catalog.Application.UseCases.Categories
{
    /// <summary>
    /// Creates the category through the domain factory (which enforces name invariants)
    /// and returns the created category data transfer object.
    /// </summary>
    public class RegisterCategoryCommandHandler : IRequestHandler<RegisterCategoryCommandMediatR, CategoryDto?>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICurrentUserService _currentUserContext;

        public RegisterCategoryCommandHandler(ICategoryRepository categoryRepository, ICurrentUserService currentUserContext)
        {
            _categoryRepository = categoryRepository;
            _currentUserContext = currentUserContext;
        }

        public async Task<CategoryDto?> Handle(RegisterCategoryCommandMediatR request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Contract.Name))
                throw new ArgumentException("Name is empty, name cannot be empty.", nameof(request.Contract.Name));

            var organizationId = _currentUserContext.OrganizationId;
            if(organizationId is null)
                throw new ArgumentNullException("organizationId is null, organizationId cannot be null");

            var category = Category.Create(
                organizationId.Value,
                request.Contract.Name.Trim());

            await _categoryRepository.AddAsync(category);

            return category.ToDto();
        }
    }
}
