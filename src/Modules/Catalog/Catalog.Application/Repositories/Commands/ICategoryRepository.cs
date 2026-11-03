using BuildingBlocks.Application.Primitives.Identifiers;
using BuildingBlocks.Application.Common.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.Primitives;

namespace Catalog.Application.Repositories.Commands
{
    public interface ICategoryRepository : IRepository<Category, CategoryId>
    {

    }
}