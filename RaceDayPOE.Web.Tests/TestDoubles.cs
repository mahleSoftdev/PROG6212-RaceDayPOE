using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace RaceDayPOE.Web.Tests;

//Responds to HTTP calls with whatever the test supplies; never touches the network.
internal sealed class FakeHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _respond;
    public FakeHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) => _respond = respond;
    public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : this(r => Task.FromResult(respond(r))) { }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => _respond(request);
}

//Minimal in-memory ISession so the cookie handler can be tested without a web server.
internal sealed class FakeSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();
    public bool IsAvailable => true;
    public string Id { get; } = Guid.NewGuid().ToString();
    public IEnumerable<string> Keys => _store.Keys;
    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, out byte[]? value) => _store.TryGetValue(key, out value);
}

internal sealed class FakeSessionFeature : ISessionFeature
{
    public ISession Session { get; set; } = new FakeSession();
}

internal static class Http
{
    public static HttpContextAccessor AccessorWith(ISession session)
    {
        var context = new DefaultHttpContext();
        context.Features.Set<ISessionFeature>(new FakeSessionFeature { Session = session });
        return new HttpContextAccessor { HttpContext = context };
    }

    public static HttpResponseMessage Json(int status, string json) =>
        new((System.Net.HttpStatusCode)status) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
}
