using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Abstractions.Auth;
using MangaTracker.Infrastructure.Auth;
using MangaTracker.Infrastructure.ExternalServices.MyAnimeList;
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

            services.AddScoped<IMangaCollectionRepository, MangaCollectionRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserTokenRepository, UserTokenRepository>();

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

            services.Configure<MalOptions>(
            configuration.GetSection("MyAnimeList"));

            services.AddHttpClient<IMalMangaClient, MalMangaClient>((serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<MalOptions>>()
                    .Value;

                client.BaseAddress = new Uri(options.BaseUrl);
            });

            return services;
        }
    }