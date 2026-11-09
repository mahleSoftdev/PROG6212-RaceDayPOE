using Microsoft.AspNetCore.Mvc;
using RaceDayPOE.Web.Infrastructure;
using RaceDayPOE.Web.Models.ViewModels;
using RaceDayPOE.Web.Services;

namespace RaceDayPOE.Web.Controllers;

public class HomeController : AppController
{
    private readonly IRaceDayApi _api;
    public HomeController(IRaceDayApi api) => _api = api;

    //Home page: upcoming events with filtering. Organisers land on their own dashboard instead.
    [HttpGet]
    public async Task<IActionResult> Index(EventFilterViewModel filter)
    {
        if (User.IsInRole(RoleNames.Organiser)) return RedirectToAction("Index", "Dashboard", new { area = "Organiser" });

        var result = await _api.GetEventsAsync();
        if (!result.Success) Error(result.Error);
        var all = result.Data ?? new();

        var today = DateTime.Today;
        filter.UpcomingCount = all.Count(e => e.EventDate.Date >= today);
        filter.Cities = all.Select(e => e.VenueCity).Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c!).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList();

        IEnumerable<Models.Api.EventDto> query = all;
        if (!filter.IncludePast) query = query.Where(e => e.EventDate.Date >= today);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(e => e.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || e.Description.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (e.Venue?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        if (filter.Type is not null) query = query.Where(e => e.EventType == filter.Type);
        if (!string.IsNullOrWhiteSpace(filter.City)) query = query.Where(e => string.Equals(e.VenueCity, filter.City, StringComparison.OrdinalIgnoreCase));
        if (filter.From is not null) query = query.Where(e => e.EventDate.Date >= filter.From.Value.Date);
        if (filter.To is not null) query = query.Where(e => e.EventDate.Date <= filter.To.Value.Date);

        filter.Events = query.OrderBy(e => e.EventDate).ToList();
        return View(filter);
    }

    [HttpGet("/Home/Error")]
    [HttpPost("/Home/Error")]
    public IActionResult Error() { Response.StatusCode = 500; return View(); }

    //Friendly page for 404s and other status codes (see UseStatusCodePagesWithReExecute).
    [HttpGet("/Home/StatusCode")]
    [HttpPost("/Home/StatusCode")]
    public IActionResult Status(int code = 404)
    {
        Response.StatusCode = code;
        ViewData["Code"] = code;
        return View();
    }
}
