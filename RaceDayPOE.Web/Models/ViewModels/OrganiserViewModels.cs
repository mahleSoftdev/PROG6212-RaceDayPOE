using System.ComponentModel.DataAnnotations;
using RaceDayPOE.Web.Models.Api;

namespace RaceDayPOE.Web.Models.ViewModels;

public class DashboardViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public int TotalEvents => Events.Count;
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public List<EventDto> Upcoming => Events.Where(e => e.IsUpcoming).OrderBy(e => e.EventDate).ToList();
    public DateTime? NextEventDate => Upcoming.FirstOrDefault()?.EventDate;
}

public class EventFormViewModel
{
    public int? EventId { get; set; }
    public bool IsEdit => EventId is not null;

    [Required(ErrorMessage = "Enter the event name."), StringLength(150)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Describe the event."), StringLength(500, ErrorMessage = "The description can be at most 500 characters.")]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "Choose the event date."), DataType(DataType.Date), Display(Name = "Event date")]
    public DateTime? EventDate { get; set; }

    [Display(Name = "Event type")]
    public EventType EventType { get; set; } = EventType.Run;

    [Range(1, int.MaxValue, ErrorMessage = "Choose a venue."), Display(Name = "Location (venue)")]
    public int VenueID { get; set; }

    //Only used when creating: becomes the event's first category (more are added on the Categories page).
    [Range(0.01, 9999.99, ErrorMessage = "Enter a distance between 0.01 and 9999.99 km."), Display(Name = "Main distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Range(0, 99999999.99, ErrorMessage = "Enter a valid entry fee."), Display(Name = "Entry fee (R)")]
    public decimal EntryFee { get; set; }

    [Range(1, 100000, ErrorMessage = "Enter a capacity of at least 1."), Display(Name = "Maximum participants")]
    public int MaxParticipants { get; set; } = 100;

    [Display(Name = "Event banner image")]
    public IFormFile? Banner { get; set; }

    public string? ExistingBannerUrl { get; set; }
    public List<VenueDto> Venues { get; set; } = new();
}

public class CategoryForm
{
    [Required(ErrorMessage = "Enter a category name, e.g. \"10km Open\" or \"Under 20\"."), StringLength(100)]
    public string Name { get; set; } = "";

    [Range(0.01, 9999.99, ErrorMessage = "Enter a distance between 0.01 and 9999.99 km."), Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Range(1, 100000, ErrorMessage = "Enter a capacity of at least 1."), Display(Name = "Maximum participants")]
    public int MaxParticipants { get; set; } = 100;

    [Range(0, 99999999.99, ErrorMessage = "Enter a valid entry fee."), Display(Name = "Entry fee (R)")]
    public decimal EntryFee { get; set; }
}

public class CategoriesPageViewModel
{
    public EventDto? Event { get; set; }
    public List<EventDto> Events { get; set; } = new();
    public CategoryForm Form { get; set; } = new();
    public int? EditingCategoryId { get; set; }
}

public class VenueForm
{
    public int? VenueId { get; set; }

    [Required(ErrorMessage = "Enter the venue name."), StringLength(150)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Enter the street address."), StringLength(200), Display(Name = "Address")]
    public string AddressLine { get; set; } = "";

    [Required(ErrorMessage = "Enter the city."), StringLength(100)]
    public string City { get; set; } = "";

    [Required(ErrorMessage = "Enter the province."), StringLength(100)]
    public string Province { get; set; } = "";

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal Longitude { get; set; }
}

public class EnrolmentsPageViewModel
{
    public EventDto? Event { get; set; }
    public List<EventDto> Events { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
    public EnrolmentStatus? StatusFilter { get; set; }
}

public class ResultRow
{
    public EnrolmentDto Enrolment { get; set; } = new();
    public ResultDto? Result { get; set; }
}

public class ResultsPageViewModel
{
    public EventDto? Event { get; set; }
    public List<EventDto> Events { get; set; } = new();
    public List<ResultRow> Rows { get; set; } = new();
    public bool EventHasHappened => Event is not null && Event.EventDate.Date <= DateTime.Today;
}
