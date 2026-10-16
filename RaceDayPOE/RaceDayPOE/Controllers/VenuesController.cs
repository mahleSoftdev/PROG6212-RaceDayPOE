using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayPOE.Data;
using RaceDayPOE.DTOs;
using RaceDayPOE.Models;

namespace RaceDayPOE.Controllers;

[ApiController, Route("api/venues")]
public class VenuesController(RaceDayDbContext db) : ControllerBase
{
    //List venues available for events.
    [AllowAnonymous, HttpGet]
    public async Task<ActionResult<IEnumerable<Venue>>> GetAll() => Ok(await db.Venues.AsNoTracking().ToListAsync());
    //Get a venue by ID.
    [AllowAnonymous, HttpGet("{id:int}")]
    public async Task<ActionResult<Venue>> Get(int id) { var v = await db.Venues.AsNoTracking().FirstOrDefaultAsync(x => x.VenueID == id); return v is null ? NotFound() : Ok(v); }
    //Create a venue. Organisers only.
    [Authorize(Policy = "OrganiserOnly"), HttpPost]
    public async Task<ActionResult<Venue>> Create(VenueRequest request)
    {
        var v = new Venue { Name = request.Name.Trim(), AddressLine = request.AddressLine.Trim(), City = request.City.Trim(), Province = request.Province.Trim(), Latitude = request.Latitude, Longitude = request.Longitude };
        db.Venues.Add(v); await db.SaveChangesAsync(); return CreatedAtAction(nameof(Get), new { id = v.VenueID }, v);
    }
    //Update a venue. Organisers only.
    [Authorize(Policy = "OrganiserOnly"), HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, VenueRequest request)
    {
        var v = await db.Venues.FindAsync(id); if (v is null) return NotFound();
        v.Name = request.Name.Trim(); v.AddressLine = request.AddressLine.Trim(); v.City = request.City.Trim(); v.Province = request.Province.Trim(); v.Latitude = request.Latitude; v.Longitude = request.Longitude;
        await db.SaveChangesAsync(); return NoContent();
    }
    //Delete a venue when no events reference it. Organisers only.
    [Authorize(Policy = "OrganiserOnly"), HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var v = await db.Venues.FindAsync(id); if (v is null) return NotFound();
        if (await db.Events.AnyAsync(e => e.VenueID == id)) return Conflict(new { message = "Venue is referenced by an event." });
        db.Venues.Remove(v); await db.SaveChangesAsync(); return NoContent();
    }
}
