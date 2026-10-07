using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using MangaTracker.Tests.Integration.Infrastructure;

namespace MangaTracker.Tests.Integration;

[Collection(ApiCollection.Name)]
public sealed class TokenExpiryTests
{
    private readonly MangaTrackerApiFactory _factory;
    private readonly HttpClient _client;

    public TokenExpiryTests(MangaTrackerApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Email_confirmation_link_should_expire_after_24_hours()
    {
        var email = TestUsers.NewEmail();
        await TestUsers.RegisterAsync(_client, email);
        var token = _factory.Emails.LatestTokenFor(email, EmailKind.Confirmation);

        using (_factory.Clock.Advance(TimeSpan.FromHours(24) + TimeSpan.FromMinutes(1)))
        {
            var response = await _client.PostAsJsonAsync("/api/auth/confirm-email", new { token });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).Should().Contain("invalid or expired");
        }
    }

    [Fact]
    public async Task Password_reset_link_should_expire_after_one_hour()
    {
        var email = TestUsers.NewEmail();
        await TestUsers.RegisterAsync(_client, email);
        await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        var token = _factory.Emails.LatestTokenFor(email, EmailKind.PasswordReset);

        using (_factory.Clock.Advance(TimeSpan.FromMinutes(61)))
        {
            var response = await _client.PostAsJsonAsync(
                "/api/auth/reset-password",
                new { token, newPassword = "New-Password-2" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task Password_reset_link_should_still_work_just_before_expiring()
    {
        var email = TestUsers.NewEmail();
        await TestUsers.RegisterAsync(_client, email);
        await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        var token = _factory.Emails.LatestTokenFor(email, EmailKind.PasswordReset);

        using (_factory.Clock.Advance(TimeSpan.FromMinutes(59)))
        {
            var response = await _client.PostAsJsonAsync(
                "/api/auth/reset-password",
                new { token, newPassword = "New-Password-2" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
