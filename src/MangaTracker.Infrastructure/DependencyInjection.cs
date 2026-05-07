using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Abstractions.Auth;
using MangaTracker.Infrastructure.Auth;
using MangaTracker.Infrastructure.ExternalServices.ComicVine;
using MangaTracker.Infrastructure.Persistence;
using MangaTracker.Infrastructure.Repositories;
using MangaTracker.Application.Abstractions.Email;
using MangaTracker.Infrastructure.Email;
using MangaTracker.Infrastructure.BackgroundJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MangaTracker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MangaTrackerDb");

        services.AddDbContext<MangaTrackerDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserTokenRepository, UserTokenRepository>();

        services.AddScoped<ISeriesRepository, SeriesRepository>();
        services.AddScoped<IEditionRepository, EditionRepository>();
        services.AddScoped<ITomeRepository, TomeRepository>();
        services.AddScoped<IUserCollectionRepository, UserCollectionRepository>();

        services.Configure<AuthLinkOptions>(
                configuration.GetSection("AuthLinks"));

        services.Configure<AuthCleanupOptions>(
            configuration.GetSection("AuthCleanup"));

        services.AddHostedService<AuthCleanupBackgroundService>();

        services.AddScoped<ITokenGenerator, TokenGenerator>();
        services.AddScoped<ITokenHasher, TokenHasher>();
        services.AddScoped<IAuthLinkBuilder, AuthLinkBuilder>();
        services.AddScoped<IEmailSender, ConsoleEmailSender>();

        services.Configure<JwtOptions>(
            configuration.GetSection("Jwt"));

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services
        .AddOptions<ComicVineOptions>()
        .Bind(configuration.GetSection(ComicVineOptions.SectionName))
        .Validate(
            options => !string.IsNullOrWhiteSpace(options.BaseUrl),
            "Comic Vine base URL is required.")
        .Validate(
            options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _),
            "Comic Vine base URL must be a valid absolute URL.")
        .ValidateOnStart();

        services.AddHttpClient<IComicVineClient, ComicVineClient>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<ComicVineOptions>>()
                .Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MangaTracker/1.0");
        });

        return services;
    }
}