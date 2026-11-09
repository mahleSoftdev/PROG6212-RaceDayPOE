using System.ComponentModel.DataAnnotations;
using RaceDayPOE.Web.Models.Api;

namespace RaceDayPOE.Web.Models.ViewModels;

public class UpdateProfileForm
{
    [Required(ErrorMessage = "Enter your first name."), StringLength(100), Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Enter your last name."), StringLength(100), Display(Name = "Last name")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Enter your email address."), EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(255)]
    public string Email { get; set; } = "";
}

public class ChangePasswordForm
{
    [Required(ErrorMessage = "Enter your current password."), DataType(DataType.Password), Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Choose a new password."), DataType(DataType.Password), StringLength(100, MinimumLength = 12, ErrorMessage = "Your new password must be at least 12 characters."), Display(Name = "New password")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Confirm your new password."), DataType(DataType.Password), Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match."), Display(Name = "Confirm new password")]
    public string ConfirmNewPassword { get; set; } = "";
}

public class ProfilePageViewModel
{
    public AccountDto Account { get; set; } = new();
    public UpdateProfileForm Details { get; set; } = new();
    public ChangePasswordForm Password { get; set; } = new();
}
