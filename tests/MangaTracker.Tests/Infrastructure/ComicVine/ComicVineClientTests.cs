using System.Net;
using System.Text;
using FluentAssertions;
using MangaTracker.Application.Common.Exceptions;
using MangaTracker.Infrastructure.ExternalServices.ComicVine;
using Microsoft.Extensions.Options;

namespace MangaTracker.Tests.Infrastructure.ComicVine;

public sealed class ComicVineClientTests
{
    private const string ApiKey = "test-api-key";

    [Theory]
    [InlineData("http://comicvine.gamespot.com/api/volume/4050-1/")]
    [InlineData("https://evil.example.com/api/volume/4050-1/")]
    [InlineData("https://comicvine.gamespot.com.evil.example.com/api/volume/4050-1/")]
    [InlineData("https://comicvine.gamespot.com:8443/api/volume/4050-1/")]
    [InlineData("https://user:pass@comicvine.gamespot.com/api/volume/4050-1/")]
    [InlineData("https://comicvine.gamespot.com/volume/4050-1/")]
    [InlineData("https://comicvine.gamespot.com/api/../admin/")]
    [InlineData("not a url")]
    public async Task GetVolumeByApiDetailUrlAsync_should_reject_urls_outside_the_comic_vine_api(string apiDetailUrl)
    {
        var handler = new RecordingHandler();
        var client = CreateClient(handler);

        var act = () => client.GetVolumeByApiDetailUrlAsync(apiDetailUrl);

        await act.Should().ThrowAsync<ValidationException>();
        handler.Requests.Should().BeEmpty("the API key must never be sent to an unvalidated URL");
    }

    [Fact]
    public async Task GetVolumeByApiDetailUrlAsync_should_call_comic_vine_over_https_with_the_api_key()
    {
        var handler = new RecordingHandler();
        var client = CreateClient(handler);

        await client.GetVolumeByApiDetailUrlAsync(
            "https://comicvine.gamespot.com/api/volume/4050-1/?field_list=id#fragment");

        var request = handler.Requests.Should().ContainSingle().Subject;

        request.Scheme.Should().Be(Uri.UriSchemeHttps);
        request.Host.Should().Be("comicvine.gamespot.com");
        request.AbsolutePath.Should().Be("/api/volume/4050-1/");
        request.Query.Should().Be($"?field_list=id&api_key={ApiKey}&format=json");
        request.Fragment.Should().BeEmpty();
    }

    private static ComicVineClient CreateClient(RecordingHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://comicvine.gamespot.com/api/")
        };

        return new ComicVineClient(
            httpClient,
            Options.Create(new ComicVineOptions { ApiKey = ApiKey }));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{ "results": { "id": 1, "name": "One Piece", "issues": [] } }""",
                    Encoding.UTF8,
                    "application/json")
            });
        }
    }
}
