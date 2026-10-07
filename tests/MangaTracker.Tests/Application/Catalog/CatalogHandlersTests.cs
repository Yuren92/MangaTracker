using AwesomeAssertions;
using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Catalog.PreviewComicVineVolume;
using MangaTracker.Application.Catalog.SearchCatalog;
using MangaTracker.Application.ComicVine.Dtos;
using MangaTracker.Application.Common.Exceptions;
using NSubstitute;

namespace MangaTracker.Tests.Application.Catalog;

public sealed class CatalogHandlersTests
{
    private readonly IComicVineClient _comicVineClient = Substitute.For<IComicVineClient>();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Search_should_reject_an_empty_query_without_calling_comic_vine(string query)
    {
        var act = () => new SearchCatalogHandler(_comicVineClient).HandleAsync(new SearchCatalogQuery(query, 10));

        await act.Should().ThrowAsync<ValidationException>();
        await _comicVineClient.DidNotReceiveWithAnyArgs().SearchVolumesAsync(default!, default, default);
    }

    [Fact]
    public async Task Search_should_reject_overly_long_queries()
    {
        var act = () => new SearchCatalogHandler(_comicVineClient).HandleAsync(new SearchCatalogQuery(new string('a', 101), 10));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(500, 50)]
    public async Task Search_should_trim_the_query_and_clamp_the_limit(int requested, int expected)
    {
        await new SearchCatalogHandler(_comicVineClient).HandleAsync(new SearchCatalogQuery("  one piece  ", requested));

        await _comicVineClient.Received(1).SearchVolumesAsync("one piece", expected, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Preview_should_return_not_found_when_comic_vine_has_no_volume()
    {
        _comicVineClient
            .GetVolumeByApiDetailUrlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ComicVineVolumeDetailDto?)null);

        var act = () => new PreviewComicVineVolumeHandler(_comicVineClient)
            .HandleAsync(new PreviewComicVineVolumeQuery("https://comicvine.gamespot.com/api/volume/4050-1/"));

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
