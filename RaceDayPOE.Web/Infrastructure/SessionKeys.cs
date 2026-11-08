namespace RaceDayPOE.Web.Infrastructure;

//Keys used in the server-side MVC session.
public static class SessionKeys
{
    //The API's authentication cookie value, forwarded on every API call made for this user.
    public const string ApiAuth = "RaceDay.ApiAuth";
    public const string AccountId = "RaceDay.AccountId";
    public const string Role = "RaceDay.Role";
    public const string DisplayName = "RaceDay.DisplayName";
}

public static class RoleNames
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}
