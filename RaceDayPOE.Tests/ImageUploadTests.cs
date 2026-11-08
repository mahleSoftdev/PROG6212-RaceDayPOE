using System.Net;
using System.Net.Http.Json;

namespace RaceDayPOE.Tests;

//Event banner and profile picture uploads go through the API and are stored in (fake) blob storage.
public class ImageUploadTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public ImageUploadTests(ApiFactory factory) => _factory = factory;

    private async Task<(HttpClient Organiser, int EventId)> OrganiserWithEventAsync()
    {
        var venueId = await TestSupport.SeedVenueAsync(_factory);
        var organiser = await TestSupport.NewOrganiserClientAsync(_factory);
        return (organiser, await TestSupport.CreateEventAsync(organiser, venueId));
    }

    [Fact]
    public async Task UploadBanner_AsOwner_SavesUrlAndShowsItOnTheEvent()
    {
        var (organiser, eventId) = await OrganiserWithEventAsync();

        var response = await organiser.PostAsync($"/api/events/{eventId}/banner", ImageFixtures.Upload(ImageFixtures.Png()));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var url = TestSupport.StringProp(await TestSupport.ReadAsync(response), "bannerUrl");
        Assert.StartsWith("https://fake.blob.local/raceday-images/events/", url);
        Assert.EndsWith(".png", url);

        var detail = await TestSupport.NewClient(_factory).GetAsync($"/api/events/{eventId}");
        Assert.Equal(url, TestSupport.StringProp(await TestSupport.ReadAsync(detail), "bannerUrl"));
    }

    [Fact]
    public async Task UploadBanner_Replacing_DeletesThePreviousImage()
    {
        var (organiser, eventId) = await OrganiserWithEventAsync();
        var first = TestSupport.StringProp(await TestSupport.ReadAsync(await organiser.PostAsync($"/api/events/{eventId}/banner", ImageFixtures.Upload(ImageFixtures.Png()))), "bannerUrl");
        var second = await organiser.PostAsync($"/api/events/{eventId}/banner", ImageFixtures.Upload(ImageFixtures.Jpeg(), "b.jpg", "image/jpeg"));
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Contains(first, _factory.Images.Deleted);
    }

    [Fact]
    public async Task UploadBanner_FileThatIsNotAnImage_ReturnsBadRequest()
    {
        var (organiser, eventId) = await OrganiserWithEventAsync();
        var response = await organiser.PostAsync($"/api/events/{eventId}/banner", ImageFixtures.Upload(ImageFixtures.NotAnImage(), "evil.png", "image/png"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_ByAnotherOrganiser_ReturnsForbidden()
    {
        var (_, eventId) = await OrganiserWithEventAsync();
        var other = await TestSupport.NewOrganiserClientAsync(_factory);
        var response = await other.PostAsync($"/api/events/{eventId}/banner", ImageFixtures.Upload(ImageFixtures.Png()));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_AsParticipantOrAnonymous_IsRejected()
    {
        var (_, eventId) = await OrganiserWithEventAsync();
        var participant = await TestSupport.NewParticipantClientAsync(_factory);
        Assert.Equal(HttpStatusCode.Forbidden, (await participant.PostAsync($"/api/events/{eventId}/banner", ImageFixtures.Upload(ImageFixtures.Png()))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await TestSupport.NewClient(_factory).PostAsync($"/api/events/{eventId}/banner", ImageFixtures.Upload(ImageFixtures.Png()))).StatusCode);
    }

    [Fact]
    public async Task UploadBanner_UnknownEvent_ReturnsNotFound()
    {
        var organiser = await TestSupport.NewOrganiserClientAsync(_factory);
        var response = await organiser.PostAsync("/api/events/999999/banner", ImageFixtures.Upload(ImageFixtures.Png()));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WhenStorageIsUnavailable_ReturnsServiceUnavailable()
    {
        using var factory = new ApiFactory();
        factory.Images.Unavailable = true;
        var venueId = await TestSupport.SeedVenueAsync(factory);
        var organiser = await TestSupport.NewOrganiserClientAsync(factory);
        var eventId = await TestSupport.CreateEventAsync(organiser, venueId);
        var response = await organiser.PostAsync($"/api/events/{eventId}/banner", ImageFixtures.Upload(ImageFixtures.Png()));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task UploadProfilePicture_AsParticipant_AppearsOnTheProfile()
    {
        var participant = await TestSupport.NewParticipantClientAsync(_factory);
        var response = await participant.PostAsync("/api/users/me/picture", ImageFixtures.Upload(ImageFixtures.Jpeg(), "me.jpg", "image/jpeg"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var url = TestSupport.StringProp(await TestSupport.ReadAsync(response), "profilePictureUrl");
        Assert.StartsWith("https://fake.blob.local/raceday-images/profiles/", url);

        var profile = await TestSupport.ReadAsync(await participant.GetAsync("/api/users/me"));
        Assert.Equal(url, TestSupport.StringProp(profile, "profilePictureUrl"));
    }

    [Fact]
    public async Task UploadProfilePicture_AsOrganiser_ReturnsForbidden()
    {
        var organiser = await TestSupport.NewOrganiserClientAsync(_factory);
        var response = await organiser.PostAsync("/api/users/me/picture", ImageFixtures.Upload(ImageFixtures.Png()));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadProfilePicture_NotAnImage_ReturnsBadRequest()
    {
        var participant = await TestSupport.NewParticipantClientAsync(_factory);
        var response = await participant.PostAsync("/api/users/me/picture", ImageFixtures.Upload(ImageFixtures.NotAnImage()));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
