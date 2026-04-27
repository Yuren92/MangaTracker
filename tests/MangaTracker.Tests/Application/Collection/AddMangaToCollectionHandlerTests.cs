using MangaTracker.Application.Collection.AddMangaToCollection;
using MangaTracker.Application.Mal.Dtos;
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
        var malClient = new FakeMalMangaClient();
        var handler = new AddMangaToCollectionHandler(repository, malClient);
        var userId = Guid.NewGuid();

        malClient.AddManga(new MalMangaDetailDto(
            MalId: 2,
            Title: "Berserk",
            ImageUrl: "https://example.com/berserk.jpg",
            TotalVolumes: 0,
            TotalChapters: 0,
            Status: "currently_publishing",
            Synopsis: "Dark fantasy manga.",
            Recommendations: []));

        var command = new AddMangaToCollectionCommand(
            UserId: userId,
            MalId: 2,
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
    public async Task HandleAsync_WhenMangaAlreadyExistsForSameUser_ShouldThrowDomainException()
    {
        // Arrange
        var repository = new FakeMangaCollectionRepository();
        var malClient = new FakeMalMangaClient();
        var handler = new AddMangaToCollectionHandler(repository, malClient);
        var userId = Guid.NewGuid();

        malClient.AddManga(new MalMangaDetailDto(
            MalId: 2,
            Title: "Berserk",
            ImageUrl: null,
            TotalVolumes: 0,
            TotalChapters: 0,
            Status: "currently_publishing",
            Synopsis: null,
            Recommendations: []));

        var command = new AddMangaToCollectionCommand(
            UserId: userId,
            MalId: 2,
            CustomTotalVolumes: 42);

        await handler.HandleAsync(command);

        // Act
        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(command));

        // Assert
        Assert.Equal("Manga is already in collection.", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_WhenMangaDoesNotExistInMal_ShouldThrowDomainException()
    {
        // Arrange
        var repository = new FakeMangaCollectionRepository();
        var malClient = new FakeMalMangaClient();
        var handler = new AddMangaToCollectionHandler(repository, malClient);
        var userId = Guid.NewGuid();

        var command = new AddMangaToCollectionCommand(
            UserId: userId,
            MalId: 999999,
            CustomTotalVolumes: null);

        // Act
        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(command));

        // Assert
        Assert.Equal("Manga was not found in MyAnimeList.", exception.Message);
    }
}