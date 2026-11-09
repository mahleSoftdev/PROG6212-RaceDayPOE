using Microsoft.AspNetCore.Mvc;
using RaceDayPOE.Web.Services;

namespace RaceDayPOE.Web.Areas.Participant.Controllers;

public class EnrolmentsController : ParticipantController
{
    private readonly IRaceDayApi _api;
    public EnrolmentsController(IRaceDayApi api) => _api = api;

    //My enrolments and their status.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMyEnrolmentsAsync();
        if (!result.Success) Error(result.Error);
        return View((result.Data ?? new()).OrderByDescending(e => e.EventDate ?? e.EnrolmentDate).ToList());
    }

    //Enrol in the chosen category. The API records the enrolment immediately and enforces capacity and duplicates.
    [HttpPost]
    public async Task<IActionResult> Create(int eventId, int? categoryId)
    {
        if (categoryId is null)
        {
            Warning("Choose a category before enrolling.");
            return RedirectToAction("Details", "Events", new { area = "", id = eventId });
        }
        var result = await _api.EnrolAsync(eventId, categoryId.Value);
        if (result.Success)
        {
            Success("You're in! Your enrolment has been recorded.");
            return RedirectToAction(nameof(Index));
        }
        Error(result.Error);
        return RedirectToAction("Details", "Events", new { area = "", id = eventId });
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _api.CancelEnrolmentAsync(id);
        if (result.Success) Success("Your enrolment has been cancelled.");
        else Error(result.Error);
        return RedirectToAction(nameof(Index));
    }
}
