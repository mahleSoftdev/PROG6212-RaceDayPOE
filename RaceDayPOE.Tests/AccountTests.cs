using System.Net;
using System.Net.Http.Json;

namespace RaceDayPOE.Tests;

/// <summary>Registration, login, sessions, provisioning and profile (Part 2: authentication and role-based access).</summary>
public class AccountTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public AccountTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = TestSupport.NewClient(_factory);
        var email = TestSupport.UniqueEmail("dup");
        var body = new { firstName = "A", lastName = "B", email, password = TestSupport.Password };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/register", body)).StatusCode);
    }

    [Fact]
    public async Task Register_InvalidBody_ReturnsBadRequest()
    {
        var client = TestSupport.NewClient(_factory);
        var response = await client.PostAsJsonAsync("/api/auth/register", new { firstName = "A", lastName = "B", email = "not-an-email", password = "short" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_CannotCreateOrganiser_EvenIfRoleIsSent()
    {
        var client = TestSupport.NewClient(_factory);
        var email = TestSupport.UniqueEmail("sneaky");
        var register = await client.PostAsJsonAsync("/api/auth/register", new { firstName = "A", lastName = "B", email, password = TestSupport.Password, role = "Organiser" });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        Assert.Equal("Participant", TestSupport.StringProp(await TestSupport.ReadAsync(register), "role"));

        var asOrganiser = await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestSupport.Password, role = "Organiser" });
        Assert.Equal(HttpStatusCode.Unauthorized, asOrganiser.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var client = TestSupport.NewClient(_factory);
        var email = TestSupport.UniqueEmail("wrongpw");
        await client.PostAsJsonAsync("/api/auth/register", new { firstName = "A", lastName = "B", email, password = TestSupport.Password });
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "TotallyWrong123!", role = "Participant" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownRole_ReturnsBadRequest()
    {
        var client = TestSupport.NewClient(_factory);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "x@example.com", password = TestSupport.Password, role = "Admin" });
        Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        var client = await TestSupport.NewParticipantClientAsync(_factory);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
    }

    [Fact]
    public async Task ProvisionOrganiser_WithValidKey_CreatesOrganiserWhoCanLogIn()
    {
        var client = TestSupport.NewClient(_factory);
        var email = TestSupport.UniqueEmail("prov");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/provision-organiser")
        {
            Content = JsonContent.Create(new { firstName = "New", lastName = "Organiser", email, temporaryPassword = "TemporaryPass123!" })
        };
        request.Headers.Add("X-Provisioning-Key", ApiFactory.ProvisioningKey);
        Assert.Equal(HttpStatusCode.Created, (await client.SendAsync(request)).StatusCode);

        var login = await client.PostAsJsonAsync("/api/organisers/login", new { email, password = "TemporaryPass123!", role = "Organiser" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal("Organiser", TestSupport.StringProp(await TestSupport.ReadAsync(login), "role"));
    }

    [Fact]
    public async Task ProvisionOrganiser_WithWrongKey_ReturnsUnauthorized()
    {
        var client = TestSupport.NewClient(_factory);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/provision-organiser")
        {
            Content = JsonContent.Create(new { firstName = "New", lastName = "Organiser", email = TestSupport.UniqueEmail("bad"), temporaryPassword = "TemporaryPass123!" })
        };
        request.Headers.Add("X-Provisioning-Key", "wrong-key");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task Profile_ViewAndUpdateOwn_RoleCannotBeChanged()
    {
        var client = await TestSupport.NewParticipantClientAsync(_factory);
        var email = TestSupport.UniqueEmail("renamed");
        var update = await client.PutAsJsonAsync("/api/users/me", new { firstName = "Renamed", lastName = "Person", email, role = "Organiser" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var me = await TestSupport.ReadAsync(await client.GetAsync("/api/users/me"));
        Assert.Equal("Renamed", TestSupport.StringProp(me, "firstName"));
        Assert.Equal("Participant", TestSupport.StringProp(me, "role"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/organisers/me")).StatusCode);
    }

    [Fact]
    public async Task Profile_UpdateToExistingEmail_ReturnsConflict()
    {
        var first = TestSupport.UniqueEmail("taken");
        var other = TestSupport.NewClient(_factory);
        await other.PostAsJsonAsync("/api/auth/register", new { firstName = "A", lastName = "B", email = first, password = TestSupport.Password });

        var client = await TestSupport.NewParticipantClientAsync(_factory);
        var update = await client.PutAsJsonAsync("/api/users/me", new { firstName = "A", lastName = "B", email = first });
        Assert.Equal(HttpStatusCode.Conflict, update.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsBadRequest()
    {
        var client = await TestSupport.NewParticipantClientAsync(_factory);
        var response = await client.PutAsJsonAsync("/api/users/me/password", new { currentPassword = "WrongPassword123!", newPassword = "AnotherStrongPass123!" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_CorrectCurrentPassword_AllowsLoginWithNewPassword()
    {
        var client = TestSupport.NewClient(_factory);
        var email = TestSupport.UniqueEmail("chg");
        await client.PostAsJsonAsync("/api/auth/register", new { firstName = "A", lastName = "B", email, password = TestSupport.Password });
        await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestSupport.Password, role = "Participant" });

        var change = await client.PutAsJsonAsync("/api/users/me/password", new { currentPassword = TestSupport.Password, newPassword = "BrandNewPassword123!" });
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        var fresh = TestSupport.NewClient(_factory);
        Assert.Equal(HttpStatusCode.Unauthorized, (await fresh.PostAsJsonAsync("/api/auth/login", new { email, password = TestSupport.Password, role = "Participant" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fresh.PostAsJsonAsync("/api/auth/login", new { email, password = "BrandNewPassword123!", role = "Participant" })).StatusCode);
    }
}
