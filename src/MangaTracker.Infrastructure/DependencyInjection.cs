using System.Net;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Http.Resilience;
using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Abstractions.Auth;
using MangaTracker.Infrastructure.Auth;
using MangaTracker.Infrastructure.ExternalServices.ComicVine;
using MangaTracker.Infrastructure.Persistence;
using MangaTracker.Infrastructure.Queries;
using MangaTracker.Infrastructure.Repositories;
using MangaTracker.Application.Abstractions.Email;
using MangaTracker.Infrastructure.Email;
using MangaTracker.Infrastructure.BackgroundJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace MangaTracker.Infrastructure;

public static class DependencyInjection
{
    // HS256 needs a key of at least 256 bits.
    private const int MinJwtSecretKeyBytes = 32;

    // "Enhance your calm": what Comic Vine answers once a key is being throttled.
    // https://comicvine.gamespot.com/forums/api-developers-2334/new-api-http-status-when-rate-limited-1751775/
    private const HttpStatusCode ComicVineRateLimitedStatus = (HttpStatusCode)420;

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("MangaTrackerDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'MangaTrackerDb' is not configured.");
        }

        services.AddDbContext<MangaTrackerDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserTokenRepository, UserTokenRepository>();

        services.AddScoped<ISeriesRepository, SeriesRepository>();
        services.AddScoped<IEditionRepository, EditionRepository>();
        services.AddScoped<ITomeRepository, TomeRepository>();
        services.AddScoped<IUserCollectionRepository, UserCollectionRepository>();
        services.AddScoped<ICollectionQueries, CollectionQueries>();

        services
            .AddOptions<AuthLinkOptions>()
            .Bind(configuration.GetSection("AuthLinks"))
            .Validate(
                options => IsAbsoluteUrl(options.FrontendBaseUrl),
                "AuthLinks:FrontendBaseUrl must be a valid absolute URL.")
            .Validate(
                options => environment.IsDevelopment() || IsHttpsUrl(options.FrontendBaseUrl),
                "AuthLinks:FrontendBaseUrl must use HTTPS outside Development.")
            .ValidateOnStart();

        services.Configure<AuthCleanupOptions>(
            configuration.GetSection("AuthCleanup"));

        services.AddScoped<AuthCleanupJob>();
        services.AddHostedService<AuthCleanupBackgroundService>();

        services.AddScoped<ITokenGenerator, TokenGenerator>();
        services.AddScoped<ITokenHasher, TokenHasher>();
        services.AddScoped<IAuthLinkBuilder, AuthLinkBuilder>();

        AddEmailSender(services, configuration, environment);

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection("Jwt"))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Issuer),
                "Jwt:Issuer is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Audience),
                "Jwt:Audience is required.")
            .Validate(
                options => Encoding.UTF8.GetByteCount(options.SecretKey) >= MinJwtSecretKeyBytes,
                $"Jwt:SecretKey must be at least {MinJwtSecretKeyBytes} bytes long.")
            .Validate(
                options => options.ExpirationMinutes is > 0 and <= 1440,
                "Jwt:ExpirationMinutes must be between 1 and 1440.")
            .ValidateOnStart();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services
            .AddOptions<ComicVineOptions>()
            .Bind(configuration.GetSection(ComicVineOptions.SectionName))
            .Validate(
                options => IsHttpsUrl(options.BaseUrl),
                "Comic Vine base URL must be a valid absolute HTTPS URL.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ApiKey),
                "Comic Vine API key is required.")
            .ValidateOnStart();

        services.AddSingleton<ComicVineRequestGate>();
        services.AddTransient<ComicVineThrottlingHandler>();

        var comicVineHttpClient = services.AddHttpClient<IComicVineClient, ComicVineClient>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<ComicVineOptions>>()
                .Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MangaTracker/1.0");
        });

        // Timeouts, retries and circuit breaking live in the resilience pipeline instead of
        // HttpClient.Timeout. Only GET requests are sent, so retrying them is safe.
        comicVineHttpClient.AddStandardResilienceHandler(resilience =>
        {
            // Retries honour Retry-After on 429 and use exponential backoff with jitter.
            // Comic Vine's own rate-limit status (420) is not retried: retrying while
            // throttled only makes the restriction harsher.
            resilience.Retry.MaxRetryAttempts = 2;

            // Each attempt includes the wait for a turn at the request gate (up to 5 s).
            resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
            resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);

            // After repeated failures, fail fast for a while instead of piling up requests
            // on a provider that is down or throttling us. The default minimum of 100
            // requests per window is never reached by this app, so the breaker would
            // never open; 5 is enough to tell a bad spell from a single error.
            resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            resilience.CircuitBreaker.MinimumThroughput = 5;
            resilience.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
            resilience.CircuitBreaker.ShouldHandle = arguments => ValueTask.FromResult(
                HttpClientResiliencePredicates.IsTransient(arguments.Outcome) ||
                arguments.Outcome.Result?.StatusCode == ComicVineRateLimitedStatus);
        });

        // Added after the resilience handler, so it runs inside it: every attempt,
        // retries included, waits for its turn.
        comicVineHttpClient.AddHttpMessageHandler<ComicVineThrottlingHandler>();

        return services;
    }

    private static void AddEmailSender(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Use cases queue emails (BackgroundEmailSender) and EmailDispatcherService sends
        // them with the transport chosen below, outside the HTTP request.
        services.AddSingleton(Channel.CreateBounded<EmailWorkItem>(new BoundedChannelOptions(1000)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        }));
        services.AddSingleton<IEmailSender, BackgroundEmailSender>();
        services.AddHostedService<EmailDispatcherService>();

        AddEmailTransport(services, configuration, environment);
    }

    private static void AddEmailTransport(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var smtpSection = configuration.GetSection("Smtp");

        if (string.IsNullOrWhiteSpace(smtpSection["Host"]))
        {
            // ConsoleEmailSender writes confirmation and password reset links (which carry
            // tokens) to the logs. That is only acceptable on a developer machine, so any
            // other environment must fail at startup instead of silently leaking them.
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    $"Smtp:Host is required in the '{environment.EnvironmentName}' environment.");
            }

            services.AddKeyedScoped<IEmailSender, ConsoleEmailSender>(EmailTransport.Key);
            return;
        }

        services
            .AddOptions<SmtpEmailOptions>()
            .Bind(smtpSection)
            .Validate(
                options => options.Port is > 0 and <= 65535,
                "Smtp:Port must be a valid TCP port.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.FromEmail),
                "Smtp:FromEmail is required.")
            .Validate(
                options => environment.IsDevelopment() || options.EnableSsl,
                "Smtp:EnableSsl must be true outside Development.")
            .ValidateOnStart();

        services.AddKeyedScoped<IEmailSender, SmtpEmailSender>(EmailTransport.Key);
    }

    private static bool IsAbsoluteUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool IsHttpsUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps;
    }
}
