namespace RaceDayPOE.Web.Infrastructure;

//The browser talks to this MVC app, and this app talks to the API. The API identifies callers by its own
//authentication cookie, so this handler (1) captures that cookie from the API's responses into the user's
//MVC session and (2) attaches it to every later API request made on that user's behalf.
//It is per-user (taken from the current session), so users can never share an API identity.
public sealed class ApiCookieHandler : DelegatingHandler
{
    public const string ApiCookieName = ".RaceDay.Auth";
    private readonly IHttpContextAccessor _accessor;

    public ApiCookieHandler(IHttpContextAccessor accessor) => _accessor = accessor;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = _accessor.HttpContext?.Session;
        if (session is not null)
        {
            await session.LoadAsync(cancellationToken);
            var stored = session.GetString(SessionKeys.ApiAuth);
            if (!string.IsNullOrEmpty(stored))
                request.Headers.TryAddWithoutValidation("Cookie", $"{ApiCookieName}={stored}");
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (session is not null && response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var header in setCookies)
            {
                if (!header.StartsWith(ApiCookieName + "=", StringComparison.Ordinal)) continue;
                var value = header.Split(';', 2)[0][(ApiCookieName.Length + 1)..];
                var expired = value.Length == 0 || header.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase);
                if (expired) session.Remove(SessionKeys.ApiAuth);
                else session.SetString(SessionKeys.ApiAuth, value);
            }
        }
        return response;
    }
}
