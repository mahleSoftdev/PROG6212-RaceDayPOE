using System.Text.Json.Serialization;

namespace RaceDayPOE.Web.Models.Api;

//Data shapes returned by the RaceDay API. These hold data only: all rules live in the API.

public enum EventType { Run, Walk, Cycle }
public enum EnrolmentStatus { Pending, Confirmed, Cancelled }
public enum AccountRole { Organiser, Participant }

public class AccountDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string? ProfilePictureUrl { get; set; }

    [JsonIgnore] public string FullName => $"{FirstName} {LastName}".Trim();
}

public class CategoryDto
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string Name { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
    public int Enrolled { get; set; }

    [JsonIgnore] public int SpotsLeft => Math.Max(0, MaxParticipants - Enrolled);
    [JsonIgnore] public bool IsFull => Enrolled >= MaxParticipants;
    [JsonIgnore] public string SpotsText => IsFull ? "Full" : $"{SpotsLeft} {(SpotsLeft == 1 ? "spot" : "spots")} left";
}

public class EventDto
{
    public int EventID { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime EventDate { get; set; }
    public EventType EventType { get; set; }
    public int VenueID { get; set; }
    public string? Venue { get; set; }
    public string? VenueCity { get; set; }
    public string? BannerUrl { get; set; }
    public int EnrolmentCount { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();

    [JsonIgnore] public bool IsUpcoming => EventDate.Date >= DateTime.Today;
}

public class VenueDto
{
    public int VenueID { get; set; }
    public string Name { get; set; } = "";
    public string AddressLine { get; set; } = "";
    public string City { get; set; } = "";
    public string Province { get; set; } = "";
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}

public class EnrolmentDto
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public string ParticipantName { get; set; } = "";
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = "";
    public int? EventID { get; set; }
    public string? EventName { get; set; }
    public DateTime? EventDate { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public EnrolmentStatus Status { get; set; }
}

public class ResultDto
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
    public DateTime RecordedAt { get; set; }
    public string? ParticipantName { get; set; }
    public string? EventName { get; set; }
    public DateTime? EventDate { get; set; }
    public string? CategoryName { get; set; }
    public int? TotalFinishers { get; set; }
}

public class CreatedEventDto
{
    public int EventID { get; set; }
}

public class UploadedImageDto
{
    public string? BannerUrl { get; set; }
}

//Request bodies sent to the API.
public record RegisterRequest(string FirstName, string LastName, string Email, string Password);
public record LoginRequest(string Email, string Password, AccountRole Role);
public record ProfileRequest(string FirstName, string LastName, string Email);
public record PasswordRequest(string CurrentPassword, string NewPassword);
public record EventRequest(string Name, string Description, DateTime EventDate, EventType EventType, int VenueID);
public record CategoryRequest(string Name, decimal DistanceKm, int MaxParticipants, decimal EntryFee);
public record VenueRequest(string Name, string AddressLine, string City, string Province, decimal Latitude, decimal Longitude);
public record EnrolmentRequest(int CategoryID);
public record EnrolmentStatusRequest(EnrolmentStatus Status);
public record ResultRequest(int EnrolmentID, TimeSpan FinishTime, int Position);
