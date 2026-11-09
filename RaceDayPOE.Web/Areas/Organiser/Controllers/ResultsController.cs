using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using RaceDayPOE.Web.Models.Api;
using RaceDayPOE.Web.Models.ViewModels;
using RaceDayPOE.Web.Services;

namespace RaceDayPOE.Web.Areas.Organiser.Controllers;

//Capture finish times and positions after an event. Saved results are visible to participants straight away.
public class ResultsController : OrganiserController
{
    private static readonly Regex TimePattern = new(@"^\d{1,2}:[0-5]\d:[0-5]\d$", RegexOptions.Compiled);
    public ResultsController(IRaceDayApi api) : base(api) { }

    [HttpGet]
    public async Task<IActionResult> Index(int? eventId)
    {
        var events = await LoadMyEventsAsync();
        var model = new ResultsPageViewModel { Events = events };
        if (eventId is null) return View(model);

        model.Event = events.FirstOrDefault(e => e.EventID == eventId);
        if (model.Event is null) return NotFound();

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId.Value);
        var results = await Api.GetEventResultsAsync(eventId.Value);
        if (!enrolments.Success) Error(enrolments.Error);
        else if (!results.Success) Error(results.Error);

        var byEnrolment = (results.Data ?? new()).ToDictionary(r => r.EnrolmentID);
        model.Rows = (enrolments.Data ?? new())
            .Where(e => e.Status != EnrolmentStatus.Cancelled)
            .Select(e => new ResultRow { Enrolment = e, Result = byEnrolment.GetValueOrDefault(e.EnrolmentID) })
            .OrderBy(r => r.Enrolment.CategoryName)
            .ThenBy(r => r.Result?.Position ?? int.MaxValue)
            .ThenBy(r => r.Enrolment.ParticipantName).ToList();
        return View(model);
    }

    //Creates the result, or updates it if one already exists for this enrolment.
    [HttpPost]
    public async Task<IActionResult> Save(int eventId, int enrolmentId, int? resultId, string? finishTime, int? position)
    {
        var text = finishTime?.Trim() ?? "";
        if (!TimePattern.IsMatch(text) || !TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var time))
        {
            Error("Enter the finish time as hh:mm:ss, for example 00:45:12.");
            return RedirectToAction(nameof(Index), new { eventId });
        }
        if (position is null or < 1)
        {
            Error("Enter the finishing position as a whole number of 1 or more.");
            return RedirectToAction(nameof(Index), new { eventId });
        }

        var request = new ResultRequest(enrolmentId, time, position.Value);
        var result = resultId is int id ? await Api.UpdateResultAsync(id, request) : await Api.CreateResultAsync(request);
        if (result.Success) Success(resultId is null ? "Result saved and published." : "Result updated.");
        else Error(result.Error);
        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int eventId, int resultId)
    {
        var result = await Api.DeleteResultAsync(resultId);
        if (result.Success) Success("The result has been removed.");
        else Error(result.Error);
        return RedirectToAction(nameof(Index), new { eventId });
    }
}
