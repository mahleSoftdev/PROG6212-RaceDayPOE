namespace RaceDayPOE.Web.Services;

//Outcome of one API call. Controllers never see raw HTTP errors: failures carry a friendly message.
public class ApiResult
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public string Error { get; init; } = "";

    public static ApiResult Ok(int statusCode = 200) => new() { Success = true, StatusCode = statusCode };
    public static ApiResult Fail(string error, int statusCode = 0) => new() { Success = false, StatusCode = statusCode, Error = error };
}

public sealed class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }

    public static ApiResult<T> Ok(T data, int statusCode = 200) => new() { Success = true, StatusCode = statusCode, Data = data };
    public new static ApiResult<T> Fail(string error, int statusCode = 0) => new() { Success = false, StatusCode = statusCode, Error = error };
}
