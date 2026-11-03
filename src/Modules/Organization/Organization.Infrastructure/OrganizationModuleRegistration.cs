using BuildingBlocks.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Organization.Application;
using Organization.Application.Common.Interfaces;
using Organization.Infrastructure.Contexts;

namespace Organization.Infrastructure;

public static class OrganizationModuleRegistration
{
    public static IServiceCollection AddOrganizationModule(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("OrganizationConnection") ?? throw new ArgumentNullException("Organization ConnectionString Is Nul");

        services.AddDbContext<OrganizationDbContext>(option =>
        {
            option.UseSqlServer(connectionString);
        });

        services.AddTransient<IOrganizationDbConnection>(_ =>
            new OrganizationDbConnection(connectionString));

        #region Application Services Registrations
        services.AddOrganizationApplicationServices();
        #endregion

        #region Repository Registrations
        services.AddScoped(typeof(IRepository<,>), typeof(Repositories.Commands.Bases.Repository<,>));
        services.AddScoped<Organization.Application.Repositories.Commands.IOrganizationRepository, Organization.Infrastructure.Repositories.Commands.OrganizationRepository>();
        #endregion

        #region Query Repository Registrations
        services.AddScoped<Organization.Application.Repositories.Queries.IOrganizationQueryRepository, Organization.Infrastructure.Repositories.Queries.OrganizationQueryRepository>();
        services.AddScoped<Organization.Application.Repositories.Queries.IOrganizationMembershipQueryRepository, Organization.Infrastructure.Repositories.Queries.OrganizationMembershipQueryRepository>();
        #endregion

        return services;
    }
}
