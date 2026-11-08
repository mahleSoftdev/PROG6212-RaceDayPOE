using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayPOE.Data;
using RaceDayPOE.DTOs;
using RaceDayPOE.Models;
using RaceDayPOE.Services;

namespace RaceDayPOE.Controllers;

[ApiController, Route("api/users"), Authorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher<Participant> _participantHasher;
    private readonly IPasswordHasher<Organiser> _organiserHasher;
    private readonly IImageStorage _images;
    public UsersController(RaceDayDbContext db, IPasswordHasher<Participant> ph, IPasswordHasher<Organiser> oh, IImageStorage images) => (_db, _participantHasher, _organiserHasher, _images) = (db, ph, oh, images);

    //View the authenticated account's own profile.
    [HttpGet("me")]
    public async Task<ActionResult<AccountResponse>> GetMine()
    {
        var id = UserId();
        if (User.IsInRole(nameof(UserRole.Participant)))
        {
            var p = await _db.Participants.FindAsync(id);
            return p is null ? NotFound() : Ok(new AccountResponse(p.ParticipantID, p.FirstName, p.LastName, p.Email, nameof(UserRole.Participant), p.CreatedAt, p.ProfilePictureUrl));
        }
        var o = await _db.Organisers.FindAsync(id);
        return o is null ? NotFound() : Ok(new AccountResponse(o.OrganiserID, o.FirstName, o.LastName, o.Email, nameof(UserRole.Organiser), o.CreatedAt));
    }

    //Update the authenticated account's own profile. Role and password cannot be changed here.
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
            return Ok(new AccountResponse(p.ParticipantID, p.FirstName, p.LastName, p.Email, nameof(UserRole.Participant), p.CreatedAt, p.ProfilePictureUrl));
        }
        var o = await _db.Organisers.FindAsync(id); if (o is null) return NotFound();
        o.FirstName = request.FirstName.Trim(); o.LastName = request.LastName.Trim(); o.Email = email; await _db.SaveChangesAsync();
        return Ok(new AccountResponse(o.OrganiserID, o.FirstName, o.LastName, o.Email, nameof(UserRole.Organiser), o.CreatedAt));
    }
    //Role-specific Organiser profile route retained for the documented API plan.
    [Authorize(Policy = "OrganiserOnly"), HttpGet("/api/organisers/me")]
    public Task<ActionResult<AccountResponse>> GetOrganiserProfile() => GetMine();

    //Role-specific Participant profile route retained for the documented API plan.
    [Authorize(Policy = "ParticipantOnly"), HttpGet("/api/participants/me")]
    public Task<ActionResult<AccountResponse>> GetParticipantProfile() => GetMine();

    //Update the authenticated Organiser's own profile.
    [Authorize(Policy = "OrganiserOnly"), HttpPut("/api/organisers/me")]
    public Task<ActionResult<AccountResponse>> UpdateOrganiserProfile(UpdateProfileRequest request) => UpdateMine(request);

    //Update the authenticated Participant's own profile.
    [Authorize(Policy = "ParticipantOnly"), HttpPut("/api/participants/me")]
    public Task<ActionResult<AccountResponse>> UpdateParticipantProfile(UpdateProfileRequest request) => UpdateMine(request);

    //Upload or replace the authenticated Participant's profile picture (stored in Azure Blob Storage).
    [Authorize(Policy = "ParticipantOnly"), HttpPost("me/picture"), Consumes("multipart/form-data")]
    [RequestSizeLimit(ImageValidator.MaxBytes + 1024 * 1024)]
    public async Task<IActionResult> UploadPicture(IFormFile file, CancellationToken cancellationToken)
    {
        var participant = await _db.Participants.FindAsync(new object[] { UserId() }, cancellationToken); if (participant is null) return NotFound();
        var image = await ImageValidator.InspectAsync(file, cancellationToken);
        if (!image.IsValid) return BadRequest(new { message = image.Error });
        string url;
        try
        {
            await using var stream = file.OpenReadStream();
            url = await _images.UploadAsync("profiles", stream, image.ContentType!, image.Extension!, cancellationToken);
        }
        catch (ImageStorageUnavailableException ex) { return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message }); }
        var previous = participant.ProfilePictureUrl;
        participant.ProfilePictureUrl = url; await _db.SaveChangesAsync(cancellationToken);
        await _images.DeleteAsync(previous, cancellationToken);
        return Ok(new AccountResponse(participant.ParticipantID, participant.FirstName, participant.LastName, participant.Email, nameof(UserRole.Participant), participant.CreatedAt, participant.ProfilePictureUrl));
    }

    //Change the authenticated user's password after verifying the current password.
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
