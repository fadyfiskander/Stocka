using BuildingBlocks.Application.Common.Interfaces;
using Inventory.Application;
using Inventory.Application.Common.Interfaces;
using Inventory.Infrastructure.Services;
using Inventory.Infrastructure.Repositories.Commands.Bases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Inventory.Infrastructure.Persistence;

namespace Inventory.Infrastructure;

public static class InventoryModuleRegistration
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException("ConnectionString Is Nul");

        services.AddDbContext<InventoryDbContext>(option =>
        {
            option.UseSqlServer(connectionString);
        });

        services.AddTransient<IInventoryDbConnection>(_ =>
            new InventoryDbConnection(connectionString));

        #region Application Services Registrations
        services.AddInventoryApplicationServices();
        #endregion

        #region Repository Registrations
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<Inventory.Application.Repositories.Commands.IBatchRepository, Inventory.Infrastructure.Repositories.Commands.BatchRepository>();
        services.AddScoped<Inventory.Application.Repositories.Commands.IWarehouseBatchRepository, Inventory.Infrastructure.Repositories.Commands.WarehouseBatchRepository>();
        services.AddScoped<Inventory.Application.Repositories.Commands.IWarehouseRepository, Inventory.Infrastructure.Repositories.Commands.WarehouseRepository>();
        services.AddScoped<Inventory.Application.Repositories.Commands.IStockMovementRepository, Inventory.Infrastructure.Repositories.Commands.StockMovementRepository>();
        #endregion

        #region Query Repository Registrations
        services.AddScoped<Inventory.Application.Repositories.Queries.IWarehouseQueryRepository, Inventory.Infrastructure.Repositories.Queries.WarehouseQueryRepository>();
        services.AddScoped<Inventory.Application.Repositories.Queries.IBatchQueryRepository, Inventory.Infrastructure.Repositories.Queries.BatchQueryRepository>();
        services.AddScoped<Inventory.Application.Repositories.Queries.IWarehouseBatchQueryRepository, Inventory.Infrastructure.Repositories.Queries.WarehouseBatchQueryRepository>();
        services.AddScoped<Inventory.Application.Repositories.Queries.IStockMovementQueryRepository, Inventory.Infrastructure.Repositories.Queries.StockMovementQueryRepository>();
        services.AddScoped<Inventory.Application.Repositories.Queries.IInventoryReportQueryRepository, Inventory.Infrastructure.Repositories.Queries.InventoryReportQueryRepository>();
        #endregion

        #region Service Registrations
        services.AddScoped<IInventoryUnitOfWork, UnitOfWork>();
        services.AddScoped<Inventory.Contracts.Services.IInventoryServices, Inventory.Application.Services.InventoryServices>();
        #endregion

        return services;
    }
}