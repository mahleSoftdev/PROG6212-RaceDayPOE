using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RaceDayPOE.Data;
using RaceDayPOE.Models;

namespace RaceDayPOE.Tests;

/// <summary>Shared helpers so each test reads as: set up -> act -> assert.</summary>
internal static class TestSupport
{
    public const string Password = "StrongPassword123!";

    public static HttpClient NewClient(ApiFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });

    public static string UniqueEmail(string prefix) => $"{prefix}{Guid.NewGuid():N}@example.com";

    public static int IntProp(JsonElement element, string name)
    {
        foreach (var p in element.EnumerateObject())
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) return p.Value.GetInt32();
        throw new KeyNotFoundException($"Property '{name}' not found in {element}");
    }

    public static string StringProp(JsonElement element, string name)
    {
        foreach (var p in element.EnumerateObject())
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) return p.Value.GetString() ?? "";
        throw new KeyNotFoundException($"Property '{name}' not found in {element}");
    }

    public static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    /// <summary>Inserts an Organiser directly (organisers cannot self-register) and returns it.</summary>
    public static async Task<Organiser> SeedOrganiserAsync(ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var organiser = new Organiser { FirstName = "Race", LastName = "Organiser", Email = UniqueEmail("org"), CreatedAt = DateTime.UtcNow };
        organiser.PasswordHash = new PasswordHasher<Organiser>().HashPassword(organiser, Password);
        db.Organisers.Add(organiser);
        await db.SaveChangesAsync();
        return organiser;
    }

    public static async Task<HttpClient> NewOrganiserClientAsync(ApiFactory factory)
    {
        var organiser = await SeedOrganiserAsync(factory);
        var client = NewClient(factory);
        var login = await client.PostAsJsonAsync("/api/organisers/login", new { email = organiser.Email, password = Password, role = "Organiser" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return client;
    }

    /// <summary>Registers a new Participant through the public API and logs in.</summary>
    public static async Task<HttpClient> NewParticipantClientAsync(ApiFactory factory)
    {
        var client = NewClient(factory);
        var email = UniqueEmail("runner");
        var register = await client.PostAsJsonAsync("/api/auth/register", new { firstName = "Test", lastName = "Runner", email, password = Password });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password, role = "Participant" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return client;
    }

    public static async Task<int> SeedVenueAsync(ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var venue = new Venue { Name = "Test Track", AddressLine = "1 Race Road", City = "Pretoria", Province = "Gauteng", Latitude = -25.7m, Longitude = 28.2m };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();
        return venue.VenueID;
    }

    public static object EventBody(int venueId, string name = "Community 5K") =>
        new { name, description = "A community running event", eventDate = "2027-04-01", eventType = "Run", venueID = venueId };

    public static async Task<int> CreateEventAsync(HttpClient organiser, int venueId, string name = "Community 5K")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", EventBody(venueId, name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return IntProp(await ReadAsync(response), "eventID");
    }

    public static async Task<int> CreateCategoryAsync(HttpClient organiser, int eventId, int maxParticipants = 100)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { name = "10km Open", distanceKm = 10, maxParticipants, entryFee = 120 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return IntProp(await ReadAsync(response), "categoryID");
    }

    public static async Task<int> EnrolAsync(HttpClient participant, int categoryId)
    {
        var response = await participant.PostAsJsonAsync("/api/enrolments", new { categoryID = categoryId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return IntProp(await ReadAsync(response), "enrolmentID");
    }
}
