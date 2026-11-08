using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDayPOE.Tests;

//Read models that the MVC dashboard, enrolment list and "My Results" pages rely on.
public class OrganiserAndResultsReadModelTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public OrganiserAndResultsReadModelTests(ApiFactory factory) => _factory = factory;

    private static JsonElement FindEvent(JsonElement events, int eventId)
    {
        foreach (var e in events.EnumerateArray())
            if (TestSupport.IntProp(e, "eventID") == eventId) return e;
        throw new KeyNotFoundException($"Event {eventId} not in response");
    }

    [Fact]
    public async Task MyEvents_ListsOnlyTheOrganisersOwnEventsWithEnrolmentTotals()
    {
        var venueId = await TestSupport.SeedVenueAsync(_factory);
        var organiser = await TestSupport.NewOrganiserClientAsync(_factory);
        var other = await TestSupport.NewOrganiserClientAsync(_factory);
        var eventId = await TestSupport.CreateEventAsync(organiser, venueId, "Mine");
        var otherEventId = await TestSupport.CreateEventAsync(other, venueId, "Theirs");
        var categoryId = await TestSupport.CreateCategoryAsync(organiser, eventId);
        var runner = await TestSupport.NewParticipantClientAsync(_factory);
        await TestSupport.EnrolAsync(runner, categoryId);

        var response = await organiser.GetAsync("/api/organisers/me/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var events = await TestSupport.ReadAsync(response);
        var mine = FindEvent(events, eventId);
        Assert.Equal(1, TestSupport.IntProp(mine, "enrolmentCount"));
        Assert.Throws<KeyNotFoundException>(() => FindEvent(events, otherEventId));
    }

    [Fact]
    public async Task MyEvents_RequiresAnOrganiser()
    {
        var participant = await TestSupport.NewParticipantClientAsync(_factory);
        Assert.Equal(HttpStatusCode.Forbidden, (await participant.GetAsync("/api/organisers/me/events")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await TestSupport.NewClient(_factory).GetAsync("/api/organisers/me/events")).StatusCode);
    }

    [Fact]
    public async Task EventDetail_ShowsEnrolledCountPerCategory_AndIgnoresCancelled()
    {
        var venueId = await TestSupport.SeedVenueAsync(_factory);
        var organiser = await TestSupport.NewOrganiserClientAsync(_factory);
        var eventId = await TestSupport.CreateEventAsync(organiser, venueId);
        var categoryId = await TestSupport.CreateCategoryAsync(organiser, eventId);
        var staying = await TestSupport.NewParticipantClientAsync(_factory);
        var leaving = await TestSupport.NewParticipantClientAsync(_factory);
        await TestSupport.EnrolAsync(staying, categoryId);
        var cancelled = await TestSupport.EnrolAsync(leaving, categoryId);
        Assert.Equal(HttpStatusCode.NoContent, (await leaving.PutAsync($"/api/enrolments/{cancelled}/cancel", null)).StatusCode);

        var detail = await TestSupport.ReadAsync(await TestSupport.NewClient(_factory).GetAsync($"/api/events/{eventId}"));
        var category = detail.GetProperty("categories")[0];
        Assert.Equal(1, TestSupport.IntProp(category, "enrolled"));
        Assert.Equal("Community 5K", TestSupport.StringProp(detail, "name"));
    }

    [Fact]
    public async Task MyEnrolments_IncludeEventIdAndDate()
    {
        var venueId = await TestSupport.SeedVenueAsync(_factory);
        var organiser = await TestSupport.NewOrganiserClientAsync(_factory);
        var eventId = await TestSupport.CreateEventAsync(organiser, venueId);
        var categoryId = await TestSupport.CreateCategoryAsync(organiser, eventId);
        var runner = await TestSupport.NewParticipantClientAsync(_factory);
        await TestSupport.EnrolAsync(runner, categoryId);

        var list = await TestSupport.ReadAsync(await runner.GetAsync("/api/enrolments"));
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal(eventId, TestSupport.IntProp(list[0], "eventID"));
        Assert.StartsWith("2027-04-01", TestSupport.StringProp(list[0], "eventDate"));
    }

    [Fact]
    public async Task MyResults_IncludeEventDateCategoryAndFinisherCount()
    {
        var venueId = await TestSupport.SeedVenueAsync(_factory);
        var organiser = await TestSupport.NewOrganiserClientAsync(_factory);
        var eventId = await TestSupport.CreateEventAsync(organiser, venueId, "Results Race");
        var categoryId = await TestSupport.CreateCategoryAsync(organiser, eventId);
        var first = await TestSupport.NewParticipantClientAsync(_factory);
        var second = await TestSupport.NewParticipantClientAsync(_factory);
        var firstEnrolment = await TestSupport.EnrolAsync(first, categoryId);
        var secondEnrolment = await TestSupport.EnrolAsync(second, categoryId);
        Assert.Equal(HttpStatusCode.Created, (await organiser.PostAsJsonAsync("/api/results", new { enrolmentID = firstEnrolment, finishTime = "00:45:12", position = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await organiser.PostAsJsonAsync("/api/results", new { enrolmentID = secondEnrolment, finishTime = "00:51:03", position = 2 })).StatusCode);

        var results = await TestSupport.ReadAsync(await second.GetAsync("/api/participants/me/results"));
        Assert.Equal(1, results.GetArrayLength());
        var row = results[0];
        Assert.Equal("Results Race", TestSupport.StringProp(row, "eventName"));
        Assert.Equal("10km Open", TestSupport.StringProp(row, "categoryName"));
        Assert.Equal(2, TestSupport.IntProp(row, "position"));
        Assert.Equal(2, TestSupport.IntProp(row, "totalFinishers"));
        Assert.StartsWith("2027-04-01", TestSupport.StringProp(row, "eventDate"));
    }
}
