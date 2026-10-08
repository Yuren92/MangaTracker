using System.Net.Http.Json;
using AwesomeAssertions;
using MangaTracker.Application.Catalog.SyncCatalog;
using MangaTracker.Tests.Fakes;
using MangaTracker.Tests.Integration.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MangaTracker.Tests.Integration;

[Collection(ApiCollection.Name)]
public sealed class SyncTests
{
    private readonly MangaTrackerApiFactory _factory;

    public SyncTests(MangaTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task A_tome_published_in_comic_vine_should_reach_every_collector_after_the_catalog_sync()
    {
        var (user, _) = await TestUsers.CreateSignedInAsync(_factory);
        var import = await user.PostAsJsonAsync(
            "/api/collections/import-comic-vine-volume",
            new { apiDetailUrl = FakeComicVineClient.GrowingVolumeUrl });
        import.EnsureSuccessStatusCode();

        (await PendingCountAsync(user)).Should().Be(3);

        // Comic Vine publishes tome 54 of the volume.
        _factory.ComicVine.AddIssue(FakeComicVineClient.GrowingVolumeUrl, ComicVineIssues.Summary(54));

        var first = await SyncCatalogAsync();
        first.Completed.Should().BeTrue();
        first.ChangedEditions.Should().BeGreaterThanOrEqualTo(1);
        (await PendingCountAsync(user)).Should().Be(4, "the new tome is missing from the user's shelf");

        // Nothing new since: the edition no longer counts as changed.
        var second = await SyncCatalogAsync();
        second.NewTomes.Should().Be(0);
        (await PendingCountAsync(user)).Should().Be(4);
    }

    private async Task<SyncCatalogResult> SyncCatalogAsync()
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SyncCatalogHandler>().HandleAsync();
    }

    private static async Task<int> PendingCountAsync(HttpClient client)
    {
        var pending = await client.GetFromJsonAsync<PendingResult>("/api/collections/pending-tomes");
        return pending!.Items.Count;
    }

    private sealed record PendingResult(IReadOnlyCollection<object> Items);
}
