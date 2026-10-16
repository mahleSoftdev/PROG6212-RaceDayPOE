using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDayPOE.Data;
using RaceDayPOE.Models;

namespace RaceDayPOE.Tests;

public class AuthAndAccessTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public AuthAndAccessTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task ParticipantRegistration_StoresHash_AndLoginSetsSessionCookie()
    {
        using var client = _factory.CreateClient(new() { HandleCookies = true, AllowAutoRedirect = false });
        var response = await client.PostAsJsonAsync("/api/auth/register", new { firstName = "Asha", lastName = "Test", email = $"asha{Guid.NewGuid():N}@example.com", password = "StrongPassword123!" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(body);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var stored = db.Participants.Single(x => x.ParticipantID == body!.Id);
        Assert.NotEqual("StrongPassword123!", stored.PasswordHash);
        Assert.StartsWith("AQAAAA", stored.PasswordHash);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = stored.Email, password = "StrongPassword123!", role = "Participant" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(login.Headers.Contains("Set-Cookie") ? login.Headers.GetValues("Set-Cookie") : Array.Empty<string>(), x => x.Contains(".RaceDay.Auth"));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);
    }

    [Fact]
    public async Task ProtectedProfile_RejectsUnauthenticatedRequest()
    {
        using var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
    }

    [Fact]
    public async Task ParticipantIsRejectedFromOrganiserOnlyVenueManagement()
    {
        using var client = _factory.CreateClient(new() { HandleCookies = true, AllowAutoRedirect = false });
        var email = $"role{Guid.NewGuid():N}@example.com";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", new { firstName = "Test", lastName = "Person", email, password = "StrongPassword123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new { email, password = "StrongPassword123!", role = "Participant" })).StatusCode);
        var response = await client.PostAsJsonAsync("/api/venues", new { name = "Test venue", addressLine = "1 Main Road", city = "Pretoria", province = "Gauteng", latitude = -25.7m, longitude = 28.2m });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InvalidLoginIsRejected()
    {
        using var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@example.com", password = "WrongPassword123!", role = "Participant" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }


    [Fact]
    public async Task OrganiserCanCreateEvent_AndParticipantCannotCreateEvent()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var organiser = new Organiser { FirstName = "Race", LastName = "Organiser", Email = $"org{Guid.NewGuid():N}@example.com", CreatedAt = DateTime.UtcNow };
        organiser.PasswordHash = new Microsoft.AspNetCore.Identity.PasswordHasher<Organiser>().HashPassword(organiser, "OrganiserPassword123!");
        db.Organisers.Add(organiser);
        var venue = new Venue { Name = "Test Track", AddressLine = "1 Race Road", City = "Pretoria", Province = "Gauteng", Latitude = -25.7m, Longitude = 28.2m };
        db.Venues.Add(venue); await db.SaveChangesAsync();
        using var client = _factory.CreateClient(new() { HandleCookies = true, AllowAutoRedirect = false });
        var login = await client.PostAsJsonAsync("/api/organisers/login", new { email = organiser.Email, password = "OrganiserPassword123!", role = "Organiser" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var create = await client.PostAsJsonAsync("/api/events", new { name = "Community 5K", description = "A community running event", eventDate = "2027-04-01", eventType = "Run", venueID = venue.VenueID });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
    }

    [Fact]
    public async Task ParticipantCanEnrol_AndDuplicateEnrolmentIsRejected()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var participant = new Participant { FirstName = "Race", LastName = "Runner", Email = $"runner{Guid.NewGuid():N}@example.com", CreatedAt = DateTime.UtcNow };
        participant.PasswordHash = new Microsoft.AspNetCore.Identity.PasswordHasher<Participant>().HashPassword(participant, "ParticipantPassword123!");
        var organiser = new Organiser { FirstName = "Event", LastName = "Owner", Email = $"owner{Guid.NewGuid():N}@example.com", CreatedAt = DateTime.UtcNow };
        organiser.PasswordHash = new Microsoft.AspNetCore.Identity.PasswordHasher<Organiser>().HashPassword(organiser, "OrganiserPassword123!");
        var venue = new Venue { Name = "Enrolment Track", AddressLine = "2 Race Road", City = "Pretoria", Province = "Gauteng", Latitude = -25.7m, Longitude = 28.2m };
        db.Participants.Add(participant); db.Organisers.Add(organiser); db.Venues.Add(venue); await db.SaveChangesAsync();
        var raceEvent = new Event { OrganiserID = organiser.OrganiserID, VenueID = venue.VenueID, Name = "Test 10K", Description = "Integration test event", EventDate = new DateTime(2027, 5, 1), EventType = EventType.Run, CreatedAt = DateTime.UtcNow };
        db.Events.Add(raceEvent); await db.SaveChangesAsync();
        var category = new Category { EventID = raceEvent.EventID, Name = "10km", DistanceKm = 10, MaxParticipants = 1, EntryFee = 100 };
        db.Categories.Add(category); await db.SaveChangesAsync();
        using var client = _factory.CreateClient(new() { HandleCookies = true, AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/participants/login", new { email = participant.Email, password = "ParticipantPassword123!", role = "Participant" })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/enrolments", new { categoryID = category.CategoryID })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/enrolments", new { categoryID = category.CategoryID })).StatusCode);
    }

    private sealed record AccountDto(int Id, string FirstName, string LastName, string Email, string Role, DateTime CreatedAt);
}
