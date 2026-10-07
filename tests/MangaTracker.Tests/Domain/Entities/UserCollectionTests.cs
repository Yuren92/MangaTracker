using AwesomeAssertions;
using MangaTracker.Domain.Common;
using MangaTracker.Domain.Entities;

namespace MangaTracker.Tests.Domain.Entities;

public sealed class UserCollectionTests
{
    private static readonly Guid EditionId = Guid.NewGuid();

    [Fact]
    public void A_tome_of_the_collections_edition_can_be_marked_once()
    {
        var collection = new UserCollection(userId: Guid.NewGuid(), editionId: EditionId);
        var tome = NewTome(EditionId);

        collection.MarkTomeAsOwned(tome);
        collection.MarkTomeAsOwned(tome);

        collection.OwnedTomes.Should().ContainSingle(owned => owned.TomeId == tome.Id);
    }

    [Fact]
    public void A_tome_of_another_edition_should_be_rejected()
    {
        var collection = new UserCollection(userId: Guid.NewGuid(), editionId: EditionId);

        var act = () => collection.MarkTomeAsOwned(NewTome(Guid.NewGuid()));

        act.Should().Throw<DomainException>().WithMessage("Tome does not belong to this collection.");
        collection.OwnedTomes.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void A_tome_without_number_is_valid_and_has_no_sort_number(string? issueNumber)
    {
        var tome = new Tome(
            editionId: EditionId,
            comicVineIssueId: 1,
            comicVineApiDetailUrl: "https://comicvine.gamespot.com/api/issue/4000-1/",
            issueNumber: issueNumber!,
            normalizedNumber: null);

        tome.IssueNumber.Should().BeEmpty();
        tome.NormalizedNumber.Should().BeNull();
    }

    private static Tome NewTome(Guid editionId)
    {
        return new Tome(
            editionId: editionId,
            comicVineIssueId: Random.Shared.Next(1, 1_000_000),
            comicVineApiDetailUrl: $"https://comicvine.gamespot.com/api/issue/4000-{Guid.NewGuid():N}/",
            issueNumber: "1",
            normalizedNumber: 1);
    }
}
