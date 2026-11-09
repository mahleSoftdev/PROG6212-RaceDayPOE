using Microsoft.AspNetCore.Http;
using RaceDayPOE.Web.Infrastructure;

namespace RaceDayPOE.Web.Tests;

//The handler is what keeps each user's API identity separate: it lives in that user's session only.
public class ApiCookieHandlerTests
{
    private static HttpMessageInvoker InvokerFor(FakeSession session, Func<HttpRequestMessage, HttpResponseMessage> inner) =>
        new(new ApiCookieHandler(Http.AccessorWith(session)) { InnerHandler = new FakeHandler(inner) });

    [Fact]
    public async Task SendsTheSessionsApiCookieOnEveryRequest()
    {
        var session = new FakeSession();
        session.SetString(SessionKeys.ApiAuth, "abc123");
        string? seen = null;
        var invoker = InvokerFor(session, r => { seen = r.Headers.TryGetValues("Cookie", out var v) ? string.Join(";", v) : null; return new HttpResponseMessage(System.Net.HttpStatusCode.OK); });

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://api.test/api/enrolments"), CancellationToken.None);

        Assert.Equal(".RaceDay.Auth=abc123", seen);
    }

    [Fact]
    public async Task SendsNoCookieWhenTheUserIsNotSignedIn()
    {
        string? seen = "unset";
        var invoker = InvokerFor(new FakeSession(), r => { seen = r.Headers.TryGetValues("Cookie", out var v) ? string.Join(";", v) : null; return new HttpResponseMessage(System.Net.HttpStatusCode.OK); });

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://api.test/api/events"), CancellationToken.None);

        Assert.Null(seen);
    }

    [Fact]
    public async Task CapturesTheCookieTheApiIssuesAtLogin()
    {
        var session = new FakeSession();
        var invoker = InvokerFor(session, _ =>
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            response.Headers.TryAddWithoutValidation("Set-Cookie", ".RaceDay.Auth=CfDJ8-token_value; path=/; samesite=strict; httponly");
            return response;
        });

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Post, "http://api.test/api/auth/login"), CancellationToken.None);

        Assert.Equal("CfDJ8-token_value", session.GetString(SessionKeys.ApiAuth));
    }

    [Fact]
    public async Task RemovesTheCookieWhenTheApiExpiresIt()
    {
        var session = new FakeSession();
        session.SetString(SessionKeys.ApiAuth, "abc123");
        var invoker = InvokerFor(session, _ =>
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.NoContent);
            response.Headers.TryAddWithoutValidation("Set-Cookie", ".RaceDay.Auth=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/");
            return response;
        });

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Post, "http://api.test/api/auth/logout"), CancellationToken.None);

        Assert.Null(session.GetString(SessionKeys.ApiAuth));
    }

    [Fact]
    public async Task IgnoresOtherCookies()
    {
        var session = new FakeSession();
        var invoker = InvokerFor(session, _ =>
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            response.Headers.TryAddWithoutValidation("Set-Cookie", ".RaceDay.Session=zzz; path=/");
            return response;
        });

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://api.test/api/events"), CancellationToken.None);

        Assert.Null(session.GetString(SessionKeys.ApiAuth));
    }
}
