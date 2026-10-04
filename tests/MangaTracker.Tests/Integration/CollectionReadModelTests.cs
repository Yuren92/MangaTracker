using System.Net.Http.Json;
using FluentAssertions;
using MangaTracker.Tests.Integration.Infrastructure;

namespace MangaTracker.Tests.Integration;

// The list and pending screens are SQL projections (ICollectionQueries); these run them
// against SQL Server to check both the translation and the numbers.
[Collection(ApiCollection.Name)]
public sealed class CollectionReadModelTests
{
    private readonly MangaTrackerApiFactory _factory;

    public CollectionReadModelTests(MangaTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Collection_list_and_pending_tomes_should_reflect_owned_tomes_per_user()
    {
        var (user, _) = await TestUsers.CreateSignedInAsync(_factory);
        var (otherUser, _) = await TestUsers.CreateSignedInAsync(_factory);

        var onePiece = await ImportAsync(user, FakeComicVineClient.OnePieceUrl);
        await ImportAsync(user, FakeComicVineClient.BerserkUrl);
        await ImportAsync(otherUser, FakeComicVineClient.OnePieceUrl);

        var detail = await user.GetFromJsonAsync<Detail>($"/api/collections/{onePiece}");
        var ownedTome = detail!.Tomes.First().TomeId;
        (await user.PostAsync($"/api/collections/{onePiece}/tomes/{ownedTome}/owned", null)).EnsureSuccessStatusCode();

        var list = await user.GetFromJsonAsync<CollectionList>("/api/collections");
        var pending = await user.GetFromJsonAsync<PendingList>("/api/collections/pending-tomes");

        list!.Items.Select(item => item.Title).Should().Equal("Berserk", "One Piece");
        var onePieceSummary = list.Items.Single(item => item.Id == onePiece);
        onePieceSummary.TotalTomes.Should().Be(3);
        onePieceSummary.OwnedTomes.Should().Be(1);
        onePieceSummary.PendingTomes.Should().Be(2);

        pending!.Items.Should().HaveCount(5);
        pending.Items.Should().NotContain(item => item.TomeId == ownedTome);
        pending.Items.Select(item => item.SeriesTitle).Should().BeInAscendingOrder();
        pending.Items.Where(item => item.SeriesTitle == "One Piece")
            .Select(item => item.NormalizedNumber).Should().BeInAscendingOrder();
    }

    private static async Task<Guid> ImportAsync(HttpClient client, string apiDetailUrl)
    {
        var response = await client.PostAsJsonAsync("/api/collections/import-comic-vine-volume", new { apiDetailUrl });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ImportResponse>())!.UserCollectionId;
    }

    private sealed record ImportResponse(Guid UserCollectionId);

    private sealed record Detail(IReadOnlyCollection<DetailTome> Tomes);

    private sealed record DetailTome(Guid TomeId);

    private sealed record CollectionList(IReadOnlyCollection<CollectionItem> Items);

    private sealed record CollectionItem(Guid Id, string Title, int TotalTomes, int OwnedTomes, int PendingTomes);

    private sealed record PendingList(IReadOnlyCollection<PendingItem> Items);

    private sealed record PendingItem(Guid TomeId, string SeriesTitle, int? NormalizedNumber);
}
