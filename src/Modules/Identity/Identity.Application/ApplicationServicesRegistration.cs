using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application
{
    public static class ApplicationServicesRegistration
    {
        public static IServiceCollection AddIdentityApplicationServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationServicesRegistration).Assembly));

            // Public module boundary (Identity.Contracts) — implemented by the pipeline-connected adapter.
            services.AddScoped<Contracts.Services.IAuthenticationServices, Services.AuthenticationServices>();

            return services;
        }
    }
}
