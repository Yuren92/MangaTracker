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

    [Fact]
    public async Task GetVolumeByApiDetailUrlAsync_should_return_null_when_comic_vine_answers_404()
    {
        var client = CreateClient(new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        var volume = await client.GetVolumeByApiDetailUrlAsync(ValidUrl);

        volume.Should().BeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task GetVolumeByApiDetailUrlAsync_should_report_provider_failures_as_unavailable(HttpStatusCode status)
    {
        var client = CreateClient(new RecordingHandler(_ => new HttpResponseMessage(status)));

        var act = () => client.GetVolumeByApiDetailUrlAsync(ValidUrl);

        await act.Should().ThrowAsync<ExternalServiceUnavailableException>();
    }

    [Fact]
    public async Task GetVolumeByApiDetailUrlAsync_should_report_a_timeout_as_unavailable()
    {
        // HttpClient signals its own timeout with TaskCanceledException, which must not be
        // confused with the caller cancelling the request.
        var client = CreateClient(new RecordingHandler(_ => throw new TaskCanceledException("timeout")));

        var act = () => client.GetVolumeByApiDetailUrlAsync(ValidUrl);

        await act.Should().ThrowAsync<ExternalServiceUnavailableException>();
    }

    [Fact]
    public async Task GetVolumeByApiDetailUrlAsync_should_propagate_cancellation_by_the_caller()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var client = CreateClient(new RecordingHandler());

        var act = () => client.GetVolumeByApiDetailUrlAsync(ValidUrl, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private const string ValidUrl = "https://comicvine.gamespot.com/api/volume/4050-1/";

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
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage>? respond = null)
        {
            _respond = respond ?? (_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{ "results": { "id": 1, "name": "One Piece", "issues": [] } }""",
                    Encoding.UTF8,
                    "application/json")
            });
        }

        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request.RequestUri!);

            return Task.FromResult(_respond(request));
        }
    }
}
