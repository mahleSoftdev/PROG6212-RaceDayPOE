using System.Text.Json;

namespace RaceDayPOE.Web.Services;

//Turns API failures into short, friendly messages. Raw status codes, stack traces and JSON never reach the user.
public static class ApiMessages
{
    public const string Unreachable = "We couldn't reach the RaceDay service right now. Please try again in a moment.";
    public const string Unreadable = "The RaceDay service sent a response we couldn't understand. Please try again.";

    public static string ForStatus(int status) => status switch
    {
        400 => "Some of the information you entered isn't valid. Please check it and try again.",
        401 => "Please sign in to continue.",
        403 => "You don't have permission to do that.",
        404 => "We couldn't find what you were looking for.",
        409 => "That conflicts with existing information.",
        413 => "That file is too large.",
        415 => "That file type isn't supported.",
        503 => "That feature is temporarily unavailable. Please try again later.",
        _ => "Something went wrong on our side. Please try again."
    };

    //Prefers the API's own { "message": "..." } text, then readable validation messages, then a generic message.
    public static string FromBody(string? body, int status)
    {
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(message.GetString()))
                        return message.GetString()!;

                    if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        var readable = new List<string>();
                        foreach (var field in errors.EnumerateObject())
                        {
                            if (field.Name.StartsWith('$') || field.Value.ValueKind != JsonValueKind.Array) continue;
                            foreach (var item in field.Value.EnumerateArray())
                            {
                                var text = item.GetString();
                                if (!string.IsNullOrWhiteSpace(text) && !text.Contains("JSON", StringComparison.OrdinalIgnoreCase)) readable.Add(text);
                            }
                        }
                        if (readable.Count > 0) return string.Join(" ", readable.Take(3));
                    }
                }
            }
            catch (JsonException) { }
        }
        return ForStatus(status);
    }
}
