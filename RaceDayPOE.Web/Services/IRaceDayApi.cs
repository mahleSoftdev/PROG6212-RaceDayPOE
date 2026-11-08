using RaceDayPOE.Web.Models.Api;

namespace RaceDayPOE.Web.Services;

//Every data operation in the MVC app goes through this interface to the Part 2 API. There is no database access here.
public interface IRaceDayApi
{
    //Accounts and sessions
    Task<ApiResult<AccountDto>> RegisterAsync(RegisterRequest request);
    Task<ApiResult<AccountDto>> LoginAsync(LoginRequest request);
    Task<ApiResult> LogoutAsync();
    Task<ApiResult<AccountDto>> GetProfileAsync();
    Task<ApiResult<AccountDto>> UpdateProfileAsync(ProfileRequest request);
    Task<ApiResult> ChangePasswordAsync(PasswordRequest request);
    Task<ApiResult<AccountDto>> UploadProfilePictureAsync(Stream content, string fileName, string contentType);

    //Events
    Task<ApiResult<List<EventDto>>> GetEventsAsync();
    Task<ApiResult<EventDto>> GetEventAsync(int eventId);
    Task<ApiResult<List<EventDto>>> GetMyEventsAsync();
    Task<ApiResult<CreatedEventDto>> CreateEventAsync(EventRequest request);
    Task<ApiResult> UpdateEventAsync(int eventId, EventRequest request);
    Task<ApiResult> DeleteEventAsync(int eventId);
    Task<ApiResult<UploadedImageDto>> UploadEventBannerAsync(int eventId, Stream content, string fileName, string contentType);

    //Venues
    Task<ApiResult<List<VenueDto>>> GetVenuesAsync();
    Task<ApiResult<VenueDto>> GetVenueAsync(int venueId);
    Task<ApiResult> CreateVenueAsync(VenueRequest request);
    Task<ApiResult> UpdateVenueAsync(int venueId, VenueRequest request);
    Task<ApiResult> DeleteVenueAsync(int venueId);

    //Categories
    Task<ApiResult<List<CategoryDto>>> GetCategoriesAsync(int eventId);
    Task<ApiResult<CategoryDto>> GetCategoryAsync(int categoryId);
    Task<ApiResult> CreateCategoryAsync(int eventId, CategoryRequest request);
    Task<ApiResult> UpdateCategoryAsync(int categoryId, CategoryRequest request);
    Task<ApiResult> DeleteCategoryAsync(int categoryId);

    //Enrolments
    Task<ApiResult> EnrolAsync(int eventId, int categoryId);
    Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync();
    Task<ApiResult> CancelEnrolmentAsync(int enrolmentId);
    Task<ApiResult<List<EnrolmentDto>>> GetEventEnrolmentsAsync(int eventId);
    Task<ApiResult> UpdateEnrolmentStatusAsync(int enrolmentId, EnrolmentStatus status);

    //Results
    Task<ApiResult<List<ResultDto>>> GetMyResultsAsync();
    Task<ApiResult<List<ResultDto>>> GetEventResultsAsync(int eventId);
    Task<ApiResult> CreateResultAsync(ResultRequest request);
    Task<ApiResult> UpdateResultAsync(int resultId, ResultRequest request);
    Task<ApiResult> DeleteResultAsync(int resultId);
}
