using RaceDayPOE.Web.Models.Api;

namespace RaceDayPOE.Web.Models.ViewModels;

public class EventFilterViewModel
{
    public string? Search { get; set; }
    public EventType? Type { get; set; }
    public string? City { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public bool IncludePast { get; set; }

    public List<EventDto> Events { get; set; } = new();
    public List<string> Cities { get; set; } = new();
    public int UpcomingCount { get; set; }
    public bool HasFilters => !string.IsNullOrWhiteSpace(Search) || Type is not null || !string.IsNullOrWhiteSpace(City) || From is not null || To is not null || IncludePast;
}

public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();
    //Categories the signed-in Participant is already actively enrolled in.
    public HashSet<int> EnrolledCategoryIds { get; set; } = new();
}
