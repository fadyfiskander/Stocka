using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Stocka.MVC.Controllers;

public sealed class LocalizationController : Controller
{
    [HttpGet]
    public IActionResult SetLanguage(string culture, string? returnUrl = null)
    {
        if (culture is not ("ar" or "en")) return BadRequest();

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                // Path must be "/" — without it the cookie is scoped to /Localization
                // and never reaches the rest of the app, so the switch looks dead.
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}
