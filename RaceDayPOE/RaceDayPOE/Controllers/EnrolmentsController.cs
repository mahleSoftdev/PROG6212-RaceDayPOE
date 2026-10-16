using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayPOE.Data;
using RaceDayPOE.DTOs;
using RaceDayPOE.Models;

namespace RaceDayPOE.Controllers;

[ApiController, Route("api/enrolments"), Authorize]
public class EnrolmentsController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>Participants view their own enrolments. Organisers view enrolments for events they own.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var q = db.Enrolments.AsNoTracking().Include(x => x.Participant).Include(x => x.Category).ThenInclude(c => c!.Event).AsQueryable();
        if (User.IsInRole(nameof(UserRole.Participant))) q = q.Where(x => x.ParticipantID == AccountId());
        else q = q.Where(x => x.Category != null && x.Category.Event != null && x.Category.Event.OrganiserID == AccountId());
        return Ok(await q.Select(x => new { x.EnrolmentID, x.ParticipantID, ParticipantName = x.Participant!.FirstName + " " + x.Participant.LastName, x.CategoryID, CategoryName = x.Category!.Name, EventName = x.Category.Event!.Name, x.EnrolmentDate, x.Status }).ToListAsync());
    }
    /// <summary>List enrolments for an event owned by the authenticated Organiser.</summary>
    [Authorize(Policy = "OrganiserOnly"), HttpGet("/api/events/{eventId:int}/enrolments")]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await db.Events.FindAsync(eventId); if (ev is null) return NotFound(); if (ev.OrganiserID != AccountId()) return Forbid();
        return Ok(await db.Enrolments.AsNoTracking().Include(x => x.Participant).Include(x => x.Category)
            .Where(x => x.Category!.EventID == eventId)
            .Select(x => new { x.EnrolmentID, x.ParticipantID, ParticipantName = x.Participant!.FirstName + " " + x.Participant.LastName, x.CategoryID, CategoryName = x.Category!.Name, x.EnrolmentDate, x.Status }).ToListAsync());
    }

    /// <summary>Retrieve an enrolment if the caller owns it or owns its event.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOne(int id)
    {
        var e = await db.Enrolments.Include(x => x.Participant).Include(x => x.Category).ThenInclude(c => c!.Event).FirstOrDefaultAsync(x => x.EnrolmentID == id);
        if (e is null) return NotFound();
        var allowed = User.IsInRole(nameof(UserRole.Participant)) ? e.ParticipantID == AccountId() : e.Category?.Event?.OrganiserID == AccountId();
        if (!allowed) return Forbid();
        return Ok(new { e.EnrolmentID, e.ParticipantID, ParticipantName = e.Participant!.FirstName + " " + e.Participant.LastName, e.CategoryID, CategoryName = e.Category!.Name, EventName = e.Category.Event!.Name, e.EnrolmentDate, e.Status });
    }

    /// <summary>Enrol in a category through the event-specific endpoint.</summary>
    [Authorize(Policy = "ParticipantOnly"), HttpPost("/api/events/{eventId:int}/enrolments")]
    public async Task<IActionResult> CreateForEvent(int eventId, EnrolmentRequest request)
    {
        var category = await db.Categories.FirstOrDefaultAsync(x => x.CategoryID == request.CategoryID && x.EventID == eventId);
        if (category is null) return BadRequest(new { message = "The selected category does not belong to this event." });
        return await Create(request);
    }

    /// <summary>Update enrolment status for an event owned by the authenticated Organiser.</summary>
    [Authorize(Policy = "OrganiserOnly"), HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateStatus(int id, EnrolmentStatusRequest request)
    {
        var e = await db.Enrolments.Include(x => x.Category).ThenInclude(c => c!.Event).FirstOrDefaultAsync(x => x.EnrolmentID == id);
        if (e is null) return NotFound(); if (e.Category?.Event?.OrganiserID != AccountId()) return Forbid();
        if (!Enum.IsDefined(request.Status)) return BadRequest(new { message = "Status must be Pending, Confirmed, or Cancelled." });
        e.Status = request.Status; await db.SaveChangesAsync(); return NoContent();
    }

    /// <summary>Cancel an enrolment as its Participant or as the event's Organiser.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteOrCancel(int id)
    {
        var e = await db.Enrolments.Include(x => x.Category).ThenInclude(c => c!.Event).FirstOrDefaultAsync(x => x.EnrolmentID == id);
        if (e is null) return NotFound();
        var participantOwner = User.IsInRole(nameof(UserRole.Participant)) && e.ParticipantID == AccountId();
        var organiserOwner = User.IsInRole(nameof(UserRole.Organiser)) && e.Category?.Event?.OrganiserID == AccountId();
        if (!participantOwner && !organiserOwner) return Forbid();
        e.Status = EnrolmentStatus.Cancelled; await db.SaveChangesAsync(); return NoContent();
    }

    /// <summary>Enrol the authenticated Participant in a category.</summary>
    [Authorize(Policy = "ParticipantOnly"), HttpPost]
    public async Task<IActionResult> Create(EnrolmentRequest request)
    {
        var category = await db.Categories.FindAsync(request.CategoryID); if (category is null) return NotFound(new { message = "Category not found." });
        var participantId = AccountId();
        if (await db.Enrolments.AnyAsync(x => x.ParticipantID == participantId && x.CategoryID == request.CategoryID && x.Status != EnrolmentStatus.Cancelled)) return Conflict(new { message = "You are already enrolled in this category." });
        var active = await db.Enrolments.CountAsync(x => x.CategoryID == request.CategoryID && x.Status != EnrolmentStatus.Cancelled);
        if (active >= category.MaxParticipants) return Conflict(new { message = "This category has reached capacity." });
        var enrolment = new Enrolment { ParticipantID = participantId, CategoryID = request.CategoryID, EnrolmentDate = DateTime.UtcNow, Status = EnrolmentStatus.Confirmed };
        db.Enrolments.Add(enrolment); await db.SaveChangesAsync(); return StatusCode(StatusCodes.Status201Created, new { enrolment.EnrolmentID, enrolment.ParticipantID, enrolment.CategoryID, enrolment.EnrolmentDate, enrolment.Status });
    }
    /// <summary>Cancel an enrolment owned by the authenticated Participant.</summary>
    [Authorize(Policy = "ParticipantOnly"), HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var e = await db.Enrolments.FindAsync(id); if (e is null) return NotFound(); if (e.ParticipantID != AccountId()) return Forbid();
        if (e.Status == EnrolmentStatus.Cancelled) return Conflict(new { message = "Enrolment is already cancelled." });
        e.Status = EnrolmentStatus.Cancelled; await db.SaveChangesAsync(); return NoContent();
    }
    private int AccountId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
