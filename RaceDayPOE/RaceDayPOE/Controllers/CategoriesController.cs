using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayPOE.Data;
using RaceDayPOE.DTOs;
using RaceDayPOE.Models;

namespace RaceDayPOE.Controllers;

[ApiController, Route("api/categories")]
public class CategoriesController(RaceDayDbContext db) : ControllerBase
{
    //List categories, optionally filtered by event.
    [AllowAnonymous, HttpGet, HttpGet("/api/events/{eventId:int}/categories")]
    public async Task<IActionResult> GetAll(int? eventId) => Ok(await db.Categories.AsNoTracking().Where(c => eventId == null || c.EventID == eventId).ToListAsync());
    //Get a category by ID.
    [AllowAnonymous, HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) { var c = await db.Categories.AsNoTracking().FirstOrDefaultAsync(x => x.CategoryID == id); return c is null ? NotFound() : Ok(c); }
    //Create a category for an event owned by the authenticated Organiser.
    [Authorize(Policy = "OrganiserOnly"), HttpPost("event/{eventId:int}"), HttpPost("/api/events/{eventId:int}/categories")]
    public async Task<IActionResult> Create(int eventId, CategoryRequest request)
    {
        var e = await db.Events.FindAsync(eventId); if (e is null) return NotFound(new { message = "Event not found." }); if (e.OrganiserID != AccountId()) return Forbid();
        var c = new Category { EventID = eventId, Name = request.Name.Trim(), DistanceKm = request.DistanceKm, MaxParticipants = request.MaxParticipants, EntryFee = request.EntryFee };
        db.Categories.Add(c); await db.SaveChangesAsync(); return CreatedAtAction(nameof(Get), new { id = c.CategoryID }, c);
    }
    //Update a category belonging to an event owned by the authenticated Organiser.
    [Authorize(Policy = "OrganiserOnly"), HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CategoryRequest request)
    {
        var c = await db.Categories.Include(x => x.Event).FirstOrDefaultAsync(x => x.CategoryID == id); if (c is null) return NotFound(); if (c.Event?.OrganiserID != AccountId()) return Forbid();
        if (request.MaxParticipants < await db.Enrolments.CountAsync(x => x.CategoryID == id && x.Status != EnrolmentStatus.Cancelled)) return Conflict(new { message = "Capacity cannot be lower than current active enrolments." });
        c.Name = request.Name.Trim(); c.DistanceKm = request.DistanceKm; c.MaxParticipants = request.MaxParticipants; c.EntryFee = request.EntryFee; await db.SaveChangesAsync(); return NoContent();
    }
    //Delete a category belonging to an event owned by the authenticated Organiser.
    [Authorize(Policy = "OrganiserOnly"), HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await db.Categories.Include(x => x.Event).FirstOrDefaultAsync(x => x.CategoryID == id); if (c is null) return NotFound(); if (c.Event?.OrganiserID != AccountId()) return Forbid();
        if (await db.Enrolments.AnyAsync(x => x.CategoryID == id)) return Conflict(new { message = "Category has enrolments and cannot be deleted." });
        db.Categories.Remove(c); await db.SaveChangesAsync(); return NoContent();
    }
    private int AccountId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
