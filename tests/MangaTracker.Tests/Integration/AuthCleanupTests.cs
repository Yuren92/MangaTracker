using System.Net.Http.Json;
using AwesomeAssertions;
using MangaTracker.Infrastructure.BackgroundJobs;
using MangaTracker.Infrastructure.Persistence;
using MangaTracker.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MangaTracker.Tests.Integration;

// The cleanup job deletes data, so it is tested against SQL Server: what it removes,
// and above all what it must keep. Defaults: unconfirmed accounts go after 48 hours.
[Collection(ApiCollection.Name)]
public sealed class AuthCleanupTests
{
    private readonly MangaTrackerApiFactory _factory;
    private readonly HttpClient _client;

    public AuthCleanupTests(MangaTrackerApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Cleanup_should_remove_only_stale_unconfirmed_accounts_and_dead_tokens()
    {
        var unconfirmed = TestUsers.NewEmail();
        await TestUsers.RegisterAsync(_client, unconfirmed);

        var confirmed = TestUsers.NewEmail();
        await TestUsers.RegisterAsync(_client, confirmed);
        await TestUsers.ConfirmAsync(_client, _factory, confirmed);

        // 47 hours later: the unconfirmed account is still inside its 48 hour window.
        using (_factory.Clock.Advance(TimeSpan.FromHours(47)))
        {
            // A reset link requested now is still valid when the job runs.
            await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email = confirmed });

            await RunCleanupAsync();

            (await UserExistsAsync(unconfirmed)).Should().BeTrue("it is not 48 hours old yet");
            (await ActiveTokenCountAsync(confirmed)).Should().Be(1, "the fresh reset token must survive");
            (await TokenCountAsync(unconfirmed)).Should().Be(0, "its 24 hour confirmation token has expired");
            (await TokenCountAsync(confirmed)).Should().Be(1, "the used confirmation token is gone, the reset token stays");
        }

        using (_factory.Clock.Advance(TimeSpan.FromHours(49)))
        {
            await RunCleanupAsync();

            (await UserExistsAsync(unconfirmed)).Should().BeFalse("unconfirmed for more than 48 hours");
            (await UserExistsAsync(confirmed)).Should().BeTrue("confirmed accounts are never removed");
        }
    }

    [Fact]
    public async Task Cleanup_should_delete_a_stale_account_even_if_it_has_a_live_token()
    {
        var email = TestUsers.NewEmail();
        await TestUsers.RegisterAsync(_client, email);

        using (_factory.Clock.Advance(TimeSpan.FromHours(49)))
        {
            // A confirmation link resent just now is still valid; the FK cascade must
            // remove it together with the account instead of failing the whole run.
            await _client.PostAsJsonAsync("/api/auth/resend-confirmation-email", new { email });
            (await ActiveTokenCountAsync(email)).Should().Be(1);

            await RunCleanupAsync();

            (await UserExistsAsync(email)).Should().BeFalse();
        }
    }

    private async Task RunCleanupAsync()
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AuthCleanupJob>().RunAsync();
    }

    private async Task<bool> UserExistsAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<MangaTrackerDbContext>()
            .Users.AnyAsync(user => user.Email == email);
    }

    private async Task<int> TokenCountAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MangaTrackerDbContext>();
        return await db.UserTokens.CountAsync(token =>
            db.Users.Any(user => user.Id == token.UserId && user.Email == email));
    }

    private async Task<int> ActiveTokenCountAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MangaTrackerDbContext>();
        var now = _factory.Clock.GetUtcNow();
        return await db.UserTokens.CountAsync(token =>
            token.UsedAt == null && token.ExpiresAt > now &&
            db.Users.Any(user => user.Id == token.UserId && user.Email == email));
    }
}
