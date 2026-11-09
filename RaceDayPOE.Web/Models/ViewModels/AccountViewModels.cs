using System.ComponentModel.DataAnnotations;
using RaceDayPOE.Web.Models.Api;

namespace RaceDayPOE.Web.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your email address."), EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(255)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Enter your password."), DataType(DataType.Password), StringLength(100)]
    public string Password { get; set; } = "";

    [Display(Name = "I am a")]
    public AccountRole Role { get; set; } = AccountRole.Participant;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Enter your first name."), StringLength(100), Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Enter your last name."), StringLength(100), Display(Name = "Last name")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Enter your email address."), EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(255)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Choose a password."), DataType(DataType.Password), StringLength(100, MinimumLength = 8, ErrorMessage = "Your password must be at least 8 characters.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Confirm your password."), DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "The passwords do not match."), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";
}
