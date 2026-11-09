using Microsoft.AspNetCore.Mvc;
using RaceDayPOE.Web.Models.Api;
using RaceDayPOE.Web.Models.ViewModels;
using RaceDayPOE.Web.Services;

namespace RaceDayPOE.Web.Areas.Organiser.Controllers;

//Venues are the "location" of an event, so Organisers need to be able to add them.
public class VenuesController : OrganiserController
{
    public VenuesController(IRaceDayApi api) : base(api) { }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await Api.GetVenuesAsync();
        if (!result.Success) Error(result.Error);
        return View((result.Data ?? new()).OrderBy(v => v.Name).ToList());
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new VenueForm());

    [HttpPost]
    public async Task<IActionResult> Create(VenueForm form)
    {
        if (!ModelState.IsValid) return View("Form", form);
        var result = await Api.CreateVenueAsync(ToRequest(form));
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Error); return View("Form", form); }
        Success($"Venue \"{form.Name}\" has been added.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await Api.GetVenueAsync(id);
        if (!result.Success) return result.StatusCode == 404 ? NotFound() : FailBack(result.Error);
        var v = result.Data!;
        return View("Form", new VenueForm { VenueId = id, Name = v.Name, AddressLine = v.AddressLine, City = v.City, Province = v.Province, Latitude = v.Latitude, Longitude = v.Longitude });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, VenueForm form)
    {
        form.VenueId = id;
        if (!ModelState.IsValid) return View("Form", form);
        var result = await Api.UpdateVenueAsync(id, ToRequest(form));
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Error); return View("Form", form); }
        Success($"Venue \"{form.Name}\" has been updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await Api.DeleteVenueAsync(id);
        if (result.Success) Success("The venue has been deleted.");
        else Error(result.Error);
        return RedirectToAction(nameof(Index));
    }

    private IActionResult FailBack(string message) { Error(message); return RedirectToAction(nameof(Index)); }
    private static VenueRequest ToRequest(VenueForm f) => new(f.Name.Trim(), f.AddressLine.Trim(), f.City.Trim(), f.Province.Trim(), f.Latitude, f.Longitude);
}
