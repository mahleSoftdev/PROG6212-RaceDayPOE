using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDayPOE.Web.Controllers;
using RaceDayPOE.Web.Infrastructure;
using RaceDayPOE.Web.Models.Api;
using RaceDayPOE.Web.Services;

namespace RaceDayPOE.Web.Areas.Organiser.Controllers;

//Everything in the Organiser area requires a signed-in Organiser. Participants are shown "access denied".
[Area("Organiser"), Authorize(Roles = RoleNames.Organiser)]
public abstract class OrganiserController : AppController
{
    protected readonly IRaceDayApi Api;
    protected OrganiserController(IRaceDayApi api) => Api = api;

    //The Organiser's own events. Event-scoped pages only ever work on events from this list.
    protected async Task<List<EventDto>> LoadMyEventsAsync()
    {
        var result = await Api.GetMyEventsAsync();
        if (!result.Success) Error(result.Error);
        return result.Data ?? new();
    }
}
