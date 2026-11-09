using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDayPOE.Web.Infrastructure;
using RaceDayPOE.Web.Models.Api;
using RaceDayPOE.Web.Models.ViewModels;
using RaceDayPOE.Web.Services;

namespace RaceDayPOE.Web.Controllers;

//Register, log in and log out. Credentials are checked by the API; this controller only manages the MVC session.
public class AccountController : AppController
{
    private readonly IRaceDayApi _api;
    public AccountController(IRaceDayApi api) => _api = api;

    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null, bool expired = false, bool loggedOut = false)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToRoleHome();
        if (expired) ViewData["Notice"] = "Your session has ended. Please sign in again.";
        else if (loggedOut) ViewData["Notice"] = "You have been signed out.";
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        HttpContext.Session.Clear();
        var result = await _api.LoginAsync(new LoginRequest(model.Email.Trim(), model.Password, model.Role));
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            model.Password = "";
            return View(model);
        }

        await AccountSession.SignInAsync(HttpContext, result.Data!);
        Success($"Welcome back, {result.Data!.FirstName}!");
        return RedirectToLocalOrHome(model.ReturnUrl);
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Register() =>
        User.Identity?.IsAuthenticated == true ? RedirectToRoleHome() : View(new RegisterViewModel());

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var registered = await _api.RegisterAsync(new RegisterRequest(model.FirstName.Trim(), model.LastName.Trim(), model.Email.Trim(), model.Password));
        if (!registered.Success)
        {
            ModelState.AddModelError(string.Empty, registered.Error);
            model.Password = model.ConfirmPassword = "";
            return View(model);
        }

        //Sign the new Participant straight in so they can enrol right away.
        HttpContext.Session.Clear();
        var login = await _api.LoginAsync(new LoginRequest(model.Email.Trim(), model.Password, AccountRole.Participant));
        if (!login.Success)
        {
            Success("Your account was created. Please sign in.");
            return RedirectToAction(nameof(Login));
        }
        await AccountSession.SignInAsync(HttpContext, login.Data!);
        Success($"Welcome to RaceDay, {login.Data!.FirstName}! Your account is ready.");
        return RedirectToAction("Index", "Home", new { area = "" });
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _api.LogoutAsync();
        await AccountSession.SignOutAsync(HttpContext);
        return RedirectToAction(nameof(Login), new { loggedOut = true });
    }

    [AllowAnonymous, HttpGet]
    public IActionResult AccessDenied() => View();
}
