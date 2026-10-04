using System.Net.Http.Json;
using FluentAssertions;
using MangaTracker.Tests.Integration.Infrastructure;

namespace MangaTracker.Tests.Integration;

[Collection(ApiCollection.Name)]
public sealed class OwnedTomesTests
{
    private readonly MangaTrackerApiFactory _factory;

    public OwnedTomesTests(MangaTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Marking_unmarking_and_marking_all_should_keep_the_counts_right()
    {
        var (client, _) = await TestUsers.CreateSignedInAsync(_factory);
        var import = await client.PostAsJsonAsync(
            "/api/collections/import-comic-vine-volume",
            new { apiDetailUrl = FakeComicVineClient.OnePieceUrl });
        var collectionId = (await import.Content.ReadFromJsonAsync<ImportResponse>())!.UserCollectionId;
        var tomeId = (await DetailAsync(client, collectionId)).Tomes.First().TomeId;

        // Marking twice is idempotent.
        (await client.PostAsync($"/api/collections/{collectionId}/tomes/{tomeId}/owned", null)).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/collections/{collectionId}/tomes/{tomeId}/owned", null)).EnsureSuccessStatusCode();
        (await DetailAsync(client, collectionId)).OwnedTomes.Should().Be(1);

        (await client.DeleteAsync($"/api/collections/{collectionId}/tomes/{tomeId}/owned")).EnsureSuccessStatusCode();
        var afterUnmark = await DetailAsync(client, collectionId);
        afterUnmark.OwnedTomes.Should().Be(0);
        afterUnmark.Tomes.Should().OnlyContain(tome => !tome.IsOwned);

        (await client.PostAsync($"/api/collections/{collectionId}/tomes/owned-all", null)).EnsureSuccessStatusCode();
        var afterAll = await DetailAsync(client, collectionId);
        afterAll.OwnedTomes.Should().Be(afterAll.TotalTomes);
        afterAll.PendingTomes.Should().Be(0);

        var pending = await client.GetFromJsonAsync<PendingList>("/api/collections/pending-tomes");
        pending!.Items.Should().NotContain(item => item.CollectionId == collectionId);
    }

    private static async Task<Detail> DetailAsync(HttpClient client, Guid collectionId) =>
        (await client.GetFromJsonAsync<Detail>($"/api/collections/{collectionId}"))!;

    private sealed record ImportResponse(Guid UserCollectionId);

    private sealed record Detail(int TotalTomes, int OwnedTomes, int PendingTomes, IReadOnlyCollection<DetailTome> Tomes);

    private sealed record DetailTome(Guid TomeId, bool IsOwned);

    private sealed record PendingList(IReadOnlyCollection<PendingItem> Items);

    private sealed record PendingItem(Guid CollectionId);
}
