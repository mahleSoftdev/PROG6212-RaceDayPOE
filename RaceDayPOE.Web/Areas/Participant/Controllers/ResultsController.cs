using Microsoft.AspNetCore.Mvc;
using RaceDayPOE.Web.Services;

namespace RaceDayPOE.Web.Areas.Participant.Controllers;

public class ResultsController : ParticipantController
{
    private readonly IRaceDayApi _api;
    public ResultsController(IRaceDayApi api) => _api = api;

    //My race history across all completed events I enrolled in.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMyResultsAsync();
        if (!result.Success) Error(result.Error);
        return View((result.Data ?? new()).OrderByDescending(r => r.EventDate ?? r.RecordedAt).ToList());
    }
}
