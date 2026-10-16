using System.Net;
using System.Net.Http.Json;

namespace RaceDayPOE.Tests;

//Enrolments (participant + event + category link) and results.
public class EnrolmentAndResultTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public EnrolmentAndResultTests(ApiFactory factory) => _factory = factory;

    private async Task<(HttpClient Organiser, int EventId, int CategoryId)> SetUpEventAsync(int maxParticipants = 100)
    {
        var venueId = await TestSupport.SeedVenueAsync(_factory);
        var organiser = await TestSupport.NewOrganiserClientAsync(_factory);
        var eventId = await TestSupport.CreateEventAsync(organiser, venueId, "Enrolment Event");
        var categoryId = await TestSupport.CreateCategoryAsync(organiser, eventId, maxParticipants);
        return (organiser, eventId, categoryId);
    }

    [Fact]
    public async Task Enrol_IsRecordedAgainstParticipantEventAndCategory()
    {
        var (organiser, eventId, categoryId) = await SetUpEventAsync();
        var participant = await TestSupport.NewParticipantClientAsync(_factory);
        var enrolmentId = await TestSupport.EnrolAsync(participant, categoryId);

        //The participant sees it, linked to the right category and event.
        var mine = await TestSupport.ReadAsync(await participant.GetAsync("/api/enrolments"));
        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal(enrolmentId, TestSupport.IntProp(mine[0], "enrolmentID"));
        Assert.Equal(categoryId, TestSupport.IntProp(mine[0], "categoryID"));
        Assert.Equal("Enrolment Event", TestSupport.StringProp(mine[0], "eventName"));

        //The organiser of the event sees it too.
        var forEvent = await TestSupport.ReadAsync(await organiser.GetAsync($"/api/events/{eventId}/enrolments"));
        Assert.Equal(1, forEvent.GetArrayLength());
        Assert.Equal(enrolmentId, TestSupport.IntProp(forEvent[0], "enrolmentID"));
    }

    [Fact]
    public async Task Enrol_ThroughEventSpecificEndpoint_Works_AndRejectsCategoryFromAnotherEvent()
    {
        var (_, eventId, categoryId) = await SetUpEventAsync();
        var (_, otherEventId, _) = await SetUpEventAsync();
        var participant = await TestSupport.NewParticipantClientAsync(_factory);

        Assert.Equal(HttpStatusCode.BadRequest, (await participant.PostAsJsonAsync($"/api/events/{otherEventId}/enrolments", new { categoryID = categoryId })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryID = categoryId })).StatusCode);
    }

    [Fact]
    public async Task Enrol_Unauthenticated_ReturnsUnauthorized()
    {
        var (_, _, categoryId) = await SetUpEventAsync();
        var response = await TestSupport.NewClient(_factory).PostAsJsonAsync("/api/enrolments", new { categoryID = categoryId });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_ReturnsForbidden()
    {
        var (organiser, _, categoryId) = await SetUpEventAsync();
        var response = await organiser.PostAsJsonAsync("/api/enrolments", new { categoryID = categoryId });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_UnknownCategory_ReturnsNotFound()
    {
        var participant = await TestSupport.NewParticipantClientAsync(_factory);
        Assert.Equal(HttpStatusCode.NotFound, (await participant.PostAsJsonAsync("/api/enrolments", new { categoryID = 999999 })).StatusCode);
    }

    [Fact]
    public async Task Enrol_Twice_ReturnsConflict()
    {
        var (_, _, categoryId) = await SetUpEventAsync();
        var participant = await TestSupport.NewParticipantClientAsync(_factory);
        await TestSupport.EnrolAsync(participant, categoryId);
        Assert.Equal(HttpStatusCode.Conflict, (await participant.PostAsJsonAsync("/api/enrolments", new { categoryID = categoryId })).StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenCategoryIsFull_ReturnsConflict()
    {
        var (_, _, categoryId) = await SetUpEventAsync(maxParticipants: 1);
        var first = await TestSupport.NewParticipantClientAsync(_factory);
        var second = await TestSupport.NewParticipantClientAsync(_factory);
        await TestSupport.EnrolAsync(first, categoryId);
        Assert.Equal(HttpStatusCode.Conflict, (await second.PostAsJsonAsync("/api/enrolments", new { categoryID = categoryId })).StatusCode);
    }

    [Fact]
    public async Task EventEnrolments_ParticipantAndOtherOrganiser_AreForbidden()
    {
        var (_, eventId, _) = await SetUpEventAsync();
        var participant = await TestSupport.NewParticipantClientAsync(_factory);
        var otherOrganiser = await TestSupport.NewOrganiserClientAsync(_factory);

        Assert.Equal(HttpStatusCode.Forbidden, (await participant.GetAsync($"/api/events/{eventId}/enrolments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherOrganiser.GetAsync($"/api/events/{eventId}/enrolments")).StatusCode);
    }

    [Fact]
    public async Task Cancel_OwnEnrolment_Succeeds_ThenSecondCancelConflicts_AndSpotIsFreed()
    {
        var (_, _, categoryId) = await SetUpEventAsync(maxParticipants: 1);
        var participant = await TestSupport.NewParticipantClientAsync(_factory);
        var enrolmentId = await TestSupport.EnrolAsync(participant, categoryId);

        Assert.Equal(HttpStatusCode.NoContent, (await participant.PutAsync($"/api/enrolments/{enrolmentId}/cancel", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await participant.PutAsync($"/api/enrolments/{enrolmentId}/cancel", null)).StatusCode);

        var another = await TestSupport.NewParticipantClientAsync(_factory);
        Assert.Equal(HttpStatusCode.Created, (await another.PostAsJsonAsync("/api/enrolments", new { categoryID = categoryId })).StatusCode);
    }

    [Fact]
    public async Task Cancel_SomeoneElsesEnrolment_ReturnsForbidden()
    {
        var (_, _, categoryId) = await SetUpEventAsync();
        var owner = await TestSupport.NewParticipantClientAsync(_factory);
        var other = await TestSupport.NewParticipantClientAsync(_factory);
        var enrolmentId = await TestSupport.EnrolAsync(owner, categoryId);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsync($"/api/enrolments/{enrolmentId}/cancel", null)).StatusCode);
    }

    [Fact]
    public async Task Result_OrganiserRecords_ParticipantSeesOnlyTheirOwn()
    {
        var (organiser, _, categoryId) = await SetUpEventAsync();
        var winner = await TestSupport.NewParticipantClientAsync(_factory);
        var bystander = await TestSupport.NewParticipantClientAsync(_factory);
        var enrolmentId = await TestSupport.EnrolAsync(winner, categoryId);

        var create = await organiser.PostAsJsonAsync("/api/results", new { enrolmentID = enrolmentId, finishTime = "00:45:12", position = 1 });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var winnerResults = await TestSupport.ReadAsync(await winner.GetAsync("/api/results"));
        Assert.Equal(1, winnerResults.GetArrayLength());
        Assert.Equal(1, TestSupport.IntProp(winnerResults[0], "position"));

        var bystanderResults = await TestSupport.ReadAsync(await bystander.GetAsync("/api/results"));
        Assert.Equal(0, bystanderResults.GetArrayLength());
    }

    [Fact]
    public async Task Result_Duplicate_ReturnsConflict()
    {
        var (organiser, _, categoryId) = await SetUpEventAsync();
        var participant = await TestSupport.NewParticipantClientAsync(_factory);
        var enrolmentId = await TestSupport.EnrolAsync(participant, categoryId);
        var body = new { enrolmentID = enrolmentId, finishTime = "00:50:00", position = 2 };

        Assert.Equal(HttpStatusCode.Created, (await organiser.PostAsJsonAsync("/api/results", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await organiser.PostAsJsonAsync("/api/results", body)).StatusCode);
    }

    [Fact]
    public async Task Result_ByParticipantOrOtherOrganiser_ReturnsForbidden()
    {
        var (_, _, categoryId) = await SetUpEventAsync();
        var participant = await TestSupport.NewParticipantClientAsync(_factory);
        var otherOrganiser = await TestSupport.NewOrganiserClientAsync(_factory);
        var enrolmentId = await TestSupport.EnrolAsync(participant, categoryId);
        var body = new { enrolmentID = enrolmentId, finishTime = "00:50:00", position = 2 };

        Assert.Equal(HttpStatusCode.Forbidden, (await participant.PostAsJsonAsync("/api/results", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherOrganiser.PostAsJsonAsync("/api/results", body)).StatusCode);
    }

    [Fact]
    public async Task Result_InvalidFinishTime_ReturnsBadRequest()
    {
        var (organiser, _, categoryId) = await SetUpEventAsync();
        var participant = await TestSupport.NewParticipantClientAsync(_factory);
        var enrolmentId = await TestSupport.EnrolAsync(participant, categoryId);

        var response = await organiser.PostAsJsonAsync("/api/results", new { enrolmentID = enrolmentId, finishTime = "1.00:00:00", position = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Result_UnknownEnrolment_ReturnsNotFound()
    {
        var organiser = await TestSupport.NewOrganiserClientAsync(_factory);
        var response = await organiser.PostAsJsonAsync("/api/results", new { enrolmentID = 999999, finishTime = "00:30:00", position = 1 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Results_Unauthenticated_ReturnsUnauthorized()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await TestSupport.NewClient(_factory).GetAsync("/api/results")).StatusCode);
    }
}
