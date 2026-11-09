using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDayPOE.Web.Infrastructure;
using RaceDayPOE.Web.Models.Api;
using RaceDayPOE.Web.Models.ViewModels;
using RaceDayPOE.Web.Services;

namespace RaceDayPOE.Web.Controllers;

//View and update personal information, change password and (Participants) upload a profile picture.
[Authorize]
public class ProfileController : AppController
{
    private readonly IRaceDayApi _api;
    public ProfileController(IRaceDayApi api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var page = await LoadAsync();
        return page is null ? RedirectToRoleHome() : View(page);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateDetails([Bind(Prefix = "Details")] UpdateProfileForm form)
    {
        if (!ModelState.IsValid) return await ReshowAsync(form, null);

        var result = await _api.UpdateProfileAsync(new ProfileRequest(form.FirstName.Trim(), form.LastName.Trim(), form.Email.Trim()));
        if (!result.Success)
        {
            ModelState.AddModelError("Details", result.Error);
            return await ReshowAsync(form, null);
        }
        //Refresh the display name shown in the navigation.
        await AccountSession.SignInAsync(HttpContext, result.Data!);
        Success("Your details have been updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "Password")] ChangePasswordForm form)
    {
        if (!ModelState.IsValid) return await ReshowAsync(null, form);

        var result = await _api.ChangePasswordAsync(new PasswordRequest(form.CurrentPassword, form.NewPassword));
        if (!result.Success)
        {
            ModelState.AddModelError("Password", result.Error);
            return await ReshowAsync(null, form);
        }
        Success("Your password has been changed.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Participant), HttpPost, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture is null || picture.Length == 0)
        {
            Warning("Choose an image to upload first.");
            return RedirectToAction(nameof(Index));
        }
        await using var stream = picture.OpenReadStream();
        var result = await _api.UploadProfilePictureAsync(stream, picture.FileName, picture.ContentType);
        if (result.Success) Success("Your profile picture has been updated.");
        else Error(result.Error);
        return RedirectToAction(nameof(Index));
    }

    private async Task<ProfilePageViewModel?> LoadAsync()
    {
        var result = await _api.GetProfileAsync();
        if (!result.Success) { Error(result.Error); return null; }
        var a = result.Data!;
        return new ProfilePageViewModel { Account = a, Details = new UpdateProfileForm { FirstName = a.FirstName, LastName = a.LastName, Email = a.Email } };
    }

    //Re-display the page with the user's invalid input and the error messages.
    private async Task<IActionResult> ReshowAsync(UpdateProfileForm? details, ChangePasswordForm? password)
    {
        var page = await LoadAsync();
        if (page is null) return RedirectToRoleHome();
        if (details is not null) page.Details = details;
        return View("Index", page);
    }
}
