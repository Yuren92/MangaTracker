using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MangaTracker.Tests.Integration.Infrastructure;

namespace MangaTracker.Tests.Integration;

[Collection(ApiCollection.Name)]
public sealed class AuthFlowTests
{
    private readonly MangaTrackerApiFactory _factory;
    private readonly HttpClient _client;

    public AuthFlowTests(MangaTrackerApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_confirm_and_login_should_give_access_to_the_current_user()
    {
        var email = TestUsers.NewEmail();

        await TestUsers.RegisterAsync(_client, email);
        await TestUsers.ConfirmAsync(_client, _factory, email);
        var accessToken = await TestUsers.LoginAsync(_client, email);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain(email);
    }

    [Fact]
    public async Task Login_should_be_rejected_until_the_email_is_confirmed()
    {
        var email = TestUsers.NewEmail();
        await TestUsers.RegisterAsync(_client, email);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = TestUsers.Password });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Email is not confirmed");
    }

    [Fact]
    public async Task Confirmation_token_should_only_work_once()
    {
        var email = TestUsers.NewEmail();
        await TestUsers.RegisterAsync(_client, email);
        var token = _factory.Emails.LatestTokenFor(email, EmailKind.Confirmation);

        var first = await _client.PostAsJsonAsync("/api/auth/confirm-email", new { token });
        var second = await _client.PostAsJsonAsync("/api/auth/confirm-email", new { token });

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Registering_an_existing_email_should_return_conflict()
    {
        var email = TestUsers.NewEmail();
        await TestUsers.RegisterAsync(_client, email);

        var response = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = TestUsers.Password });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_with_a_wrong_password_should_return_the_same_error_as_an_unknown_email()
    {
        var email = TestUsers.NewEmail();
        await TestUsers.RegisterAsync(_client, email);
        await TestUsers.ConfirmAsync(_client, _factory, email);

        var wrongPassword = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Wrong-Password-1" });
        var unknownEmail = await _client.PostAsJsonAsync("/api/auth/login", new { email = TestUsers.NewEmail(), password = "Wrong-Password-1" });

        wrongPassword.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        unknownEmail.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await wrongPassword.Content.ReadAsStringAsync())
            .Should().Be(await unknownEmail.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Forgot_password_should_not_reveal_whether_an_email_is_registered()
    {
        var email = TestUsers.NewEmail();
        await TestUsers.RegisterAsync(_client, email);
        var unknownEmail = TestUsers.NewEmail();

        var known = await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        var unknown = await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email = unknownEmail });

        known.StatusCode.Should().Be(HttpStatusCode.OK);
        unknown.StatusCode.Should().Be(HttpStatusCode.OK);
        (await known.Content.ReadAsStringAsync()).Should().Be(await unknown.Content.ReadAsStringAsync());

        _factory.Emails.CountFor(email, EmailKind.PasswordReset).Should().Be(1);
        _factory.Emails.CountFor(unknownEmail, EmailKind.PasswordReset).Should().Be(0);
    }

    [Fact]
    public async Task Reset_password_should_replace_the_password_and_the_token_should_only_work_once()
    {
        var email = TestUsers.NewEmail();
        await TestUsers.RegisterAsync(_client, email);
        await TestUsers.ConfirmAsync(_client, _factory, email);

        await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        var token = _factory.Emails.LatestTokenFor(email, EmailKind.PasswordReset);
        const string newPassword = "New-Password-2";

        var reset = await _client.PostAsJsonAsync("/api/auth/reset-password", new { token, newPassword });
        var reuse = await _client.PostAsJsonAsync("/api/auth/reset-password", new { token, newPassword = "Another-Password-3" });

        reset.StatusCode.Should().Be(HttpStatusCode.OK);
        reuse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var oldPasswordLogin = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = TestUsers.Password });
        oldPasswordLogin.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await TestUsers.LoginAsync(_client, email, newPassword);
    }

    [Fact]
    public async Task Requesting_a_new_reset_link_should_invalidate_the_previous_one()
    {
        var email = TestUsers.NewEmail();
        await TestUsers.RegisterAsync(_client, email);

        await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        var firstToken = _factory.Emails.LatestTokenFor(email, EmailKind.PasswordReset);
        await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email });

        var response = await _client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new { token = firstToken, newPassword = "New-Password-2" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Changing_the_password_should_invalidate_pending_reset_links()
    {
        var (client, email) = await TestUsers.CreateSignedInAsync(_factory);

        await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        var resetToken = _factory.Emails.LatestTokenFor(email, EmailKind.PasswordReset);

        var change = await client.PostAsJsonAsync(
            "/api/auth/change-password",
            new { currentPassword = TestUsers.Password, newPassword = "Changed-Password-2" });
        var reset = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new { token = resetToken, newPassword = "Attacker-Password-3" });

        change.StatusCode.Should().Be(HttpStatusCode.OK);
        reset.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-jwt")]
    [InlineData("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJ4In0.c2lnbmF0dXJl")]
    public async Task Protected_endpoints_should_reject_missing_or_invalid_tokens(string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/collections");

        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
