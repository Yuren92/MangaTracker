using MangaTracker.Application.ComicVine.Dtos;

namespace MangaTracker.Tests.Fakes;

public static class ComicVineIssues
{
    public static int Id(int number) => 100000 + number;

    public static string Url(int number) => $"https://comicvine.gamespot.com/api/issue/4000-{Id(number)}/";

    public static ComicVineIssueDetailDto Detail(int number)
    {
        return new ComicVineIssueDetailDto(
            ComicVineIssueId: Id(number),
            IssueNumber: number.ToString(),
            NormalizedNumber: number,
            Title: $"Volume {number}",
            ImageUrl: null,
            CoverDate: null,
            StoreDate: null,
            SiteDetailUrl: null,
            ApiDetailUrl: Url(number));
    }

    public static ComicVineIssueSummaryDto Summary(int number)
    {
        return new ComicVineIssueSummaryDto(
            ComicVineIssueId: Id(number),
            IssueNumber: number.ToString(),
            NormalizedNumber: number,
            Title: $"Volume {number}",
            SiteDetailUrl: null,
            ApiDetailUrl: Url(number));
    }
}
