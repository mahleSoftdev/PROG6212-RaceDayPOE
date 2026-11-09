using Microsoft.Extensions.Logging.Abstractions;
using RaceDayPOE.Web.Infrastructure;
using RaceDayPOE.Web.Models.Api;
using RaceDayPOE.Web.Services;

namespace RaceDayPOE.Web.Tests;

//The typed client turns HTTP into ApiResult values and friendly messages.
public class RaceDayApiTests
{
    private static RaceDayApi ApiFor(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) =>
        new(new HttpClient(new FakeHandler(respond)) { BaseAddress = new Uri("http://api.test/") }, NullLogger<RaceDayApi>.Instance);

    private static RaceDayApi ApiFor(Func<HttpRequestMessage, HttpResponseMessage> respond) => ApiFor(r => Task.FromResult(respond(r)));

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    public async Task Login_WithBadCredentials_GivesOneVagueMessage(int status)
    {
        var api = ApiFor(_ => Http.Json(status, "{\"message\":\"Invalid email, password, or role.\"}"));
        var result = await api.LoginAsync(new LoginRequest("a@b.co", "wrong-password", AccountRole.Participant));
        Assert.False(result.Success);
        Assert.Equal("Invalid email, password, or role.", result.Error);
    }

    [Fact]
    public async Task ProtectedCall_Returning401_RaisesSessionExpired()
    {
        var api = ApiFor(_ => new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));
        await Assert.ThrowsAsync<ApiUnauthorizedException>(() => api.GetMyEnrolmentsAsync());
    }

    [Fact]
    public async Task UnreachableApi_GivesAFriendlyMessage_NotAnException()
    {
        var api = ApiFor(new Func<HttpRequestMessage, HttpResponseMessage>(_ => throw new HttpRequestException("connection refused")));
        var result = await api.GetEventsAsync();
        Assert.False(result.Success);
        Assert.Equal(ApiMessages.Unreachable, result.Error);
    }

    [Fact]
    public async Task GetEvents_ParsesEnumsCategoriesAndCapacity()
    {
        var json = "[{\"eventID\":1,\"name\":\"Night Ride\",\"description\":\"d\",\"eventDate\":\"2026-12-01T00:00:00\",\"eventType\":\"Cycle\",\"venueID\":2,\"venue\":\"Track\",\"bannerUrl\":null," +
                   "\"categories\":[{\"categoryID\":3,\"eventID\":1,\"name\":\"40km\",\"distanceKm\":40,\"maxParticipants\":5,\"entryFee\":220.5,\"enrolled\":5}]}]";
        var api = ApiFor(_ => Http.Json(200, json));

        var result = await api.GetEventsAsync();

        Assert.True(result.Success);
        var e = Assert.Single(result.Data!);
        Assert.Equal(EventType.Cycle, e.EventType);
        Assert.Equal(new DateTime(2026, 12, 1), e.EventDate);
        var category = Assert.Single(e.Categories);
        Assert.True(category.IsFull);
        Assert.Equal(0, category.SpotsLeft);
        Assert.Equal(220.5m, category.EntryFee);
    }

    [Fact]
    public async Task Enrol_PostsTheCategoryToTheEventsEnrolmentEndpoint()
    {
        HttpMethod? method = null; string? path = null; string? body = null;
        var api = ApiFor(async r =>
        {
            method = r.Method; path = r.RequestUri!.AbsolutePath; body = await r.Content!.ReadAsStringAsync();
            return Http.Json(201, "{}");
        });

        var result = await api.EnrolAsync(20, 31);

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/api/events/20/enrolments", path);
        Assert.Equal("{\"categoryID\":31}", body);
    }

    [Fact]
    public async Task CreateResult_SendsFinishTimeAsHhMmSs()
    {
        string? body = null;
        var api = ApiFor(async r => { body = await r.Content!.ReadAsStringAsync(); return Http.Json(201, "{}"); });

        await api.CreateResultAsync(new ResultRequest(41, new TimeSpan(0, 45, 12), 1));

        Assert.Equal("{\"enrolmentID\":41,\"finishTime\":\"00:45:12\",\"position\":1}", body);
    }

    [Fact]
    public async Task Conflict_SurfacesTheApisMessage()
    {
        var api = ApiFor(_ => Http.Json(409, "{\"message\":\"You are already enrolled in this category.\"}"));
        var result = await api.EnrolAsync(20, 31);
        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("You are already enrolled in this category.", result.Error);
    }

    [Fact]
    public async Task UploadBanner_SendsMultipartWithAFilePart_ToTheApi()
    {
        string? path = null; string? contentType = null; string? body = null;
        var api = ApiFor(async r =>
        {
            path = r.RequestUri!.AbsolutePath; contentType = r.Content!.Headers.ContentType!.MediaType; body = await r.Content.ReadAsStringAsync();
            return Http.Json(200, "{\"bannerUrl\":\"https://blob.example/events/x.png\"}");
        });

        var result = await api.UploadEventBannerAsync(20, new MemoryStream(new byte[] { 1, 2, 3 }), "banner.png", "image/png");

        Assert.True(result.Success);
        Assert.Equal("https://blob.example/events/x.png", result.Data!.BannerUrl);
        Assert.Equal("/api/events/20/banner", path);
        Assert.Equal("multipart/form-data", contentType);
        Assert.Contains("name=file", body!.Replace("\"", ""));
        Assert.Contains("filename=banner.png", body.Replace("\"", ""));
    }

    [Fact]
    public async Task GarbledResponse_IsReportedFriendly()
    {
        var api = ApiFor(_ => Http.Json(200, "<html>not json</html>"));
        var result = await api.GetEventsAsync();
        Assert.False(result.Success);
        Assert.Equal(ApiMessages.Unreadable, result.Error);
    }
}
