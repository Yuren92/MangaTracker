using MangaTracker.Application.Abstractions;
using MangaTracker.Application.ComicVine.Dtos;
using MangaTracker.Application.Common.Exceptions;

namespace MangaTracker.Application.ComicVine.GetIssueDetailsPage;

public sealed class GetIssueDetailsPageHandler
{
    private const int DefaultLimit = 25;
    private const int MaxLimit = 50;

    private readonly IComicVineClient _comicVineClient;

    public GetIssueDetailsPageHandler(IComicVineClient comicVineClient)
    {
        _comicVineClient = comicVineClient;
    }

    public async Task<GetIssueDetailsPageResult> HandleAsync(
        GetIssueDetailsPageQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ApiDetailUrl))
        {
            throw new ValidationException("Comic Vine volume API detail URL is required.");
        }

        var offset = Math.Max(query.Offset, 0);
        var limit = NormalizeLimit(query.Limit);

        var volume = await _comicVineClient.GetVolumeByApiDetailUrlAsync(
            query.ApiDetailUrl,
            cancellationToken);

        if (volume is null)
        {
            throw new NotFoundException("Comic Vine volume was not found.");
        }

        var total = volume.Issues.Count;

        var issueSummaries = volume.Issues
            .OrderBy(issue => issue.NormalizedNumber ?? int.MaxValue)
            .ThenBy(issue => issue.IssueNumber, StringComparer.OrdinalIgnoreCase)
            .Skip(offset)
            .Take(limit)
            .ToList();

        var issueDetails = new List<ComicVineIssueDetailDto>();

        foreach (var issueSummary in issueSummaries)
        {
            var issueDetail = await _comicVineClient.GetIssueByApiDetailUrlAsync(
                issueSummary.ApiDetailUrl,
                cancellationToken);

            if (issueDetail is not null)
            {
                issueDetails.Add(issueDetail);
            }
        }

        var nextOffset = offset + issueDetails.Count;
        var isCompleted = nextOffset >= total;

        return new GetIssueDetailsPageResult(
            Total: total,
            Offset: offset,
            Limit: limit,
            Processed: issueDetails.Count,
            NextOffset: isCompleted ? null : nextOffset,
            IsCompleted: isCompleted,
            Items: issueDetails);
    }

    private static int NormalizeLimit(int limit)
    {
        if (limit <= 0)
        {
            return DefaultLimit;
        }

        return Math.Min(limit, MaxLimit);
    }
}

public sealed record GetIssueDetailsPageQuery(
    string ApiDetailUrl,
    int Offset,
    int Limit);

public sealed record GetIssueDetailsPageResult(
    int Total,
    int Offset,
    int Limit,
    int Processed,
    int? NextOffset,
    bool IsCompleted,
    IReadOnlyCollection<ComicVineIssueDetailDto> Items);