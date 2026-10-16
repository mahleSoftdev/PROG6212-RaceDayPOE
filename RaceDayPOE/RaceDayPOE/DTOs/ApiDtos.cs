using System.ComponentModel.DataAnnotations;
using RaceDayPOE.Models;

namespace RaceDayPOE.DTOs;

public sealed class RegisterParticipantRequest
{
    [Required, MaxLength(100)] public string FirstName { get; init; } = "";
    [Required, MaxLength(100)] public string LastName { get; init; } = "";
    [Required, EmailAddress, MaxLength(255)] public string Email { get; init; } = "";
    [Required, MinLength(8), MaxLength(100)] public string Password { get; init; } = "";
}
public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(255)] public string Email { get; init; } = "";
    [Required, MinLength(8), MaxLength(100)] public string Password { get; init; } = "";
    [Required] public UserRole? Role { get; init; }
}
public sealed class ProvisionOrganiserRequest
{
    [Required, MaxLength(100)] public string FirstName { get; init; } = "";
    [Required, MaxLength(100)] public string LastName { get; init; } = "";
    [Required, EmailAddress, MaxLength(255)] public string Email { get; init; } = "";
    [Required, MinLength(12), MaxLength(100)] public string TemporaryPassword { get; init; } = "";
}
public sealed record AccountResponse(int Id, string FirstName, string LastName, string Email, string Role, DateTime CreatedAt);
public sealed class UpdateProfileRequest
{
    [Required, MaxLength(100)] public string FirstName { get; init; } = "";
    [Required, MaxLength(100)] public string LastName { get; init; } = "";
    [Required, EmailAddress, MaxLength(255)] public string Email { get; init; } = "";
}
public sealed class VenueRequest
{
    [Required, MaxLength(150)] public string Name { get; init; } = "";
    [Required, MaxLength(200)] public string AddressLine { get; init; } = "";
    [Required, MaxLength(100)] public string City { get; init; } = "";
    [Required, MaxLength(100)] public string Province { get; init; } = "";
    [Range(typeof(decimal), "-90", "90")] public decimal Latitude { get; init; }
    [Range(typeof(decimal), "-180", "180")] public decimal Longitude { get; init; }
}
public sealed class EventRequest
{
    [Required, MaxLength(150)] public string Name { get; init; } = "";
    [Required, MaxLength(500)] public string Description { get; init; } = "";
    [Required] public DateTime EventDate { get; init; }
    [Required] public EventType EventType { get; init; }
    [Range(1, int.MaxValue)] public int VenueID { get; init; }
}
public sealed class CategoryRequest
{
    [Required, MaxLength(100)] public string Name { get; init; } = "";
    [Range(typeof(decimal), "0.01", "9999.99")] public decimal DistanceKm { get; init; }
    [Range(1, int.MaxValue)] public int MaxParticipants { get; init; }
    [Range(typeof(decimal), "0", "99999999.99")] public decimal EntryFee { get; init; }
}
public sealed class EnrolmentRequest { [Range(1, int.MaxValue)] public int CategoryID { get; init; } }
public sealed class ResultRequest
{
    [Range(1, int.MaxValue)] public int EnrolmentID { get; init; }
    [Required] public TimeSpan FinishTime { get; init; }
    [Range(1, int.MaxValue)] public int Position { get; init; }
}

public sealed class ChangePasswordRequest
{
    [Required, MinLength(8), MaxLength(100)] public string CurrentPassword { get; init; } = "";
    [Required, MinLength(12), MaxLength(100)] public string NewPassword { get; init; } = "";
}

public sealed class EnrolmentStatusRequest { [Required] public EnrolmentStatus Status { get; init; } }
