using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MangaTracker.Tests.Integration.Infrastructure;

namespace MangaTracker.Tests.Integration;

[Collection(ApiCollection.Name)]
public sealed class ErrorHandlingTests
{
    private readonly MangaTrackerApiFactory _factory;

    public ErrorHandlingTests(MangaTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task An_unexpected_exception_should_be_a_generic_500_without_internal_details()
    {
        var (client, _) = await TestUsers.CreateSignedInAsync(_factory);

        var response = await client.PostAsJsonAsync(
            "/api/collections/import-comic-vine-volume",
            new { apiDetailUrl = FakeComicVineClient.BrokenVolumeUrl });

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("An unexpected error occurred.");
        body.Should().NotContain(FakeComicVineClient.SecretInErrorMessage)
            .And.NotContain("InvalidOperationException")
            .And.NotContain("   at ", "no stack trace");
    }
}
