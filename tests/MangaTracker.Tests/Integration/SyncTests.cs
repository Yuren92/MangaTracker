using System.Net.Http.Json;
using FluentAssertions;
using MangaTracker.Tests.Integration.Infrastructure;

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
    public async Task Sync_should_respect_the_cooldown_and_refresh_due_collections()
    {
        var (user, _) = await TestUsers.CreateSignedInAsync(_factory);
        var import = await user.PostAsJsonAsync(
            "/api/collections/import-comic-vine-volume",
            new { apiDetailUrl = FakeComicVineClient.BerserkUrl });
        import.EnsureSuccessStatusCode();

        // Just imported: inside the 24 hour cooldown, Comic Vine is not called.
        var fresh = await SyncAsync(user);
        fresh.SkippedCollections.Should().Be(1);
        fresh.SyncedCollections.Should().Be(0);

        using (_factory.Clock.Advance(TimeSpan.FromHours(25)))
        {
            var due = await SyncAsync(user);

            due.SyncedCollections.Should().Be(1);
            due.NewTomes.Should().Be(0, "every issue of the volume is already stored");

            var again = await SyncAsync(user);
            again.SkippedCollections.Should().Be(1, "the sync above restarted the cooldown");
        }
    }

    private static async Task<SyncResult> SyncAsync(HttpClient client)
    {
        var response = await client.PostAsync("/api/collections/sync", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SyncResult>())!;
    }

    private sealed record SyncResult(int SyncedCollections, int SkippedCollections, int NewTomes);
}
