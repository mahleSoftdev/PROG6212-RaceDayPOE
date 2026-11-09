using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RaceDayPOE.Web.Infrastructure;
using RaceDayPOE.Web.Services;

//Invariant culture everywhere so decimals ("28.2") and dates parse the same on every machine and in Docker.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add<ApiSessionExpiredFilter>();
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Web.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = ".RaceDay.Web.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Events.OnValidatePrincipal = AccountSession.ValidateAsync;
});
builder.Services.AddAuthorization();

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

//Users always see a friendly page, never a stack trace or raw error text.
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "areas", pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

public partial class Program { }
