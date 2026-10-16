using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayPOE.Data;
using RaceDayPOE.DTOs;
using RaceDayPOE.Models;

namespace RaceDayPOE.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher<Participant> _participantHasher;
    private readonly IPasswordHasher<Organiser> _organiserHasher;
    private readonly IConfiguration _configuration;
    public AuthController(RaceDayDbContext db, IPasswordHasher<Participant> participantHasher, IPasswordHasher<Organiser> organiserHasher, IConfiguration configuration)
        => (_db, _participantHasher, _organiserHasher, _configuration) = (db, participantHasher, organiserHasher, configuration);

    //Register a Participant account. Organiser self-registration is intentionally unavailable.
    [AllowAnonymous, HttpPost("register")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AccountResponse>> Register(RegisterParticipantRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await EmailExists(email)) return Conflict(new { message = "An account with this email already exists." });
        var participant = new Participant { FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), Email = email, CreatedAt = DateTime.UtcNow };
        participant.PasswordHash = _participantHasher.HashPassword(participant, request.Password);
        _db.Participants.Add(participant);
        await _db.SaveChangesAsync();
        var response = ToResponse(participant);
        return Created("/api/users/me", response);
    }

    //Login as a registered Participant or provisioned Organiser. Role is required in the body.
    [AllowAnonymous, HttpPost("login")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AccountResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        int id; string firstName; string lastName; string role; DateTime created;
        if (request.Role == UserRole.Participant)
        {
            var user = await _db.Participants.SingleOrDefaultAsync(x => x.Email == email);
            if (user is null || _participantHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
                return Unauthorized(new { message = "Invalid email, password, or role." });
            id = user.ParticipantID; firstName = user.FirstName; lastName = user.LastName; role = nameof(UserRole.Participant); created = user.CreatedAt;
        }
        else if (request.Role == UserRole.Organiser)
        {
            var user = await _db.Organisers.SingleOrDefaultAsync(x => x.Email == email);
            if (user is null || _organiserHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
                return Unauthorized(new { message = "Invalid email, password, or role." });
            id = user.OrganiserID; firstName = user.FirstName; lastName = user.LastName; role = nameof(UserRole.Organiser); created = user.CreatedAt;
        }
        else return BadRequest(new { message = "Role must be Organiser or Participant." });

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Name, email), new Claim(ClaimTypes.Role, role) };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = true, AllowRefresh = true, ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) });
        HttpContext.Session.SetString("AccountRole", role);
        HttpContext.Session.SetString("AccountId", id.ToString());
        return Ok(new AccountResponse(id, firstName, lastName, email, role, created));
    }

    //Role-specific Participant login route. The request body must specify Participant as its role.
    [AllowAnonymous, HttpPost("/api/participants/login")]
    public Task<ActionResult<AccountResponse>> ParticipantLogin(LoginRequest request)
    {
        if (request.Role != UserRole.Participant) return Task.FromResult<ActionResult<AccountResponse>>(BadRequest(new { message = "Role must be Participant for this endpoint." }));
        return Login(request);
    }

    //Role-specific Organiser login route for accounts provisioned by trusted operators.
    [AllowAnonymous, HttpPost("/api/organisers/login")]
    public Task<ActionResult<AccountResponse>> OrganiserLogin(LoginRequest request)
    {
        if (request.Role != UserRole.Organiser) return Task.FromResult<ActionResult<AccountResponse>>(BadRequest(new { message = "Role must be Organiser for this endpoint." }));
        return Login(request);
    }

    //Provision an Organiser account using the deployment's secret key. This is not public self-registration.
    [AllowAnonymous, HttpPost("provision-organiser")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AccountResponse>> ProvisionOrganiser(ProvisionOrganiserRequest request, [FromHeader(Name = "X-Provisioning-Key"), System.ComponentModel.DataAnnotations.Required] string provisioningKey)
    {
        var expected = _configuration["Security:OrganiserProvisioningKey"];
        if (string.IsNullOrWhiteSpace(expected) || !CryptographicEquals(expected, provisioningKey)) return Unauthorized(new { message = "Invalid provisioning credentials." });
        var email = request.Email.Trim().ToLowerInvariant();
        if (await EmailExists(email)) return Conflict(new { message = "An account with this email already exists." });
        var organiser = new Organiser { FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), Email = email, CreatedAt = DateTime.UtcNow };
        organiser.PasswordHash = _organiserHasher.HashPassword(organiser, request.TemporaryPassword);
        _db.Organisers.Add(organiser);
        await _db.SaveChangesAsync();
        return Created("/api/auth/login", ToResponse(organiser));
    }

    //End the authenticated session.
    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    private async Task<bool> EmailExists(string email) =>
        await _db.Participants.AnyAsync(x => x.Email == email) || await _db.Organisers.AnyAsync(x => x.Email == email);
    private static AccountResponse ToResponse(Participant x) => new(x.ParticipantID, x.FirstName, x.LastName, x.Email, nameof(UserRole.Participant), x.CreatedAt);
    private static AccountResponse ToResponse(Organiser x) => new(x.OrganiserID, x.FirstName, x.LastName, x.Email, nameof(UserRole.Organiser), x.CreatedAt);
    private static bool CryptographicEquals(string a, string b)
    {
        var x = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(a));
        var y = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(b));
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(x, y);
    }
}
