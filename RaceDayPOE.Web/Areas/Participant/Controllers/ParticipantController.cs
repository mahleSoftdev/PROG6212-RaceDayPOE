using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDayPOE.Web.Controllers;
using RaceDayPOE.Web.Infrastructure;

namespace RaceDayPOE.Web.Areas.Participant.Controllers;

//Everything in the Participant area requires a signed-in Participant. Organisers are shown "access denied".
[Area("Participant"), Authorize(Roles = RoleNames.Participant)]
public abstract class ParticipantController : AppController { }
