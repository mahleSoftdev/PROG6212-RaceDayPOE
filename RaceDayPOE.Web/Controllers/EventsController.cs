using Microsoft.AspNetCore.Mvc;
using RaceDayPOE.Web.Infrastructure;
using RaceDayPOE.Web.Models.Api;
using RaceDayPOE.Web.Models.ViewModels;
using RaceDayPOE.Web.Services;

namespace RaceDayPOE.Web.Controllers;

//Public event detail page. Participants also see which categories they are already enrolled in.
public class EventsController : AppController
{
    private readonly IRaceDayApi _api;
    public EventsController(IRaceDayApi api) => _api = api;

    [HttpGet("/Events/Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var result = await _api.GetEventAsync(id);
        if (!result.Success)
        {
            if (result.StatusCode == 404) return NotFound();
            Error(result.Error);
            return RedirectToAction("Index", "Home");
        }

        var model = new EventDetailsViewModel { Event = result.Data! };
        if (User.IsInRole(RoleNames.Participant))
        {
            var mine = await _api.GetMyEnrolmentsAsync();
            if (mine.Success)
                model.EnrolledCategoryIds = mine.Data!.Where(e => e.Status != EnrolmentStatus.Cancelled).Select(e => e.CategoryID).ToHashSet();
        }
        return View(model);
    }
}
