using Microsoft.AspNetCore.Mvc;
using RaceDayPOE.Web.Models.Api;
using RaceDayPOE.Web.Models.ViewModels;
using RaceDayPOE.Web.Services;

namespace RaceDayPOE.Web.Areas.Organiser.Controllers;

public class EventsController : OrganiserController
{
    public EventsController(IRaceDayApi api) : base(api) { }

    [HttpGet]
    public async Task<IActionResult> Index() => View(await LoadMyEventsAsync());

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var form = new EventFormViewModel { EventDate = DateTime.Today.AddDays(30) };
        await LoadVenuesAsync(form);
        return View("Form", form);
    }

    [HttpPost, RequestSizeLimit(7 * 1024 * 1024)]
    public async Task<IActionResult> Create(EventFormViewModel form)
    {
        if (form.DistanceKm is null) ModelState.AddModelError(nameof(form.DistanceKm), "Enter the main distance in km.");
        if (!ModelState.IsValid) return await ReshowAsync(form);

        var created = await Api.CreateEventAsync(ToRequest(form));
        if (!created.Success)
        {
            ModelState.AddModelError(string.Empty, created.Error);
            return await ReshowAsync(form);
        }

        var eventId = created.Data!.EventID;
        //The event exists now; anything that fails below is reported, never silently dropped.
        var problems = new List<string>();
        var category = await Api.CreateCategoryAsync(eventId, new CategoryRequest($"{form.DistanceKm!.Value:0.##}km", form.DistanceKm.Value, form.MaxParticipants, form.EntryFee));
        if (!category.Success) problems.Add($"the main category couldn't be added ({category.Error})");
        var banner = await UploadBannerAsync(eventId, form.Banner);
        if (banner is not null) problems.Add($"the banner wasn't saved ({banner})");

        if (problems.Count == 0) Success($"\"{form.Name}\" has been created.");
        else Warning($"\"{form.Name}\" was created, but {string.Join(" and ", problems)}. You can fix this from the event's Edit and Categories pages.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var existing = (await LoadMyEventsAsync()).FirstOrDefault(e => e.EventID == id);
        if (existing is null) return NotFound();
        var form = new EventFormViewModel
        {
            EventId = id, Name = existing.Name, Description = existing.Description, EventDate = existing.EventDate,
            EventType = existing.EventType, VenueID = existing.VenueID, ExistingBannerUrl = existing.BannerUrl
        };
        await LoadVenuesAsync(form);
        return View("Form", form);
    }

    [HttpPost, RequestSizeLimit(7 * 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, EventFormViewModel form)
    {
        form.EventId = id;
        //Distance, fee and capacity are only collected when creating; categories are managed on their own page.
        ModelState.Remove(nameof(form.DistanceKm)); ModelState.Remove(nameof(form.EntryFee)); ModelState.Remove(nameof(form.MaxParticipants));
        if (!ModelState.IsValid) return await ReshowAsync(form);

        var updated = await Api.UpdateEventAsync(id, ToRequest(form));
        if (!updated.Success)
        {
            ModelState.AddModelError(string.Empty, updated.Error);
            return await ReshowAsync(form);
        }
        var banner = await UploadBannerAsync(id, form.Banner);
        if (banner is null) Success($"\"{form.Name}\" has been updated.");
        else Warning($"\"{form.Name}\" was updated, but the banner wasn't saved ({banner}).");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await Api.DeleteEventAsync(id);
        if (result.Success) Success("The event has been deleted.");
        else Error(result.Error);
        return RedirectToAction(nameof(Index));
    }

    private static EventRequest ToRequest(EventFormViewModel f) => new(f.Name.Trim(), f.Description.Trim(), f.EventDate!.Value.Date, f.EventType, f.VenueID);

    //Returns null on success (or when no file was chosen), otherwise the friendly reason it failed.
    private async Task<string?> UploadBannerAsync(int eventId, IFormFile? file)
    {
        if (file is null || file.Length == 0) return null;
        await using var stream = file.OpenReadStream();
        var result = await Api.UploadEventBannerAsync(eventId, stream, file.FileName, file.ContentType);
        return result.Success ? null : result.Error;
    }

    private async Task LoadVenuesAsync(EventFormViewModel form)
    {
        var venues = await Api.GetVenuesAsync();
        if (!venues.Success) Error(venues.Error);
        form.Venues = venues.Data ?? new();
    }

    private async Task<IActionResult> ReshowAsync(EventFormViewModel form)
    {
        await LoadVenuesAsync(form);
        if (form.IsEdit)
            form.ExistingBannerUrl = (await LoadMyEventsAsync()).FirstOrDefault(e => e.EventID == form.EventId)?.BannerUrl;
        return View("Form", form);
    }
}
