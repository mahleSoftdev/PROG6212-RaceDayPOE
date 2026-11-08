using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayPOE.Data;
using RaceDayPOE.DTOs;
using RaceDayPOE.Models;
using RaceDayPOE.Services;

namespace RaceDayPOE.Controllers;

[ApiController, Route("api/events")]
public class EventsController(RaceDayDbContext db, IImageStorage images) : ControllerBase
{
    //Shared projection used by the public list, the detail endpoint and the Organiser's own list.
    private static readonly Expression<Func<Event, EventView>> ToView = e => new EventView
    {
        EventID = e.EventID, Name = e.Name, Description = e.Description, EventDate = e.EventDate, EventType = e.EventType,
        VenueID = e.VenueID, Venue = e.Venue == null ? null : e.Venue.Name, VenueCity = e.Venue == null ? null : e.Venue.City,
        BannerUrl = e.BannerUrl,
        EnrolmentCount = e.Categories.SelectMany(c => c.Enrolments).Count(x => x.Status != EnrolmentStatus.Cancelled),
        Categories = e.Categories.Select(c => new CategoryView
        {
            CategoryID = c.CategoryID, EventID = c.EventID, Name = c.Name, DistanceKm = c.DistanceKm, MaxParticipants = c.MaxParticipants, EntryFee = c.EntryFee,
            Enrolled = c.Enrolments.Count(x => x.Status != EnrolmentStatus.Cancelled)
        }).ToList()
    };

    //Browse events. Public access supports event discovery.
    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await db.Events.AsNoTracking().OrderBy(e => e.EventDate).Select(ToView).ToListAsync());
    [AllowAnonymous, HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var e = await db.Events.AsNoTracking().Where(x => x.EventID == id).Select(ToView).FirstOrDefaultAsync();
        return e is null ? NotFound() : Ok(e);
    }
    //Events owned by the authenticated Organiser, with enrolment totals (dashboard and management pages).
    [Authorize(Policy = "OrganiserOnly"), HttpGet("/api/organisers/me/events")]
    public async Task<IActionResult> GetMine()
    {
        var organiserId = AccountId();
        return Ok(await db.Events.AsNoTracking().Where(e => e.OrganiserID == organiserId).OrderBy(e => e.EventDate).Select(ToView).ToListAsync());
    }
    //Upload or replace the event banner. The image is stored in Azure Blob Storage and its URL saved on the event.
    [Authorize(Policy = "OrganiserOnly"), HttpPost("{id:int}/banner"), Consumes("multipart/form-data")]
    [RequestSizeLimit(ImageValidator.MaxBytes + 1024 * 1024)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file, CancellationToken cancellationToken)
    {
        var e = await db.Events.FindAsync(new object[] { id }, cancellationToken); if (e is null) return NotFound(); if (e.OrganiserID != AccountId()) return Forbid();
        var image = await ImageValidator.InspectAsync(file, cancellationToken);
        if (!image.IsValid) return BadRequest(new { message = image.Error });
        string url;
        try
        {
            await using var stream = file.OpenReadStream();
            url = await images.UploadAsync("events", stream, image.ContentType!, image.Extension!, cancellationToken);
        }
        catch (ImageStorageUnavailableException ex) { return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message }); }
        var previous = e.BannerUrl;
        e.BannerUrl = url; await db.SaveChangesAsync(cancellationToken);
        await images.DeleteAsync(previous, cancellationToken);
        return Ok(new { bannerUrl = url });
    }
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
        var banner = e.BannerUrl; db.Events.Remove(e); await db.SaveChangesAsync(); await images.DeleteAsync(banner); return NoContent();
    }
    private int AccountId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
