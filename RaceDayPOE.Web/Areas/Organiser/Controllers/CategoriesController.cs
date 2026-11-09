using Microsoft.AspNetCore.Mvc;
using RaceDayPOE.Web.Models.Api;
using RaceDayPOE.Web.Models.ViewModels;
using RaceDayPOE.Web.Services;

namespace RaceDayPOE.Web.Areas.Organiser.Controllers;

//Add age or distance categories to an event. Pick an event first, then manage its categories.
public class CategoriesController : OrganiserController
{
    public CategoriesController(IRaceDayApi api) : base(api) { }

    [HttpGet]
    public async Task<IActionResult> Index(int? eventId, int? editId)
    {
        var events = await LoadMyEventsAsync();
        var model = new CategoriesPageViewModel { Events = events };
        if (eventId is null) return View(model);

        model.Event = events.FirstOrDefault(e => e.EventID == eventId);
        if (model.Event is null) return NotFound();
        if (editId is not null)
        {
            var category = model.Event.Categories.FirstOrDefault(c => c.CategoryID == editId);
            if (category is null) return NotFound();
            model.EditingCategoryId = editId;
            model.Form = new CategoryForm { Name = category.Name, DistanceKm = category.DistanceKm, MaxParticipants = category.MaxParticipants, EntryFee = category.EntryFee };
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int eventId, [Bind(Prefix = "Form")] CategoryForm form)
    {
        if (!ModelState.IsValid) return await ReshowAsync(eventId, form, null);
        var result = await Api.CreateCategoryAsync(eventId, new CategoryRequest(form.Name.Trim(), form.DistanceKm, form.MaxParticipants, form.EntryFee));
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Error); return await ReshowAsync(eventId, form, null); }
        Success($"Category \"{form.Name}\" has been added.");
        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> Update(int eventId, int id, [Bind(Prefix = "Form")] CategoryForm form)
    {
        if (!ModelState.IsValid) return await ReshowAsync(eventId, form, id);
        var result = await Api.UpdateCategoryAsync(id, new CategoryRequest(form.Name.Trim(), form.DistanceKm, form.MaxParticipants, form.EntryFee));
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Error); return await ReshowAsync(eventId, form, id); }
        Success($"Category \"{form.Name}\" has been updated.");
        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int eventId, int id)
    {
        var result = await Api.DeleteCategoryAsync(id);
        if (result.Success) Success("The category has been deleted.");
        else Error(result.Error);
        return RedirectToAction(nameof(Index), new { eventId });
    }

    private async Task<IActionResult> ReshowAsync(int eventId, CategoryForm form, int? editingId)
    {
        var events = await LoadMyEventsAsync();
        var selected = events.FirstOrDefault(e => e.EventID == eventId);
        if (selected is null) return NotFound();
        return View("Index", new CategoriesPageViewModel { Events = events, Event = selected, Form = form, EditingCategoryId = editingId });
    }
}
