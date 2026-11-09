using RaceDayPOE.Web.Services;

namespace RaceDayPOE.Web.Tests;

//Users must only ever see friendly text, never raw status codes, JSON or stack traces.
public class ApiMessagesTests
{
    [Fact]
    public void FromBody_PrefersTheApisOwnMessage() =>
        Assert.Equal("This category has reached capacity.", ApiMessages.FromBody("{\"message\":\"This category has reached capacity.\"}", 409));

    [Fact]
    public void FromBody_UsesReadableValidationMessages()
    {
        var body = "{\"title\":\"One or more validation errors occurred.\",\"errors\":{\"Name\":[\"The Name field is required.\"]}}";
        Assert.Equal("The Name field is required.", ApiMessages.FromBody(body, 400));
    }

    [Fact]
    public void FromBody_HidesJsonBindingErrors()
    {
        var body = "{\"errors\":{\"$.eventDate\":[\"The JSON value could not be converted to System.DateTime.\"]}}";
        Assert.Equal(ApiMessages.ForStatus(400), ApiMessages.FromBody(body, 400));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("<html>Server Error</html>")]
    [InlineData("{\"unexpected\":true}")]
    public void FromBody_FallsBackToAGenericMessage(string? body) => Assert.Equal(ApiMessages.ForStatus(500), ApiMessages.FromBody(body, 500));

    [Theory]
    [InlineData(403, "permission")]
    [InlineData(404, "find")]
    [InlineData(409, "conflicts")]
    [InlineData(503, "unavailable")]
    [InlineData(500, "went wrong")]
    public void ForStatus_IsFriendly(int status, string expectedFragment) => Assert.Contains(expectedFragment, ApiMessages.ForStatus(status));
}
