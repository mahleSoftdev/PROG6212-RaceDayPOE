using Microsoft.AspNetCore.Mvc;
using RaceDayPOE.Web.Infrastructure;

namespace RaceDayPOE.Web.Controllers;

//Base class for every controller: one-line flash messages and role-aware redirects.
public abstract class AppController : Controller
{
    protected void Success(string message) => TempData["Success"] = message;
    protected void Warning(string message) => TempData["Warning"] = message;
    protected void Error(string message) => TempData["Error"] = message;
    protected void Info(string message) => TempData["Info"] = message;

    protected IActionResult RedirectToRoleHome() => User.IsInRole(RoleNames.Organiser)
        ? RedirectToAction("Index", "Dashboard", new { area = "Organiser" })
        : RedirectToAction("Index", "Home", new { area = "" });

    //Only ever redirects to a local URL, so a crafted returnUrl cannot bounce users to another site.
    protected IActionResult RedirectToLocalOrHome(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToRoleHome();
}
