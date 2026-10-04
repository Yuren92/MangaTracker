using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MangaTracker.Tests.Integration.Infrastructure;

namespace MangaTracker.Tests.Integration;

// Collections are looked up by (user id, collection id). These tests check that knowing
// another user's collection id (IDOR) is not enough to read or change it.
[Collection(ApiCollection.Name)]
public sealed class CollectionAuthorizationTests
{
    private readonly MangaTrackerApiFactory _factory;

    public CollectionAuthorizationTests(MangaTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task A_user_should_not_see_another_users_collection()
    {
        var (owner, _) = await TestUsers.CreateSignedInAsync(_factory);
        var (intruder, _) = await TestUsers.CreateSignedInAsync(_factory);
        var collectionId = await ImportAsync(owner, FakeComicVineClient.OnePieceUrl);

        var detail = await intruder.GetAsync($"/api/collections/{collectionId}");
        var list = await intruder.GetFromJsonAsync<CollectionList>("/api/collections");

        detail.StatusCode.Should().Be(HttpStatusCode.NotFound);
        list!.Items.Should().NotContain(item => item.Id == collectionId);
    }

    [Fact]
    public async Task A_user_should_not_change_or_delete_another_users_collection()
    {
        var (owner, _) = await TestUsers.CreateSignedInAsync(_factory);
        var (intruder, _) = await TestUsers.CreateSignedInAsync(_factory);
        var collectionId = await ImportAsync(owner, FakeComicVineClient.OnePieceUrl);
        var tomeId = (await GetDetailAsync(owner, collectionId)).Tomes.First().TomeId;

        var markOne = await intruder.PostAsync($"/api/collections/{collectionId}/tomes/{tomeId}/owned", null);
        var markAll = await intruder.PostAsync($"/api/collections/{collectionId}/tomes/owned-all", null);
        var unmark = await intruder.DeleteAsync($"/api/collections/{collectionId}/tomes/{tomeId}/owned");
        var delete = await intruder.DeleteAsync($"/api/collections/{collectionId}");

        markOne.StatusCode.Should().Be(HttpStatusCode.NotFound);
        markAll.StatusCode.Should().Be(HttpStatusCode.NotFound);
        unmark.StatusCode.Should().Be(HttpStatusCode.NotFound);
        delete.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var ownerView = await GetDetailAsync(owner, collectionId);
        ownerView.OwnedTomes.Should().Be(0);
    }

    [Fact]
    public async Task Two_users_importing_the_same_volume_should_get_independent_collections()
    {
        var (first, _) = await TestUsers.CreateSignedInAsync(_factory);
        var (second, _) = await TestUsers.CreateSignedInAsync(_factory);

        var firstCollectionId = await ImportAsync(first, FakeComicVineClient.OnePieceUrl);
        var secondCollectionId = await ImportAsync(second, FakeComicVineClient.OnePieceUrl);

        var tomeId = (await GetDetailAsync(first, firstCollectionId)).Tomes.First().TomeId;
        (await first.PostAsync($"/api/collections/{firstCollectionId}/tomes/{tomeId}/owned", null))
            .EnsureSuccessStatusCode();

        firstCollectionId.Should().NotBe(secondCollectionId);
        (await GetDetailAsync(first, firstCollectionId)).OwnedTomes.Should().Be(1);
        (await GetDetailAsync(second, secondCollectionId)).OwnedTomes.Should().Be(0);
    }

    [Fact]
    public async Task Importing_the_same_volume_twice_should_reuse_the_collection()
    {
        var (user, _) = await TestUsers.CreateSignedInAsync(_factory);

        var first = await ImportAsync(user, FakeComicVineClient.BerserkUrl);
        var second = await ImportAsync(user, FakeComicVineClient.BerserkUrl);

        second.Should().Be(first);
        var detail = await GetDetailAsync(user, first);
        detail.TotalTomes.Should().Be(3);
    }

    [Fact]
    public async Task A_tome_from_another_edition_should_not_be_marked_in_a_collection()
    {
        var (user, _) = await TestUsers.CreateSignedInAsync(_factory);
        var onePiece = await ImportAsync(user, FakeComicVineClient.OnePieceUrl);
        var berserk = await ImportAsync(user, FakeComicVineClient.BerserkUrl);
        var berserkTome = (await GetDetailAsync(user, berserk)).Tomes.First().TomeId;

        var response = await user.PostAsync($"/api/collections/{onePiece}/tomes/{berserkTome}/owned", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<Guid> ImportAsync(HttpClient client, string apiDetailUrl)
    {
        var response = await client.PostAsJsonAsync(
            "/api/collections/import-comic-vine-volume",
            new { apiDetailUrl });

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ImportResponse>();
        result!.IsCompleted.Should().BeTrue();

        return result.UserCollectionId;
    }

    private static async Task<CollectionDetail> GetDetailAsync(HttpClient client, Guid collectionId)
    {
        return (await client.GetFromJsonAsync<CollectionDetail>($"/api/collections/{collectionId}"))!;
    }

    private sealed record ImportResponse(Guid UserCollectionId, bool IsCompleted);

    private sealed record CollectionList(IReadOnlyCollection<CollectionListItem> Items);

    private sealed record CollectionListItem(Guid Id);

    private sealed record CollectionDetail(Guid Id, int TotalTomes, int OwnedTomes, IReadOnlyCollection<TomeItem> Tomes);

    private sealed record TomeItem(Guid TomeId, bool IsOwned);
}
