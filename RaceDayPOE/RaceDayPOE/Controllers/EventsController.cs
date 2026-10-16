using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayPOE.Data;
using RaceDayPOE.DTOs;
using RaceDayPOE.Models;

namespace RaceDayPOE.Controllers;

[ApiController, Route("api/events")]
public class EventsController(RaceDayDbContext db) : ControllerBase
{
    //Browse events. Public access supports event discovery.
    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await db.Events.AsNoTracking().Include(e => e.Venue).Include(e => e.Categories).Select(e => new { e.EventID, e.Name, e.Description, e.EventDate, e.EventType, e.VenueID, Venue = e.Venue == null ? null : e.Venue.Name, Categories = e.Categories.Select(c => new { c.CategoryID, c.Name, c.DistanceKm, c.MaxParticipants, c.EntryFee }) }).ToListAsync());
    //Get an event and its categories.
    [AllowAnonymous, HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var e = await db.Events.AsNoTracking().Include(x => x.Venue).Include(x => x.Categories).FirstOrDefaultAsync(x => x.EventID == id);
        return e is null ? NotFound() : Ok(new { e.EventID, e.Name, e.Description, e.EventDate, e.EventType, e.VenueID, Venue = e.Venue?.Name, e.Categories });
    }
    //Create an event owned by the authenticated Organiser.
    [Authorize(Policy = "OrganiserOnly"), HttpPost]
    public async Task<IActionResult> Create(EventRequest request)
    {
        if (!Enum.IsDefined(request.EventType)) return BadRequest(new { message = "EventType must be Run, Walk, or Cycle." });
        if (!await db.Venues.AnyAsync(v => v.VenueID == request.VenueID)) return BadRequest(new { message = "Venue does not exist." });
        var e = new Event { OrganiserID = AccountId(), VenueID = request.VenueID, Name = request.Name.Trim(), Description = request.Description.Trim(), EventDate = request.EventDate.Date, EventType = request.EventType, CreatedAt = DateTime.UtcNow };
        db.Events.Add(e); await db.SaveChangesAsync(); return CreatedAtAction(nameof(Get), new { id = e.EventID }, e);
    }
    //Update an event owned by the authenticated Organiser.
    [Authorize(Policy = "OrganiserOnly"), HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, EventRequest request)
    {
        var e = await db.Events.FindAsync(id); if (e is null) return NotFound(); if (e.OrganiserID != AccountId()) return Forbid();
        if (!Enum.IsDefined(request.EventType) || !await db.Venues.AnyAsync(v => v.VenueID == request.VenueID)) return BadRequest(new { message = "A valid event type and venue are required." });
        e.Name = request.Name.Trim(); e.Description = request.Description.Trim(); e.EventDate = request.EventDate.Date; e.EventType = request.EventType; e.VenueID = request.VenueID;
        await db.SaveChangesAsync(); return NoContent();
    }
    //Delete an event owned by the authenticated Organiser when no dependent records exist.
    [Authorize(Policy = "OrganiserOnly"), HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await db.Events.FindAsync(id); if (e is null) return NotFound(); if (e.OrganiserID != AccountId()) return Forbid();
        if (await db.Categories.AnyAsync(c => c.EventID == id)) return Conflict(new { message = "Remove dependent categories and enrolments before deleting this event." });
        db.Events.Remove(e); await db.SaveChangesAsync(); return NoContent();
    }
    private int AccountId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
