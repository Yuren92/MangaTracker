using AwesomeAssertions;
using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Abstractions.Email;
using MangaTracker.Infrastructure;
using MangaTracker.Infrastructure.Auth;
using MangaTracker.Infrastructure.Email;
using MangaTracker.Infrastructure.ExternalServices.ComicVine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MangaTracker.Tests.Infrastructure;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_should_fail_outside_development_when_smtp_is_not_configured()
    {
        var configuration = BuildConfiguration(("Smtp:Host", ""));

        var act = () => new ServiceCollection().AddInfrastructure(configuration, Environment(Environments.Production));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Smtp:Host*Production*");
    }

    [Fact]
    public void AddInfrastructure_should_use_console_email_sender_in_development_when_smtp_is_not_configured()
    {
        var configuration = BuildConfiguration(("Smtp:Host", ""));

        var services = new ServiceCollection()
            .AddInfrastructure(configuration, Environment(Environments.Development));

        services.Should().Contain(descriptor => IsEmailTransport(descriptor, typeof(ConsoleEmailSender)));
    }

    [Fact]
    public void AddInfrastructure_should_use_smtp_email_sender_when_smtp_is_configured()
    {
        var configuration = BuildConfiguration();

        var services = new ServiceCollection()
            .AddInfrastructure(configuration, Environment(Environments.Production));

        services.Should().Contain(descriptor => IsEmailTransport(descriptor, typeof(SmtpEmailSender)));
    }

    [Fact]
    public void Use_cases_should_get_the_queued_email_sender()
    {
        var services = new ServiceCollection()
            .AddInfrastructure(BuildConfiguration(), Environment(Environments.Production));

        services.Should().Contain(descriptor =>
            !descriptor.IsKeyedService &&
            descriptor.ServiceType == typeof(IEmailSender) &&
            descriptor.ImplementationType == typeof(BackgroundEmailSender));
    }

    private static bool IsEmailTransport(ServiceDescriptor descriptor, Type implementationType)
    {
        return descriptor.IsKeyedService &&
            Equals(descriptor.ServiceKey, EmailTransport.Key) &&
            descriptor.ServiceType == typeof(IEmailSender) &&
            descriptor.KeyedImplementationType == implementationType;
    }

    [Fact]
    public void AddInfrastructure_should_fail_when_connection_string_is_missing()
    {
        var configuration = BuildConfiguration(("ConnectionStrings:MangaTrackerDb", ""));

        var act = () => new ServiceCollection().AddInfrastructure(configuration, Environment(Environments.Production));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MangaTrackerDb*");
    }

    [Fact]
    public void Valid_production_configuration_should_pass_all_option_validations()
    {
        using var provider = BuildProvider(BuildConfiguration(), Environments.Production);

        var act = () =>
        {
            _ = provider.GetRequiredService<IOptions<JwtOptions>>().Value;
            _ = provider.GetRequiredService<IOptions<AuthLinkOptions>>().Value;
            _ = provider.GetRequiredService<IOptions<ComicVineOptions>>().Value;
            _ = provider.GetRequiredService<IOptions<SmtpEmailOptions>>().Value;
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void Comic_vine_client_should_build_with_a_valid_resilience_pipeline()
    {
        // Polly validates its options when the client is created (e.g. the circuit
        // breaker's sampling window must be at least twice the attempt timeout).
        using var provider = BuildProvider(BuildConfiguration(), Environments.Production);

        var act = () => provider.GetRequiredService<IComicVineClient>();

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("too-short-key")]
    [InlineData("")]
    public void Jwt_options_should_reject_keys_shorter_than_256_bits(string secretKey)
    {
        using var provider = BuildProvider(
            BuildConfiguration(("Jwt:SecretKey", secretKey)),
            Environments.Production);

        var act = () => provider.GetRequiredService<IOptions<JwtOptions>>().Value;

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*Jwt:SecretKey*");
    }

    [Fact]
    public void Comic_vine_options_should_require_an_api_key()
    {
        using var provider = BuildProvider(
            BuildConfiguration(("ComicVine:ApiKey", "")),
            Environments.Production);

        var act = () => provider.GetRequiredService<IOptions<ComicVineOptions>>().Value;

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*API key*");
    }

    [Fact]
    public void Comic_vine_options_should_require_https()
    {
        using var provider = BuildProvider(
            BuildConfiguration(("ComicVine:BaseUrl", "http://comicvine.gamespot.com/api/")),
            Environments.Production);

        var act = () => provider.GetRequiredService<IOptions<ComicVineOptions>>().Value;

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*HTTPS*");
    }

    [Fact]
    public void Auth_links_should_require_https_outside_development()
    {
        using var provider = BuildProvider(
            BuildConfiguration(("AuthLinks:FrontendBaseUrl", "http://example.com")),
            Environments.Production);

        var act = () => provider.GetRequiredService<IOptions<AuthLinkOptions>>().Value;

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*HTTPS*");
    }

    [Fact]
    public void Auth_links_should_allow_http_localhost_in_development()
    {
        using var provider = BuildProvider(
            BuildConfiguration(("AuthLinks:FrontendBaseUrl", "http://localhost:4200")),
            Environments.Development);

        var act = () => provider.GetRequiredService<IOptions<AuthLinkOptions>>().Value;

        act.Should().NotThrow();
    }

    private static ServiceProvider BuildProvider(IConfiguration configuration, string environmentName)
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));

        return services
            .AddInfrastructure(configuration, Environment(environmentName))
            .BuildServiceProvider();
    }

    private static IConfiguration BuildConfiguration(params (string Key, string Value)[] overrides)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:MangaTrackerDb"] = "Server=localhost;Database=Test;Trusted_Connection=True;",
            ["Jwt:Issuer"] = "MangaTracker",
            ["Jwt:Audience"] = "MangaTracker",
            ["Jwt:SecretKey"] = new string('k', 32),
            ["Jwt:ExpirationMinutes"] = "120",
            ["AuthLinks:FrontendBaseUrl"] = "https://manga-tracker.example.com",
            ["ComicVine:BaseUrl"] = "https://comicvine.gamespot.com/api/",
            ["ComicVine:ApiKey"] = "test-api-key",
            ["Smtp:Host"] = "smtp.example.com",
            ["Smtp:Port"] = "587",
            ["Smtp:FromEmail"] = "no-reply@example.com",
            ["Smtp:EnableSsl"] = "true"
        };

        foreach (var (key, value) in overrides)
        {
            values[key] = value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static IHostEnvironment Environment(string name)
    {
        return new TestHostEnvironment { EnvironmentName = name };
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "MangaTracker.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
