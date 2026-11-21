using FluentValidation;
using Inventory.Application.Behaviors;
using Inventory.Contracts.UseCases.BatchCases;
using Inventory.Contracts.UseCases.ReportCases;
using Inventory.Contracts.UseCases.StockCases;
using Inventory.Contracts.UseCases.WarehouseCases;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Inventory.Application;

public static class ApplicationServicesRegistration
{
    public static IServiceCollection AddInventoryApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

        // Pure contract use-case interfaces (Inventory.Contracts) implemented by pipeline adapters.
        services.AddTransient<IWarehouseCases, Adapters.WarehouseCases.WarehouseCases>();
        services.AddScoped<IStockCases, Adapters.StockCases.StockCases>();
        services.AddTransient<IBatchCases, Adapters.BatchCases.BatchCases>();
        services.AddScoped<IInventoryReportCases, UseCases.ReportCases.InventoryReportCases>();

        return services;
    }
}
