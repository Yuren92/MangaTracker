using System.Net;
using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Abstractions.Email;
using MangaTracker.Infrastructure.BackgroundJobs;
using MangaTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace MangaTracker.Tests.Integration.Infrastructure;

// Runs the real API (controllers, auth, EF Core, SQL Server) in memory against a
// throwaway database created from the migrations. Only the external services are
// replaced: email and Comic Vine.
//
// SQL Server comes from MANGATRACKER_TEST_SQLSERVER (CI uses a container) and
// defaults to LocalDB. SQLite is not an option: EF Core cannot translate the
// DateTimeOffset comparisons used by the token queries.
public sealed class MangaTrackerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DefaultServer =
        @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True";

    private readonly string _connectionString;

    public MangaTrackerApiFactory()
    {
        var server = Environment.GetEnvironmentVariable("MANGATRACKER_TEST_SQLSERVER");

        _connectionString = new SqlConnectionStringBuilder(string.IsNullOrWhiteSpace(server) ? DefaultServer : server)
        {
            InitialCatalog = $"MangaTrackerTests_{Guid.NewGuid():N}"
        }.ConnectionString;
    }

    public CapturingEmailSender Emails { get; } = new();

    public FakeComicVineClient ComicVine { get; } = new();

    public AdjustableClock Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:MangaTrackerDb", _connectionString);
        builder.UseSetting("Jwt:Issuer", "MangaTracker");
        builder.UseSetting("Jwt:Audience", "MangaTracker");
        builder.UseSetting("Jwt:SecretKey", "integration-tests-secret-key-with-enough-bytes");
        builder.UseSetting("Jwt:ExpirationMinutes", "60");
        builder.UseSetting("AuthLinks:FrontendBaseUrl", "https://manga-tracker.test");
        builder.UseSetting("ComicVine:ApiKey", "unused-in-tests");
        builder.UseSetting("Smtp:Host", "smtp.unused.test");
        builder.UseSetting("Smtp:FromEmail", "no-reply@manga-tracker.test");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);

            services.RemoveAll<IComicVineClient>();
            services.AddSingleton<IComicVineClient>(ComicVine);

            // Background jobs are run explicitly by the tests that cover them.
            services.Remove(services.Single(descriptor =>
                descriptor.ImplementationType == typeof(AuthCleanupBackgroundService)));
            services.Remove(services.Single(descriptor =>
                descriptor.ImplementationType == typeof(CatalogSyncBackgroundService)));
            services.Remove(services.Single(descriptor =>
                descriptor.ImplementationType == typeof(TomeImportBackgroundService)));

            services.RemoveAll<ITomeImportQueue>();
            services.AddSingleton<ITomeImportQueue, InlineTomeImportQueue>();

            // The test server has no client IP, so every request would share one
            // rate limit bucket. Give each request its own address instead; rate
            // limiting is covered by its own tests.
            services.AddSingleton<IStartupFilter, RandomClientIpStartupFilter>();
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MangaTrackerDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MangaTrackerDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
        }

        await DisposeAsync();
    }

    private sealed class RandomClientIpStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use((context, nextMiddleware) =>
                {
                    context.Connection.RemoteIpAddress = new IPAddress(Random.Shared.Next());
                    return nextMiddleware(context);
                });

                next(app);
            };
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<MangaTrackerApiFactory>
{
    public const string Name = "API";
}
