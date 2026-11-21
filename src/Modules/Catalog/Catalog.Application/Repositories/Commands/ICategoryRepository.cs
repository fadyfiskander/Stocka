using BuildingBlocks.Application.Common.Interfaces;
using Catalog.Domain.Categories;

namespace Catalog.Application.Repositories.Commands
{
    public interface ICategoryRepository : IRepository<Category, CategoryId>
    {

    }
}