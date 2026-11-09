using Microsoft.AspNetCore.Mvc;
using RaceDayPOE.Web.Models.ViewModels;
using RaceDayPOE.Web.Services;

namespace RaceDayPOE.Web.Areas.Organiser.Controllers;

public class DashboardController : OrganiserController
{
    public DashboardController(IRaceDayApi api) : base(api) { }

    //Summary of the Organiser's events, enrolments per event and upcoming dates.
    [HttpGet]
    public async Task<IActionResult> Index() => View(new DashboardViewModel { Events = await LoadMyEventsAsync() });
}
