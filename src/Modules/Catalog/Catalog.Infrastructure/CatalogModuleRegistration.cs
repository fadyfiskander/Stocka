using BuildingBlocks.Application.Common.Interfaces;
using Catalog.Application;
using Catalog.Application.Common.Interfaces;
using Catalog.Application.Repositories.Commands;
using Catalog.Application.Repositories.Queries.Category;
using Catalog.Application.Repositories.Queries.Product;
using Catalog.Contracts.Services;
using Catalog.Infrastructure.Contexts;
using Catalog.Infrastructure.Repositories.Commands;
using Catalog.Infrastructure.Repositories.Commands.Bases;
using Catalog.Infrastructure.Repositories.Queries;
using Catalog.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Catalog.Infrastructure;

public static class CatalogModuleRegistration
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException("ConnectionString Is Nul");

        services.AddDbContext<CatalogDbContext>(option =>
        {
            option.UseSqlServer(connectionString);
        });

        services.AddTransient<ICatalogDbConnection>(_ =>
            new CatalogDbConnection(connectionString));

        #region Application Services Registrations
        services.AddCatalogApplicationServices();
        #endregion

        #region Repository Registrations
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        #endregion

        #region Query Repository Registrations
        services.AddScoped<IProductQueryRepository, ProductQueryRepository>();
        services.AddScoped<ICategoryQueryRepository, CategoryQueryRepository>();
        #endregion

        #region Service Registrations
        services.AddScoped<ICatalogUnitOfWork, UnitOfWork>();
        services.AddScoped<Catalog.Contracts.Services.ICatalogServices, Catalog.Application.Services.CatalogServices>();
        #endregion

        return services;
    }
}