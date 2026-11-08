using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayPOE.Data;
using RaceDayPOE.DTOs;
using RaceDayPOE.Models;

namespace RaceDayPOE.Controllers;

[ApiController, Route("api/results"), Authorize]
public class ResultsController(RaceDayDbContext db) : ControllerBase
{
    //Participants view their own results. Organisers view results for events they own.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var q = db.Results.AsNoTracking().Include(r => r.Enrolment).ThenInclude(e => e!.Participant).Include(r => r.Enrolment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event).AsQueryable();
        if (User.IsInRole(nameof(UserRole.Participant))) q = q.Where(r => r.Enrolment!.ParticipantID == AccountId());
        else q = q.Where(r => r.Enrolment!.Category!.Event!.OrganiserID == AccountId());
        return Ok(await q.Select(r => new { r.ResultID, r.EnrolmentID, r.FinishTime, r.Position, r.RecordedAt, ParticipantName = r.Enrolment!.Participant!.FirstName + " " + r.Enrolment.Participant.LastName, EventName = r.Enrolment.Category!.Event!.Name }).ToListAsync());
    }
    //Retrieve one result if the caller owns the participant account or the event.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOne(int id)
    {
        var r = await db.Results.AsNoTracking().Include(x => x.Enrolment).ThenInclude(e => e!.Participant).Include(x => x.Enrolment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event).FirstOrDefaultAsync(x => x.ResultID == id);
        if (r is null) return NotFound();
        var participantOwner = User.IsInRole(nameof(UserRole.Participant)) && r.Enrolment!.ParticipantID == AccountId();
        var organiserOwner = User.IsInRole(nameof(UserRole.Organiser)) && r.Enrolment!.Category!.Event!.OrganiserID == AccountId();
        if (!participantOwner && !organiserOwner) return Forbid();
        return Ok(new { r.ResultID, r.EnrolmentID, r.FinishTime, r.Position, r.RecordedAt, EventName = r.Enrolment!.Category!.Event!.Name });
    }

    //Get results for an event owned by the authenticated Organiser.
    [Authorize(Policy = "OrganiserOnly"), HttpGet("/api/events/{eventId:int}/results")]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await db.Events.FindAsync(eventId); if (ev is null) return NotFound(); if (ev.OrganiserID != AccountId()) return Forbid();
        return Ok(await db.Results.AsNoTracking().Include(r => r.Enrolment).ThenInclude(e => e!.Participant)
            .Where(r => r.Enrolment!.Category!.EventID == eventId)
            .Select(r => new { r.ResultID, r.EnrolmentID, r.FinishTime, r.Position, r.RecordedAt, ParticipantName = r.Enrolment!.Participant!.FirstName + " " + r.Enrolment.Participant.LastName }).ToListAsync());
    }

    //Get all results belonging to the authenticated Participant.
    [Authorize(Policy = "ParticipantOnly"), HttpGet("/api/participants/me/results")]
    public async Task<IActionResult> GetMyResults() => Ok(await db.Results.AsNoTracking().Include(r => r.Enrolment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event)
        .Where(r => r.Enrolment!.ParticipantID == AccountId())
        .OrderByDescending(r => r.Enrolment!.Category!.Event!.EventDate)
        .Select(r => new
        {
            r.ResultID, r.EnrolmentID, r.FinishTime, r.Position, r.RecordedAt,
            EventName = r.Enrolment!.Category!.Event!.Name,
            EventDate = r.Enrolment.Category.Event.EventDate,
            CategoryName = r.Enrolment.Category.Name,
            //Finishers are counted per category, because each category is ranked separately.
            TotalFinishers = db.Results.Count(x => x.Enrolment!.CategoryID == r.Enrolment.CategoryID)
        }).ToListAsync());

    //Get the result linked to an enrolment if the caller owns the participant account or event.
    [HttpGet("/api/enrolments/{enrolmentId:int}/results")]
    public async Task<IActionResult> GetForEnrolment(int enrolmentId)
    {
        var e = await db.Enrolments.Include(x => x.Category).ThenInclude(c => c!.Event).FirstOrDefaultAsync(x => x.EnrolmentID == enrolmentId);
        if (e is null) return NotFound();
        var participantOwner = User.IsInRole(nameof(UserRole.Participant)) && e.ParticipantID == AccountId();
        var organiserOwner = User.IsInRole(nameof(UserRole.Organiser)) && e.Category?.Event?.OrganiserID == AccountId();
        if (!participantOwner && !organiserOwner) return Forbid();
        var result = await db.Results.AsNoTracking().FirstOrDefaultAsync(r => r.EnrolmentID == enrolmentId);
        return result is null ? NotFound() : Ok(result);
    }

    //Record a result for an enrolment in an event owned by the authenticated Organiser.
    [Authorize(Policy = "OrganiserOnly"), HttpPost]
    public async Task<IActionResult> Create(ResultRequest request)
    {
        if (request.FinishTime < TimeSpan.Zero || request.FinishTime >= TimeSpan.FromDays(1)) return BadRequest(new { message = "FinishTime must be a valid duration below 24 hours." });
        var enrolment = await db.Enrolments.Include(e => e.Category).ThenInclude(c => c!.Event).FirstOrDefaultAsync(e => e.EnrolmentID == request.EnrolmentID);
        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.Category?.Event?.OrganiserID != AccountId()) return Forbid();
        if (await db.Results.AnyAsync(r => r.EnrolmentID == request.EnrolmentID)) return Conflict(new { message = "A result already exists for this enrolment." });
        var result = new Result { EnrolmentID = request.EnrolmentID, FinishTime = request.FinishTime, Position = request.Position, RecordedAt = DateTime.UtcNow };
        db.Results.Add(result); await db.SaveChangesAsync(); return StatusCode(StatusCodes.Status201Created, new { result.ResultID, result.EnrolmentID, result.FinishTime, result.Position, result.RecordedAt });
    }
    //Update a result for an event owned by the authenticated Organiser.
    [Authorize(Policy = "OrganiserOnly"), HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ResultRequest request)
    {
        var result = await db.Results.Include(r => r.Enrolment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event).FirstOrDefaultAsync(r => r.ResultID == id);
        if (result is null) return NotFound(); if (result.Enrolment?.Category?.Event?.OrganiserID != AccountId()) return Forbid();
        if (result.EnrolmentID != request.EnrolmentID || request.FinishTime < TimeSpan.Zero || request.FinishTime >= TimeSpan.FromDays(1)) return BadRequest(new { message = "EnrolmentID must match the existing result and FinishTime must be valid." });
        result.FinishTime = request.FinishTime; result.Position = request.Position; result.RecordedAt = DateTime.UtcNow; await db.SaveChangesAsync(); return NoContent();
    }
    //Delete a result belonging to an event owned by the authenticated Organiser.
    [Authorize(Policy = "OrganiserOnly"), HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await db.Results.Include(r => r.Enrolment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event).FirstOrDefaultAsync(r => r.ResultID == id);
        if (result is null) return NotFound(); if (result.Enrolment?.Category?.Event?.OrganiserID != AccountId()) return Forbid();
        db.Results.Remove(result); await db.SaveChangesAsync(); return NoContent();
    }
    private int AccountId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
