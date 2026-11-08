using RaceDayPOE.Web.Infrastructure;
using RaceDayPOE.Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Web.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

//The only way this app reads or writes data: the Part 2 API (base address comes from configuration / Api__BaseUrl).
var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiBase))
    throw new InvalidOperationException("Configure Api:BaseUrl (or the Api__BaseUrl environment variable) with the RaceDay API address.");
builder.Services.AddTransient<ApiCookieHandler>();
builder.Services.AddHttpClient<IRaceDayApi, RaceDayApi>(client =>
    {
        client.BaseAddress = new Uri(apiBase.AbsoluteUri.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    //Cookies are managed per user by ApiCookieHandler, so the shared handler must not keep its own cookie jar.
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { UseCookies = false, AllowAutoRedirect = true, MaxAutomaticRedirections = 3 })
    .AddHttpMessageHandler<ApiCookieHandler>();

var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
