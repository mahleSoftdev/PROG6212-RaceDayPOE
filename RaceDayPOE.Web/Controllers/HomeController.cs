using Microsoft.AspNetCore.Mvc;

namespace RaceDayPOE.Web.Controllers;

public class HomeController : AppController
{
    public IActionResult Index() => View();
}
