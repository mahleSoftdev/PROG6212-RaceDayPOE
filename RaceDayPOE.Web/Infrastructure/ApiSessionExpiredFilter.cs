using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDayPOE.Web.Infrastructure;

//If the API says the signed-in user is no longer authenticated, end the MVC session and send them to the login page.
public sealed class ApiSessionExpiredFilter : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is not ApiUnauthorizedException) return;
        var http = context.HttpContext;
        await AccountSession.SignOutAsync(http);
        var returnUrl = HttpMethods.IsGet(http.Request.Method) ? http.Request.GetEncodedPathAndQuery() : null;
        context.Result = new RedirectToActionResult("Login", "Account", new { area = "", expired = true, returnUrl });
        context.ExceptionHandled = true;
    }
}
