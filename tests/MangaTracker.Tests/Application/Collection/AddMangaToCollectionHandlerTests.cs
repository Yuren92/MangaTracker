using MangaTracker.Application.Collection.AddMangaToCollection;
using MangaTracker.Domain.Common;
using MangaTracker.Tests.Fakes;

namespace MangaTracker.Tests.Application.Collection.AddMangaToCollection;

public class AddMangaToCollectionHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenMangaDoesNotExist_ShouldAddMangaToCollection()
    {
        // Arrange
        var repository = new FakeMangaCollectionRepository();
        var handler = new AddMangaToCollectionHandler(repository);

        var command = new AddMangaToCollectionCommand(
            UserId: Guid.NewGuid(),
            MalId: 2,
            Title: "Berserk",
            ImageUrl: "https://example.com/berserk.jpg",
            MalTotalVolumes: 0,
            CustomTotalVolumes: 42);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(2, result.MalId);
        Assert.Equal("Berserk", result.Title);
        Assert.Equal("https://example.com/berserk.jpg", result.ImageUrl);
        Assert.Equal(42, result.EffectiveTotalVolumes);
    }

    [Fact]
    public async Task HandleAsync_WhenMangaAlreadyExists_ShouldThrowDomainException()
    {
        // Arrange
        var repository = new FakeMangaCollectionRepository();
        var handler = new AddMangaToCollectionHandler(repository);

        var command = new AddMangaToCollectionCommand(
            UserId: Guid.NewGuid(),
            MalId: 2,
            Title: "Berserk",
            ImageUrl: null,
            MalTotalVolumes: 0,
            CustomTotalVolumes: 42);

        await handler.HandleAsync(command);

        // Act
        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(command));

        // Assert
        Assert.Equal("Manga is already in collection.", exception.Message);
    }
}