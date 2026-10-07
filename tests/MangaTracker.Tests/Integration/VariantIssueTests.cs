using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using MangaTracker.Tests.Integration.Infrastructure;

namespace MangaTracker.Tests.Integration;

[Collection(ApiCollection.Name)]
public sealed class VariantIssueTests
{
    private readonly MangaTrackerApiFactory _factory;

    public VariantIssueTests(MangaTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task A_volume_with_two_issues_sharing_a_number_should_import_both()
    {
        // Comic Vine lists variant covers as separate issues with the same number.
        // Each has its own Comic Vine id, which is what identifies a tome.
        var (client, _) = await TestUsers.CreateSignedInAsync(_factory);

        var response = await client.PostAsJsonAsync(
            "/api/collections/import-comic-vine-volume",
            new { apiDetailUrl = FakeComicVineClient.VariantVolumeUrl });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var result = await response.Content.ReadFromJsonAsync<ImportResult>();
        result!.TotalIssues.Should().Be(4);
        result.ImportedTomes.Should().Be(4);
        result.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task A_volume_with_an_issue_without_number_should_import_it()
    {
        // Specials and one-shots can come from Comic Vine with no issue number.
        var (client, _) = await TestUsers.CreateSignedInAsync(_factory);

        var response = await client.PostAsJsonAsync(
            "/api/collections/import-comic-vine-volume",
            new { apiDetailUrl = FakeComicVineClient.UnnumberedVolumeUrl });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var result = await response.Content.ReadFromJsonAsync<ImportResult>();
        result!.ImportedTomes.Should().Be(3);
        result.IsCompleted.Should().BeTrue();
    }

    private sealed record ImportResult(int TotalIssues, int ImportedTomes, bool IsCompleted);
}
