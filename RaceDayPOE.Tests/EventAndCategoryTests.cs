using System.Net;
using System.Net.Http.Json;

namespace RaceDayPOE.Tests;

/// <summary>Event and category management, ownership and role enforcement.</summary>
public class EventAndCategoryTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public EventAndCategoryTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Events_CanBeBrowsedWithoutLogin()
    {
        var response = await TestSupport.NewClient(_factory).GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_Unauthenticated_ReturnsUnauthorized()
    {
        var venueId = await TestSupport.SeedVenueAsync(_factory);
        var response = await TestSupport.NewClient(_factory).PostAsJsonAsync("/api/events", TestSupport.EventBody(venueId));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_ReturnsForbidden()
    {
        var venueId = await TestSupport.SeedVenueAsync(_factory);
        var participant = await TestSupport.NewParticipantClientAsync(_factory);
        var response = await participant.PostAsJsonAsync("/api/events", TestSupport.EventBody(venueId));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_IsVisibleToEveryone()
    {
        var venueId = await TestSupport.SeedVenueAsync(_factory);
        var organiser = await TestSupport.NewOrganiserClientAsync(_factory);
        var eventId = await TestSupport.CreateEventAsync(organiser, venueId, "Visible Race");

        var anonymous = await TestSupport.NewClient(_factory).GetAsync($"/api/events/{eventId}");
        Assert.Equal(HttpStatusCode.OK, anonymous.StatusCode);
        Assert.Equal("Visible Race", TestSupport.StringProp(await TestSupport.ReadAsync(anonymous), "name"));
    }

    [Fact]
    public async Task CreateEvent_UnknownVenue_ReturnsBadRequest()
    {
        var organiser = await TestSupport.NewOrganiserClientAsync(_factory);
        var response = await organiser.PostAsJsonAsync("/api/events", TestSupport.EventBody(venueId: 999999));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_ReturnsBadRequest()
    {
        var venueId = await TestSupport.SeedVenueAsync(_factory);
        var organiser = await TestSupport.NewOrganiserClientAsync(_factory);
        var response = await organiser.PostAsJsonAsync("/api/events",
            new { name = "Bad", description = "Bad type", eventDate = "2027-04-01", eventType = "Swim", venueID = venueId });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_Unknown_ReturnsNotFound()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await TestSupport.NewClient(_factory).GetAsync("/api/events/999999")).StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_Owner_Succeeds_AndChangeIsStored()
    {
        var venueId = await TestSupport.SeedVenueAsync(_factory);
        var organiser = await TestSupport.NewOrganiserClientAsync(_factory);
        var eventId = await TestSupport.CreateEventAsync(organiser, venueId);

        var update = await organiser.PutAsJsonAsync($"/api/events/{eventId}", TestSupport.EventBody(venueId, "Renamed Event"));
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var get = await TestSupport.ReadAsync(await organiser.GetAsync($"/api/events/{eventId}"));
        Assert.Equal("Renamed Event", TestSupport.StringProp(get, "name"));
    }

    [Fact]
    public async Task UpdateAndDeleteEvent_ByDifferentOrganiser_ReturnsForbidden()
    {
        var venueId = await TestSupport.SeedVenueAsync(_factory);
        var owner = await TestSupport.NewOrganiserClientAsync(_factory);
        var other = await TestSupport.NewOrganiserClientAsync(_factory);
        var eventId = await TestSupport.CreateEventAsync(owner, venueId);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsJsonAsync($"/api/events/{eventId}", TestSupport.EventBody(venueId, "Hijack"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/events/{eventId}")).StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_Missing_ReturnsNotFound()
    {
        var venueId = await TestSupport.SeedVenueAsync(_factory);
        var organiser = await TestSupport.NewOrganiserClientAsync(_factory);
        Assert.Equal(HttpStatusCode.NotFound, (await organiser.PutAsJsonAsync("/api/events/999999", TestSupport.EventBody(venueId))).StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WithoutCategories_Succeeds_ThenIsGone()
    {
        var venueId = await TestSupport.SeedVenueAsync(_factory);
        var organiser = await TestSupport.NewOrganiserClientAsync(_factory);
        var eventId = await TestSupport.CreateEventAsync(organiser, venueId);

        Assert.Equal(HttpStatusCode.NoContent, (await organiser.DeleteAsync($"/api/events/{eventId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await organiser.GetAsync($"/api/events/{eventId}")).StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WithCategories_ReturnsConflict()
    {
        var venueId = await TestSupport.SeedVenueAsync(_factory);
        var organiser = await TestSupport.NewOrganiserClientAsync(_factory);
        var eventId = await TestSupport.CreateEventAsync(organiser, venueId);
        await TestSupport.CreateCategoryAsync(organiser, eventId);

        Assert.Equal(HttpStatusCode.Conflict, (await organiser.DeleteAsync($"/api/events/{eventId}")).StatusCode);
    }

    [Fact]
    public async Task Category_CreatedByOwner_IsListedPubliclyForTheEvent()
    {
        var venueId = await TestSupport.SeedVenueAsync(_factory);
        var organiser = await TestSupport.NewOrganiserClientAsync(_factory);
        var eventId = await TestSupport.CreateEventAsync(organiser, venueId);
        await TestSupport.CreateCategoryAsync(organiser, eventId);

        var list = await TestSupport.NewClient(_factory).GetAsync($"/api/categories?eventId={eventId}");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(1, (await TestSupport.ReadAsync(list)).GetArrayLength());
    }

    [Fact]
    public async Task Category_CreateAsParticipant_ReturnsForbidden()
    {
        var venueId = await TestSupport.SeedVenueAsync(_factory);
        var organiser = await TestSupport.NewOrganiserClientAsync(_factory);
        var eventId = await TestSupport.CreateEventAsync(organiser, venueId);
        var participant = await TestSupport.NewParticipantClientAsync(_factory);

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/categories", new { name = "x", distanceKm = 5, maxParticipants = 10, entryFee = 50 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Category_CreateByDifferentOrganiser_ReturnsForbidden()
    {
        var venueId = await TestSupport.SeedVenueAsync(_factory);
        var owner = await TestSupport.NewOrganiserClientAsync(_factory);
        var other = await TestSupport.NewOrganiserClientAsync(_factory);
        var eventId = await TestSupport.CreateEventAsync(owner, venueId);

        var response = await other.PostAsJsonAsync($"/api/events/{eventId}/categories", new { name = "x", distanceKm = 5, maxParticipants = 10, entryFee = 50 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Category_InvalidBody_ReturnsBadRequest()
    {
        var venueId = await TestSupport.SeedVenueAsync(_factory);
        var organiser = await TestSupport.NewOrganiserClientAsync(_factory);
        var eventId = await TestSupport.CreateEventAsync(organiser, venueId);

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new { name = "x", distanceKm = 0, maxParticipants = 0, entryFee = -1 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Venue_OrganiserCanCreate_ParticipantCannot()
    {
        var body = new { name = "New Venue", addressLine = "5 Main Road", city = "Durban", province = "KwaZulu-Natal", latitude = -29.8m, longitude = 31.0m };
        var organiser = await TestSupport.NewOrganiserClientAsync(_factory);
        var participant = await TestSupport.NewParticipantClientAsync(_factory);

        Assert.Equal(HttpStatusCode.Created, (await organiser.PostAsJsonAsync("/api/venues", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await participant.PostAsJsonAsync("/api/venues", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await TestSupport.NewClient(_factory).PostAsJsonAsync("/api/venues", body)).StatusCode);
    }
}
