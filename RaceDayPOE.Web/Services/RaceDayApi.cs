using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RaceDayPOE.Web.Infrastructure;
using RaceDayPOE.Web.Models.Api;

namespace RaceDayPOE.Web.Services;

//Typed HttpClient for the RaceDay API (see ApiCookieHandler for how the signed-in user is identified).
public sealed class RaceDayApi : IRaceDayApi
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;
    private readonly ILogger<RaceDayApi> _logger;

    public RaceDayApi(HttpClient http, ILogger<RaceDayApi> logger) => (_http, _logger) = (http, logger);

    // ---- accounts and sessions ----
    public Task<ApiResult<AccountDto>> RegisterAsync(RegisterRequest request) => SendAsync<AccountDto>(HttpMethod.Post, "api/auth/register", request, expectSession: false);

    public async Task<ApiResult<AccountDto>> LoginAsync(LoginRequest request)
    {
        var result = await SendAsync<AccountDto>(HttpMethod.Post, "api/auth/login", request, expectSession: false);
        //The API answers 400/401 for a bad email, password or role. Keep that vague on purpose.
        return result.StatusCode is 400 or 401 ? ApiResult<AccountDto>.Fail("Invalid email, password, or role.", result.StatusCode) : result;
    }

    public Task<ApiResult> LogoutAsync() => SendAsync(HttpMethod.Post, "api/auth/logout", null, expectSession: false);
    public Task<ApiResult<AccountDto>> GetProfileAsync() => SendAsync<AccountDto>(HttpMethod.Get, "api/users/me");
    public Task<ApiResult<AccountDto>> UpdateProfileAsync(ProfileRequest request) => SendAsync<AccountDto>(HttpMethod.Put, "api/users/me", request);
    public Task<ApiResult> ChangePasswordAsync(PasswordRequest request) => SendAsync(HttpMethod.Put, "api/users/me/password", request);
    public Task<ApiResult<AccountDto>> UploadProfilePictureAsync(Stream content, string fileName, string contentType) =>
        UploadAsync<AccountDto>("api/users/me/picture", content, fileName, contentType);

    // ---- events ----
    public Task<ApiResult<List<EventDto>>> GetEventsAsync() => SendAsync<List<EventDto>>(HttpMethod.Get, "api/events", expectSession: false);
    public Task<ApiResult<EventDto>> GetEventAsync(int eventId) => SendAsync<EventDto>(HttpMethod.Get, $"api/events/{eventId}", expectSession: false);
    public Task<ApiResult<List<EventDto>>> GetMyEventsAsync() => SendAsync<List<EventDto>>(HttpMethod.Get, "api/organisers/me/events");
    public Task<ApiResult<CreatedEventDto>> CreateEventAsync(EventRequest request) => SendAsync<CreatedEventDto>(HttpMethod.Post, "api/events", request);
    public Task<ApiResult> UpdateEventAsync(int eventId, EventRequest request) => SendAsync(HttpMethod.Put, $"api/events/{eventId}", request);
    public Task<ApiResult> DeleteEventAsync(int eventId) => SendAsync(HttpMethod.Delete, $"api/events/{eventId}");
    public Task<ApiResult<UploadedImageDto>> UploadEventBannerAsync(int eventId, Stream content, string fileName, string contentType) =>
        UploadAsync<UploadedImageDto>($"api/events/{eventId}/banner", content, fileName, contentType);

    // ---- venues ----
    public Task<ApiResult<List<VenueDto>>> GetVenuesAsync() => SendAsync<List<VenueDto>>(HttpMethod.Get, "api/venues", expectSession: false);
    public Task<ApiResult<VenueDto>> GetVenueAsync(int venueId) => SendAsync<VenueDto>(HttpMethod.Get, $"api/venues/{venueId}", expectSession: false);
    public Task<ApiResult> CreateVenueAsync(VenueRequest request) => SendAsync(HttpMethod.Post, "api/venues", request);
    public Task<ApiResult> UpdateVenueAsync(int venueId, VenueRequest request) => SendAsync(HttpMethod.Put, $"api/venues/{venueId}", request);
    public Task<ApiResult> DeleteVenueAsync(int venueId) => SendAsync(HttpMethod.Delete, $"api/venues/{venueId}");

    // ---- categories ----
    public Task<ApiResult<List<CategoryDto>>> GetCategoriesAsync(int eventId) => SendAsync<List<CategoryDto>>(HttpMethod.Get, $"api/events/{eventId}/categories", expectSession: false);
    public Task<ApiResult<CategoryDto>> GetCategoryAsync(int categoryId) => SendAsync<CategoryDto>(HttpMethod.Get, $"api/categories/{categoryId}", expectSession: false);
    public Task<ApiResult> CreateCategoryAsync(int eventId, CategoryRequest request) => SendAsync(HttpMethod.Post, $"api/events/{eventId}/categories", request);
    public Task<ApiResult> UpdateCategoryAsync(int categoryId, CategoryRequest request) => SendAsync(HttpMethod.Put, $"api/categories/{categoryId}", request);
    public Task<ApiResult> DeleteCategoryAsync(int categoryId) => SendAsync(HttpMethod.Delete, $"api/categories/{categoryId}");

    // ---- enrolments ----
    public Task<ApiResult> EnrolAsync(int eventId, int categoryId) => SendAsync(HttpMethod.Post, $"api/events/{eventId}/enrolments", new EnrolmentRequest(categoryId));
    public Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync() => SendAsync<List<EnrolmentDto>>(HttpMethod.Get, "api/enrolments");
    public Task<ApiResult> CancelEnrolmentAsync(int enrolmentId) => SendAsync(HttpMethod.Put, $"api/enrolments/{enrolmentId}/cancel");
    public Task<ApiResult<List<EnrolmentDto>>> GetEventEnrolmentsAsync(int eventId) => SendAsync<List<EnrolmentDto>>(HttpMethod.Get, $"api/events/{eventId}/enrolments");
    public Task<ApiResult> UpdateEnrolmentStatusAsync(int enrolmentId, EnrolmentStatus status) => SendAsync(HttpMethod.Put, $"api/enrolments/{enrolmentId}", new EnrolmentStatusRequest(status));

    // ---- results ----
    public Task<ApiResult<List<ResultDto>>> GetMyResultsAsync() => SendAsync<List<ResultDto>>(HttpMethod.Get, "api/participants/me/results");
    public Task<ApiResult<List<ResultDto>>> GetEventResultsAsync(int eventId) => SendAsync<List<ResultDto>>(HttpMethod.Get, $"api/events/{eventId}/results");
    public Task<ApiResult> CreateResultAsync(ResultRequest request) => SendAsync(HttpMethod.Post, "api/results", request);
    public Task<ApiResult> UpdateResultAsync(int resultId, ResultRequest request) => SendAsync(HttpMethod.Put, $"api/results/{resultId}", request);
    public Task<ApiResult> DeleteResultAsync(int resultId) => SendAsync(HttpMethod.Delete, $"api/results/{resultId}");

    // ---- plumbing ----
    private async Task<ApiResult<T>> UploadAsync<T>(string url, Stream content, string fileName, string contentType)
    {
        var file = new StreamContent(content);
        file.Headers.ContentType = MediaTypeHeaderValue.TryParse(contentType, out var parsed) ? parsed : new MediaTypeHeaderValue("application/octet-stream");
        using var form = new MultipartFormDataContent { { file, "file", string.IsNullOrWhiteSpace(fileName) ? "upload" : Path.GetFileName(fileName) } };
        return await SendAsync<T>(HttpMethod.Post, url, content: form);
    }

    private async Task<ApiResult> SendAsync(HttpMethod method, string url, object? body = null, bool expectSession = true)
    {
        var (response, failure) = await ExecuteAsync(method, url, body, null, expectSession);
        if (failure is not null) return failure;
        using (response)
            return response!.IsSuccessStatusCode ? ApiResult.Ok((int)response.StatusCode) : await FailureAsync(response);
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body = null, bool expectSession = true, HttpContent? content = null)
    {
        var (response, failure) = await ExecuteAsync(method, url, body, content, expectSession);
        if (failure is not null) return ApiResult<T>.Fail(failure.Error, failure.StatusCode);
        using (response)
        {
            if (!response!.IsSuccessStatusCode)
            {
                var failed = await FailureAsync(response);
                return ApiResult<T>.Fail(failed.Error, failed.StatusCode);
            }
            try
            {
                var data = await response.Content.ReadFromJsonAsync<T>(Json);
                return data is null ? ApiResult<T>.Fail(ApiMessages.Unreadable, (int)response.StatusCode) : ApiResult<T>.Ok(data, (int)response.StatusCode);
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException)
            {
                _logger.LogWarning(ex, "Could not read the API response for {Url}", url);
                return ApiResult<T>.Fail(ApiMessages.Unreadable, (int)response.StatusCode);
            }
        }
    }

    //Sends the request. A 401 for a signed-in user means their API session ended, which the UI handles by signing them out.
    private async Task<(HttpResponseMessage? Response, ApiResult? Failure)> ExecuteAsync(HttpMethod method, string url, object? body, HttpContent? content, bool expectSession)
    {
        using var request = new HttpRequestMessage(method, url);
        if (content is not null) request.Content = content;
        else if (body is not null) request.Content = JsonContent.Create(body, body.GetType(), options: Json);
        try
        {
            var response = await _http.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.Unauthorized && expectSession)
            {
                response.Dispose();
                throw new ApiUnauthorizedException();
            }
            return (response, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "RaceDay API call {Method} {Url} failed", method, url);
            return (null, ApiResult.Fail(ApiMessages.Unreachable));
        }
    }

    private static async Task<ApiResult> FailureAsync(HttpResponseMessage response)
    {
        string? body = null;
        try { body = await response.Content.ReadAsStringAsync(); } catch (Exception) { }
        var status = (int)response.StatusCode;
        return ApiResult.Fail(ApiMessages.FromBody(body, status), status);
    }
}
