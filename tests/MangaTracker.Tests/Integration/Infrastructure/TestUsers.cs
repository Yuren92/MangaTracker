using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;

namespace MangaTracker.Tests.Integration.Infrastructure;

public static class TestUsers
{
    public const string Password = "Correct-Horse-1";

    public static string NewEmail() => $"user-{Guid.NewGuid():N}@manga-tracker.test";

    public static async Task RegisterAsync(HttpClient client, string email, string password = Password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        response.EnsureSuccessStatusCode();
    }

    public static async Task ConfirmAsync(HttpClient client, MangaTrackerApiFactory factory, string email)
    {
        var token = factory.Emails.LatestTokenFor(email, EmailKind.Confirmation);
        var response = await client.PostAsJsonAsync("/api/auth/confirm-email", new { token });
        response.EnsureSuccessStatusCode();
    }

    public static async Task<string> LoginAsync(HttpClient client, string email, string password = Password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();

        return body.AccessToken;
    }

    // Registers, confirms and logs in a new user; returns a client authenticated as them.
    public static async Task<(HttpClient Client, string Email)> CreateSignedInAsync(MangaTrackerApiFactory factory)
    {
        var email = NewEmail();
        var client = factory.CreateClient();

        await RegisterAsync(client, email);
        await ConfirmAsync(client, factory, email);
        var accessToken = await LoginAsync(client, email);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return (client, email);
    }

    private sealed record LoginResponse(string AccessToken);
}
