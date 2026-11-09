using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using RaceDayPOE.Web.Models.Api;

namespace RaceDayPOE.Web.Infrastructure;

//Keeps the signed-in state in two places that must agree: the MVC auth cookie (claims, used by [Authorize])
//and the server-side session (account id, role, display name, plus the forwarded API cookie).
public static class AccountSession
{
    public static async Task SignInAsync(HttpContext http, AccountDto account)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.FullName),
            new(ClaimTypes.Email, account.Email),
            new(ClaimTypes.Role, account.Role)
        };
        http.Session.SetString(SessionKeys.AccountId, account.Id.ToString());
        http.Session.SetString(SessionKeys.Role, account.Role);
        http.Session.SetString(SessionKeys.DisplayName, account.FullName);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties { IsPersistent = false });
    }

    public static async Task SignOutAsync(HttpContext http)
    {
        http.Session.Clear();
        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    //Rejects an auth cookie whose server-side session is gone (expired, app restarted, signed out elsewhere).
    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var session = context.HttpContext.Session;
        await session.LoadAsync();
        var role = context.Principal?.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrEmpty(session.GetString(SessionKeys.ApiAuth)) || session.GetString(SessionKeys.Role) != role)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
