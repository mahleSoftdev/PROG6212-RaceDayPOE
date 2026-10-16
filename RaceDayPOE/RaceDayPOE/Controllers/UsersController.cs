using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayPOE.Data;
using RaceDayPOE.DTOs;
using RaceDayPOE.Models;

namespace RaceDayPOE.Controllers;

[ApiController, Route("api/users"), Authorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher<Participant> _participantHasher;
    private readonly IPasswordHasher<Organiser> _organiserHasher;
    public UsersController(RaceDayDbContext db, IPasswordHasher<Participant> ph, IPasswordHasher<Organiser> oh) => (_db, _participantHasher, _organiserHasher) = (db, ph, oh);

    /// <summary>View the authenticated account's own profile.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<AccountResponse>> GetMine()
    {
        var id = UserId();
        if (User.IsInRole(nameof(UserRole.Participant)))
        {
            var p = await _db.Participants.FindAsync(id);
            return p is null ? NotFound() : Ok(new AccountResponse(p.ParticipantID, p.FirstName, p.LastName, p.Email, nameof(UserRole.Participant), p.CreatedAt));
        }
        var o = await _db.Organisers.FindAsync(id);
        return o is null ? NotFound() : Ok(new AccountResponse(o.OrganiserID, o.FirstName, o.LastName, o.Email, nameof(UserRole.Organiser), o.CreatedAt));
    }

    /// <summary>Update the authenticated account's own profile. Role and password cannot be changed here.</summary>
    [HttpPut("me")]
    public async Task<ActionResult<AccountResponse>> UpdateMine(UpdateProfileRequest request)
    {
        var id = UserId(); var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Participants.AnyAsync(x => x.Email == email && !(User.IsInRole(nameof(UserRole.Participant)) && x.ParticipantID == id)) ||
            await _db.Organisers.AnyAsync(x => x.Email == email && !(User.IsInRole(nameof(UserRole.Organiser)) && x.OrganiserID == id)))
            return Conflict(new { message = "An account with this email already exists." });
        if (User.IsInRole(nameof(UserRole.Participant)))
        {
            var p = await _db.Participants.FindAsync(id); if (p is null) return NotFound();
            p.FirstName = request.FirstName.Trim(); p.LastName = request.LastName.Trim(); p.Email = email; await _db.SaveChangesAsync();
            return Ok(new AccountResponse(p.ParticipantID, p.FirstName, p.LastName, p.Email, nameof(UserRole.Participant), p.CreatedAt));
        }
        var o = await _db.Organisers.FindAsync(id); if (o is null) return NotFound();
        o.FirstName = request.FirstName.Trim(); o.LastName = request.LastName.Trim(); o.Email = email; await _db.SaveChangesAsync();
        return Ok(new AccountResponse(o.OrganiserID, o.FirstName, o.LastName, o.Email, nameof(UserRole.Organiser), o.CreatedAt));
    }
    /// <summary>Role-specific Organiser profile route retained for the documented API plan.</summary>
    [Authorize(Policy = "OrganiserOnly"), HttpGet("/api/organisers/me")]
    public Task<ActionResult<AccountResponse>> GetOrganiserProfile() => GetMine();

    /// <summary>Role-specific Participant profile route retained for the documented API plan.</summary>
    [Authorize(Policy = "ParticipantOnly"), HttpGet("/api/participants/me")]
    public Task<ActionResult<AccountResponse>> GetParticipantProfile() => GetMine();

    /// <summary>Update the authenticated Organiser's own profile.</summary>
    [Authorize(Policy = "OrganiserOnly"), HttpPut("/api/organisers/me")]
    public Task<ActionResult<AccountResponse>> UpdateOrganiserProfile(UpdateProfileRequest request) => UpdateMine(request);

    /// <summary>Update the authenticated Participant's own profile.</summary>
    [Authorize(Policy = "ParticipantOnly"), HttpPut("/api/participants/me")]
    public Task<ActionResult<AccountResponse>> UpdateParticipantProfile(UpdateProfileRequest request) => UpdateMine(request);

    /// <summary>Change the authenticated user's password after verifying the current password.</summary>
    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var id = UserId();
        if (User.IsInRole(nameof(UserRole.Participant)))
        {
            var participant = await _db.Participants.FindAsync(id); if (participant is null) return NotFound();
            if (_participantHasher.VerifyHashedPassword(participant, participant.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
                return BadRequest(new { message = "Current password is incorrect." });
            participant.PasswordHash = _participantHasher.HashPassword(participant, request.NewPassword);
        }
        else
        {
            var organiser = await _db.Organisers.FindAsync(id); if (organiser is null) return NotFound();
            if (_organiserHasher.VerifyHashedPassword(organiser, organiser.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
                return BadRequest(new { message = "Current password is incorrect." });
            organiser.PasswordHash = _organiserHasher.HashPassword(organiser, request.NewPassword);
        }
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private int UserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
