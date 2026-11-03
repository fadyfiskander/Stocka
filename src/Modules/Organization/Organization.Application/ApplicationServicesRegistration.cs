using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Organization.Contracts.Services;
using System.Reflection;

namespace Organization.Application;

public static class ApplicationServicesRegistration
{
    public static IServiceCollection AddOrganizationApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Public cross-module contract (Organization.Contracts) — auth lookups for Identity flows.
        services.AddScoped<IOrganizationAuthService, Services.OrganizationAuthService>();

        // TODO: Register pipeline behaviors (ValidationBehavior, UnitOfWorkBehavior)
        // and contract use-case adapters once use-cases are added.

        return services;
    }
}
