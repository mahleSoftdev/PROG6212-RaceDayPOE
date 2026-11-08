using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDayPOE.Data;
using RaceDayPOE.Services;

namespace RaceDayPOE.Tests;

//>Runs the real API in memory, replacing SQL Server with an isolated EF Core in-memory database.
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string ProvisioningKey = "test-provisioning-key";

    private readonly string _databaseName = "RaceDayTests_" + Guid.NewGuid();

    //Replaces Azure Blob Storage in every test.
    public FakeImageStorage Images { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:OrganiserProvisioningKey"] = ProvisioningKey
            }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();
            services.RemoveAll<RaceDayDbContext>();
            services.AddDbContext<RaceDayDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            services.RemoveAll<IImageStorage>();
            services.AddSingleton<IImageStorage>(Images);
        });
    }
}
