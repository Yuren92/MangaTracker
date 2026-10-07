using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;
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
    public async Task Registering_an_existing_email_should_look_identical_and_notify_the_owner()
    {
        var email = TestUsers.NewEmail();
        var newEmail = TestUsers.NewEmail();
        await TestUsers.RegisterAsync(_client, email);

        var existing = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = TestUsers.Password });
        var brandNew = await _client.PostAsJsonAsync("/api/auth/register", new { email = newEmail, password = TestUsers.Password });

        existing.StatusCode.Should().Be(HttpStatusCode.Accepted);
        brandNew.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await existing.Content.ReadAsStringAsync()).Should().Be(await brandNew.Content.ReadAsStringAsync());

        _factory.Emails.CountFor(email, EmailKind.Confirmation).Should().Be(1, "only the first registration creates the account");
        _factory.Emails.CountFor(email, EmailKind.ExistingAccountNotice).Should().Be(1);
        _factory.Emails.CountFor(newEmail, EmailKind.Confirmation).Should().Be(1);
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
        // Everything but the per-request traceId must match.
        var wrongPasswordProblem = await wrongPassword.Content.ReadFromJsonAsync<ProblemResponse>();
        var unknownEmailProblem = await unknownEmail.Content.ReadFromJsonAsync<ProblemResponse>();

        wrongPasswordProblem.Should().Be(unknownEmailProblem);
    }

    private sealed record ProblemResponse(string Title, int Status, string Detail);

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
    public async Task A_reset_link_used_by_concurrent_requests_should_work_only_once()
    {
        var email = TestUsers.NewEmail();
        await TestUsers.RegisterAsync(_client, email);
        await TestUsers.ConfirmAsync(_client, _factory, email);

        await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        var token = _factory.Emails.LatestTokenFor(email, EmailKind.PasswordReset);

        // All requests read the token as unused before any of them saves; without the
        // concurrency check on UsedAt, several would succeed with different passwords.
        var responses = await Task.WhenAll(Enumerable.Range(1, 5).Select(attempt =>
            _client.PostAsJsonAsync("/api/auth/reset-password", new { token, newPassword = $"Racing-Password-{attempt}" })));

        responses.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(1);
        responses.Where(response => response.StatusCode != HttpStatusCode.OK)
            .Should().AllSatisfy(response => response.StatusCode.Should().Be(HttpStatusCode.BadRequest));
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
    [Fact]
    public async Task Changing_the_password_should_revoke_existing_tokens_and_return_a_new_one()
    {
        var (client, email) = await TestUsers.CreateSignedInAsync(_factory);
        var otherSession = await TestUsers.LoginAsync(_client, email);

        var change = await client.PostAsJsonAsync(
            "/api/auth/change-password",
            new { currentPassword = TestUsers.Password, newPassword = "Changed-Password-2" });
        change.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await change.Content.ReadFromJsonAsync<ChangePasswordResponse>();

        // The token used for the request and any other session are revoked...
        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await GetMeAsync(otherSession)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // ...and the token returned by the change keeps the current session working.
        (await GetMeAsync(body!.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Resetting_the_password_should_revoke_existing_tokens()
    {
        var (client, email) = await TestUsers.CreateSignedInAsync(_factory);

        await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        var token = _factory.Emails.LatestTokenFor(email, EmailKind.PasswordReset);
        (await _client.PostAsJsonAsync("/api/auth/reset-password", new { token, newPassword = "New-Password-2" }))
            .EnsureSuccessStatusCode();

        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpResponseMessage> GetMeAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }

    private sealed record ChangePasswordResponse(string Message, string AccessToken);
}
