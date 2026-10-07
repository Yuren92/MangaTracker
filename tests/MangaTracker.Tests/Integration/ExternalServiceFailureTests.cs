using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using MangaTracker.Tests.Integration.Infrastructure;

namespace MangaTracker.Tests.Integration;

[Collection(ApiCollection.Name)]
public sealed class ExternalServiceFailureTests
{
    private readonly MangaTrackerApiFactory _factory;

    public ExternalServiceFailureTests(MangaTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Comic_vine_outage_should_be_a_503_with_retry_after_and_no_internal_details()
    {
        var (client, _) = await TestUsers.CreateSignedInAsync(_factory);

        var response = await client.PostAsJsonAsync(
            "/api/collections/import-comic-vine-volume",
            new { apiDetailUrl = FakeComicVineClient.UnavailableVolumeUrl });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Headers.RetryAfter.Should().NotBeNull();

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("try again later").And.NotContain("HttpRequestException");
    }
}
