using Identity.Contracts.DTOs;
using Identity.Contracts.DTOs.Identity.Users.Login;
using Identity.Contracts.Exceptions;
using Identity.Contracts.Services;
using Identity.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stocka.MVC.Areas.Account.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Stocka.MVC.Areas.Account.Controllers
{
    [Area("Account")]
    [Route("[area]/[controller]")]
    public class AccountController : Controller
    {
        private readonly IAuthenticationServices _identityServices;

        public AccountController(IAuthenticationServices identityServices)
        {
            _identityServices = identityServices;
        }

        // GET: /Account/Account/Login
        // No-op when already authenticated (the token cookie answers the request).
        [HttpGet("Login")]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };

            return View(model);
        }

        // POST: /Account/Account/Login
        // Flow: authenticate through the Identity facade → it issues a module JWT →
        // the JWT is parked inside the token cookie (claims + token claim) whose
        // expiry equals the token's — so an expired token falls back to /Account/Login.
        [HttpPost("Login")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // 1. Authenticate through Identity Contracts.

                var result = await _identityServices.AuthenticateAsync(
                    new LoginRequest(
                        model.Email,
                        model.Password
                    ),
                    cancellationToken
                );

                // 2. Build Claims.

                var claims = new List<Claim>
            {
                new(
                    ClaimTypes.NameIdentifier,
                    result.UserId.ToString()
                ),

                new(
                    ClaimTypes.Email,
                    result.Email
                ),

                new(
                    "PartyId",
                    result.PartyId.ToString()
                ),

                new(
                    "OrganizationId",
                    result.Organizations
                    .Select(o => o.OrganizationId)
                    .FirstOrDefault()
                    .ToString()

                ),

                new(
                    ClaimTypes.Role,
                    result.Organizations.Select(o => o.Role)
                    .FirstOrDefault() ?? string.Empty
                    
                )
            };

                // 3. Create ClaimsIdentity.

                var claimsIdentity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme
                );

                // 4. Create ClaimsPrincipal.

                var claimsPrincipal = new ClaimsPrincipal(
                    claimsIdentity
                );

                // 5. Configure Authentication Properties.

                var authenticationProperties =
                    new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,

                        AllowRefresh = true,

                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(1)
                    };

                // 6. Issue Authentication Cookie.

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    claimsPrincipal,
                    authenticationProperties
                );

                // 7. Redirect after successful login.

                return RedirectToAction("Index", "Home");
            }
            catch (InvalidCredentialsException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password."
                );

                return View(model);
            }
            //catch (AccountDisabledException)
            //{
            //    ModelState.AddModelError(
            //        string.Empty,
            //        "Your account is disabled."
            //    );

            //    return View(model);
            //}
        }

        // POST: /Account/Account/Logout
        [HttpPost("Logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/Account/AccessDenied
        [HttpGet("AccessDenied")]
        public IActionResult AccessDenied()
        {
            ViewBag.Title = "Access Denied";
            return View();
        }

        /// <summary>
        /// Builds the cookie principal: identity claims (user id / name / email), the app
        /// roles as role claims, the JWT issued at login parked in the token claim (usable
        /// later for API surfaces), and the organization context (first membership is the
        /// active one until the user switches) plus one organizationAccess claim per
        /// reachable organization. No AuthenticationProperties on purpose: a session cookie
        /// honoured by the host's ExpireTimeSpan + SlidingExpiration.
        /// </summary>
        //private async Task SignInWithTokenCookieAsync(AuthModelMVC result)
        //{
        //    var organizations = await _identityServices.GetUserOrganizationIdsAsync(result.UserId);

        //    var claims = new List<Claim>
        //    {
        //        new(JwtRegisteredClaimNames.Sub, result.UserId.ToString()),
        //        new(ClaimNames.UserId, result.UserId.ToString()),
        //        new(ClaimNames.Token, result.Token), // raw JWT — usable later for API calls
        //        new(ClaimTypes.Name, result.UserName),
        //        new(ClaimTypes.Email, result.Email),
        //        new(ClaimNames.ActiveOrganizationId, organizations.Count > 0 ? organizations[0].ToString() : Guid.Empty.ToString())
        //    };

        //    foreach (var role in result.Roles)
        //        claims.Add(new Claim(ClaimTypes.Role, role));

        //    foreach (var organizationId in organizations)
        //        claims.Add(new Claim(ClaimNames.organizationAccess, organizationId.ToString()));

        //    var principal = new ClaimsPrincipal(
        //        new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        //    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        //}

        private IActionResult RedirectToReturnUrlOrHome(string? returnUrl)
        {
            var local = returnUrl != null && Url.IsLocalUrl(returnUrl);
            return Redirect(local ? returnUrl! : Url.Action("Index", "Home")!);
        }
    }
}
