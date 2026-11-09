using Microsoft.AspNetCore.Mvc;
using RaceDayPOE.Web.Models.Api;
using RaceDayPOE.Web.Models.ViewModels;
using RaceDayPOE.Web.Services;

namespace RaceDayPOE.Web.Areas.Organiser.Controllers;

//Everyone enrolled in one of the Organiser's events, with their category and status.
public class EnrolmentsController : OrganiserController
{
    public EnrolmentsController(IRaceDayApi api) : base(api) { }

    [HttpGet]
    public async Task<IActionResult> Index(int? eventId, EnrolmentStatus? status)
    {
        var events = await LoadMyEventsAsync();
        var model = new EnrolmentsPageViewModel { Events = events, StatusFilter = status };
        if (eventId is null) return View(model);

        model.Event = events.FirstOrDefault(e => e.EventID == eventId);
        if (model.Event is null) return NotFound();
        var result = await Api.GetEventEnrolmentsAsync(eventId.Value);
        if (!result.Success) Error(result.Error);
        model.Enrolments = (result.Data ?? new())
            .Where(e => status is null || e.Status == status)
            .OrderBy(e => e.CategoryName).ThenBy(e => e.ParticipantName).ToList();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int eventId, int id, EnrolmentStatus status)
    {
        var result = await Api.UpdateEnrolmentStatusAsync(id, status);
        if (result.Success) Success($"Enrolment marked as {status}.");
        else Error(result.Error);
        return RedirectToAction(nameof(Index), new { eventId });
    }
}
