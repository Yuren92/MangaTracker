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

    [Fact]
    public async Task SearchVolumesAsync_should_map_results_and_skip_those_without_an_api_url()
    {
        const string json = """
            { "results": [
              { "id": 1, "name": "Berserk", "publisher": { "name": "Dark Horse" }, "count_of_issues": 41,
                "start_year": "2003", "deck": "Dark fantasy.",
                "image": { "thumb_url": "https://img/thumb.jpg", "original_url": "https://img/original.jpg" },
                "api_detail_url": "https://comicvine.gamespot.com/api/volume/4050-1/" },
              { "id": 2, "name": "No API url", "start_year": "n/a" }
            ] }
            """;
        var handler = new RecordingHandler(_ => Json(json));

        var results = await CreateClient(handler).SearchVolumesAsync("  berserk  ", limit: 500);

        var volume = results.Should().ContainSingle().Subject;
        volume.ComicVineVolumeId.Should().Be(1);
        volume.PublisherName.Should().Be("Dark Horse");
        volume.CountOfIssues.Should().Be(41);
        volume.StartYear.Should().Be(2003);
        volume.ImageUrl.Should().Be("https://img/original.jpg", "the largest image wins");

        var query = handler.Requests.Single().Query;
        query.Should().Contain("query=berserk&").And.Contain("limit=50").And.Contain("resources=volume");
    }

    [Fact]
    public async Task SearchVolumesAsync_should_not_call_comic_vine_for_a_blank_query()
    {
        var handler = new RecordingHandler();

        var results = await CreateClient(handler).SearchVolumesAsync("   ");

        results.Should().BeEmpty();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task GetIssueByApiDetailUrlAsync_should_map_numbers_dates_and_image_fallbacks()
    {
        const string json = """
            { "results": { "id": 777, "issue_number": "12.5", "name": "Special",
              "cover_date": "2004-02-01", "store_date": "not a date",
              "image": { "medium_url": "https://img/medium.jpg", "tiny_url": "https://img/tiny.jpg" } } }
            """;
        const string issueUrl = "https://comicvine.gamespot.com/api/issue/4000-777/";

        var issue = await CreateClient(new RecordingHandler(_ => Json(json))).GetIssueByApiDetailUrlAsync(issueUrl);

        issue!.ComicVineIssueId.Should().Be(777);
        issue.IssueNumber.Should().Be("12.5");
        issue.NormalizedNumber.Should().BeNull("only whole numbers are used for ordering");
        issue.CoverDate.Should().Be(new DateOnly(2004, 2, 1));
        issue.StoreDate.Should().BeNull();
        issue.ImageUrl.Should().Be("https://img/medium.jpg");
        issue.ApiDetailUrl.Should().Be(issueUrl, "falls back to the requested URL when the response omits it");
    }

    [Fact]
    public async Task Object_not_found_should_be_null_even_though_comic_vine_answers_200()
    {
        // What Comic Vine really sends for an unknown id: HTTP 200, status_code 101 and
        // results as an empty array. It means "does not exist", not "provider down".
        var client = CreateClient(new RecordingHandler(_ =>
            Json("""{ "error": "Object Not Found", "status_code": 101, "results": [] }""")));

        (await client.GetVolumeByApiDetailUrlAsync(ValidUrl)).Should().BeNull();
        (await client.GetIssueByApiDetailUrlAsync("https://comicvine.gamespot.com/api/issue/4000-1/")).Should().BeNull();
    }

    [Theory]
    [InlineData("""{ "error": "Invalid API Key", "status_code": 100, "results": [] }""")]
    [InlineData("""{ "status_code": 107, "results": [] }""")]
    [InlineData("""{ "results": [] }""")]
    public async Task Other_comic_vine_errors_should_be_reported_as_unavailable(string json)
    {
        var client = CreateClient(new RecordingHandler(_ => Json(json)));

        var act = () => client.GetVolumeByApiDetailUrlAsync(ValidUrl);

        await act.Should().ThrowAsync<ExternalServiceUnavailableException>();
    }

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

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
