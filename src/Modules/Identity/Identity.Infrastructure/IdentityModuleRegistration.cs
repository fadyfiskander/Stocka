using BuildingBlocks.Application.Common.Interfaces;
using Identity.Application;
using Identity.Application.Common.Interfaces;
using Identity.Application.Repositories.Commands;
using Identity.Application.Repositories.Queries;
using Identity.Infrastructure.Authentication;
using Identity.Infrastructure.Identity;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Repositories.Commands;
using Identity.Infrastructure.Repositories.Commands.Bases;
using Identity.Infrastructure.Repositories.Queries;
using Identity.Infrastructure.Services;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Identity.Infrastructure
{
    public static class IdentityModuleRegistration
    {
        public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("IdentityConnection") ?? throw new ArgumentNullException("Connection String is null");
        
            services.AddDbContext<AppIdentityDbContext>(options => 
            {
                options.UseSqlServer(connectionString);
            });

            services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

            #region ASP.NET Identity Hosting
            // Stores + UserManager/RoleManager. Password rules are stated explicitly so
            // behavior is visible instead of hidden in soft defaults.
            services
                .AddIdentity<ApplicationUser, ApplicationRole>(options =>
                {
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                })
                .AddEntityFrameworkStores<AppIdentityDbContext>()
                .AddDefaultTokenProviders();
            #endregion

            services.AddTransient<IIdentityDbConnection>(_ =>
                new IdentityDbConnection(connectionString));

            #region Application Services Registrations
            services.AddIdentityApplicationServices();
            #endregion

            #region Repository Registrations
            // Note: the abstract base (Repository<,>) must NOT be registered itself —
            // every aggregate gets a concrete registration below.

            // Aggregate-specific command (write-side) repositories.
            services.AddScoped<IPartyRepository, PartyRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<ISupplierRepository, SupplierRepository>();
            #endregion

            #region Query Repository Registrations
            // Read-side (CQRS) repositories over raw SQL (Dapper); scoped per the transient IDbConnection below.
            services.AddScoped<IPartyQueryRepository, PartyQueryRepository>();
            services.AddScoped<ICustomerQueryRepository, CustomerQueryRepository>();
            services.AddScoped<IEmployeeQueryRepository, EmployeeQueryRepository>();
            services.AddScoped<ISupplierQueryRepository, SupplierQueryRepository>();
            #endregion

            #region Service Registrations
            services.AddScoped<IIdentityUnitOfWork, UnitOfWork>();
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IUserServices, UserServices>();
            services.AddScoped<ITokenGenerator, TokenGenerator>();
            services.AddScoped<ICurrentUserContext, CurrentUserContext>();
            #endregion

            #region Authentication (JWT + MVC token cookie)
            var jwtSection = configuration.GetSection(JwtOptions.SectionName);

            var jwtOptions = jwtSection.Get<JwtOptions>()
                ?? throw new ArgumentNullException("JWT configuration is missing");

            services
                .AddAuthentication(options =>
                {
                    // The MVC host speaks the token-cookie flow by default: Login issues a
                    // module JWT, the cookie carries it, and its expiry equals the token's —
                    // so an expired token redirects any protected operation to /Account/Login.
                    options.DefaultScheme = AuthenticationSchemes.TokenCookie;
                    options.DefaultAuthenticateScheme = AuthenticationSchemes.TokenCookie;
                    options.DefaultChallengeScheme = AuthenticationSchemes.TokenCookie;

                    // JwtBearer stays registered for API surfaces, which must opt in
                    // explicitly: [Authorize(AuthenticationSchemes = AuthenticationSchemes.JwtBearer)]
                })
                .AddCookie(AuthenticationSchemes.TokenCookie, options =>
                {
                    options.Cookie.Name = "Stocka.Auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;

                    // Hard ceiling — tickets are signed out with the JWT's own expiry,
                    // and no sliding renewal: token lifetime is strict.
                    options.ExpireTimeSpan = TimeSpan.FromHours(jwtOptions.DurationInHours);
                    options.SlidingExpiration = false;

                    options.LoginPath = "/Account/Login";
                    options.ReturnUrlParameter = "returnUrl";
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,

                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromHours(jwtOptions.DurationInHours)
                    };
                });
            #endregion

            return services;
        }
    }
}
