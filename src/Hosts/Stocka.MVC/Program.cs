using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Interfaces;
using Catalog.Infrastructure;
using Identity.Infrastructure;
using Organization.Infrastructure;
using Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Stocka.MVC.Infrastructure;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddLocalization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddHealthChecks();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

#region Modules
var configuration = builder.Configuration;
builder.Services.AddInventoryModule(configuration);
builder.Services.AddCatalogModule(configuration);
builder.Services.AddOrganizationModule(configuration);
builder.Services.AddIdentityModule(configuration);
// Host-level user context: registered after AddIdentityModule so it wins the
// ICurrentUserContext resolution — unlike the module's CurrentUserContext it
// keeps serving anonymous (pre-login) requests with a least-privileged fallback
// instead of throwing when no role claim exists yet.
builder.Services.AddScoped<ICurrentUserContext, HttpUserContext>();
#endregion

// Host-owned authentication: the classic MVC cookie flow is the host's scheme.
// ((AddIdentityModule keeps its own JwtBearer registered for API surfaces — defaults
// here win because this call comes after it in registration order.)
builder.Services
    .AddAuthentication(options =>
    {
        // CookieAuthenticationDefaults.AuthenticationScheme == "Cookies"
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        // Account area routes (area = Account, controller = Account).
        options.LoginPath = "/Account/Account/Login";
        options.AccessDeniedPath = "/Account/Account/AccessDenied";

        options.Cookie.Name = "Stocka.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        options.Cookie.SameSite = SameSiteMode.Lax;

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Auto-migrate databases on startup (development only — use with care in production)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    try
    {
        var catalogCtx = scope.ServiceProvider.GetService<Catalog.Infrastructure.Contexts.CatalogDbContext>();
        if (catalogCtx != null) await catalogCtx.Database.MigrateAsync();

        var inventoryCtx = scope.ServiceProvider.GetService<Inventory.Infrastructure.Contexts.InventoryDbContext>();
        if (inventoryCtx != null) await inventoryCtx.Database.MigrateAsync();

        var organizationCtx = scope.ServiceProvider.GetService<Organization.Infrastructure.Contexts.OrganizationDbContext>();
        if (organizationCtx != null) await organizationCtx.Database.MigrateAsync();

        var identityCtx = scope.ServiceProvider.GetService<Identity.Infrastructure.Persistence.AppIdentityDbContext>();
        if (identityCtx != null) await identityCtx.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
var supportedCultures = new[] { new CultureInfo("ar"), new CultureInfo("en") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("ar"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});
app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();
app.MapHealthChecks("/health");



app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
