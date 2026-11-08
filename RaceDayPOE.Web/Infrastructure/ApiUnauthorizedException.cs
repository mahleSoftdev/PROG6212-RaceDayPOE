namespace RaceDayPOE.Web.Infrastructure;

//Raised when the API rejects a signed-in user's credentials (for example the API session expired).
public sealed class ApiUnauthorizedException : Exception
{
    public ApiUnauthorizedException() : base("The API session is no longer valid.") { }
}
