using FluentAssertions;
using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Collections.ImportComicVineVolume;
using MangaTracker.Application.ComicVine.Dtos;
using MangaTracker.Application.Common.Exceptions;
using MangaTracker.Domain.Entities;
using NSubstitute;

namespace MangaTracker.Tests.Application.Collections.ImportComicVineVolume;

public sealed class ImportComicVineVolumeHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldThrowValidationException_WhenUserIdIsEmpty()
    {
        // Arrange
        var handler = CreateHandler();

        var command = new ImportComicVineVolumeCommand(
            UserId: Guid.Empty,
            ApiDetailUrl: "https://comicvine.gamespot.com/api/volume/4050-21397/");

        // Act
        var act = async () => await handler.HandleAsync(command);

        // Assert
        await act.Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("User id is required.");
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowValidationException_WhenApiDetailUrlIsEmpty()
    {
        // Arrange
        var handler = CreateHandler();

        var command = new ImportComicVineVolumeCommand(
            UserId: Guid.NewGuid(),
            ApiDetailUrl: " ");

        // Act
        var act = async () => await handler.HandleAsync(command);

        // Assert
        await act.Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("Comic Vine volume API detail URL is required.");
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowNotFoundException_WhenComicVineVolumeDoesNotExist()
    {
        // Arrange
        var apiDetailUrl = "https://comicvine.gamespot.com/api/volume/4050-21397/";

        var comicVineClient = Substitute.For<IComicVineClient>();
        var editionRepository = Substitute.For<IEditionRepository>();

        editionRepository
            .GetByComicVineApiDetailUrlWithTomesAsync(apiDetailUrl, Arg.Any<CancellationToken>())
            .Returns((Edition?)null);

        comicVineClient
            .GetVolumeByApiDetailUrlAsync(apiDetailUrl, Arg.Any<CancellationToken>())
            .Returns((ComicVineVolumeDetailDto?)null);

        var handler = CreateHandler(
            comicVineClient: comicVineClient,
            editionRepository: editionRepository);

        var command = new ImportComicVineVolumeCommand(
            UserId: Guid.NewGuid(),
            ApiDetailUrl: apiDetailUrl);

        // Act
        var act = async () => await handler.HandleAsync(command);

        // Assert
        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Comic Vine volume was not found.");
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowValidationException_WhenVolumeHasTooManyIssues()
    {
        // Arrange
        var apiDetailUrl = "https://comicvine.gamespot.com/api/volume/4050-21397/";

        var comicVineClient = Substitute.For<IComicVineClient>();
        var editionRepository = Substitute.For<IEditionRepository>();

        editionRepository
            .GetByComicVineApiDetailUrlWithTomesAsync(apiDetailUrl, Arg.Any<CancellationToken>())
            .Returns((Edition?)null);

        var volume = CreateVolumeDetail(
            apiDetailUrl: apiDetailUrl,
            issues: CreateIssueSummaries(count: 251));

        comicVineClient
            .GetVolumeByApiDetailUrlAsync(apiDetailUrl, Arg.Any<CancellationToken>())
            .Returns(volume);

        var handler = CreateHandler(
            comicVineClient: comicVineClient,
            editionRepository: editionRepository);

        var command = new ImportComicVineVolumeCommand(
            UserId: Guid.NewGuid(),
            ApiDetailUrl: apiDetailUrl);

        // Act
        var act = async () => await handler.HandleAsync(command);

        // Assert
        await act.Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("This volume has too many issues to import at once. Maximum allowed is 250.");
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateSeriesEditionAndUserCollection_WhenVolumeIsValidAndDoesNotExist()
    {
        // Arrange
        var apiDetailUrl = "https://comicvine.gamespot.com/api/volume/4050-21397/";
        var userId = Guid.NewGuid();

        var comicVineClient = Substitute.For<IComicVineClient>();
        var seriesRepository = Substitute.For<ISeriesRepository>();
        var editionRepository = Substitute.For<IEditionRepository>();
        var tomeRepository = Substitute.For<ITomeRepository>();
        var userCollectionRepository = Substitute.For<IUserCollectionRepository>();

        editionRepository
            .GetByComicVineApiDetailUrlWithTomesAsync(apiDetailUrl, Arg.Any<CancellationToken>())
            .Returns((Edition?)null);

        seriesRepository
            .GetByTitleAsync("One Piece", Arg.Any<CancellationToken>())
            .Returns((Series?)null);

        editionRepository
            .GetByComicVineApiDetailUrlAsync(apiDetailUrl, Arg.Any<CancellationToken>())
            .Returns((Edition?)null);

        userCollectionRepository
            .GetByUserIdAndEditionIdAsync(userId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((UserCollection?)null);

        tomeRepository
            .GetByEditionIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Tome>());

        var volume = CreateVolumeDetail(
            apiDetailUrl: apiDetailUrl,
            issues: []);

        comicVineClient
            .GetVolumeByApiDetailUrlAsync(apiDetailUrl, Arg.Any<CancellationToken>())
            .Returns(volume);

        var handler = CreateHandler(
            comicVineClient: comicVineClient,
            seriesRepository: seriesRepository,
            editionRepository: editionRepository,
            tomeRepository: tomeRepository,
            userCollectionRepository: userCollectionRepository);

        var command = new ImportComicVineVolumeCommand(
            UserId: userId,
            ApiDetailUrl: apiDetailUrl);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.ComicVineVolumeId.Should().Be(21397);
        result.Title.Should().Be("One Piece");
        result.PublisherName.Should().Be("Shueisha");
        result.TotalIssues.Should().Be(0);
        result.ImportedTomes.Should().Be(0);
        result.IsCompleted.Should().BeTrue();

        await seriesRepository
            .Received(1)
            .AddAsync(Arg.Any<Series>(), Arg.Any<CancellationToken>());

        await editionRepository
            .Received(1)
            .AddAsync(Arg.Any<Edition>(), Arg.Any<CancellationToken>());

        await userCollectionRepository
            .Received(1)
            .AddAsync(Arg.Any<UserCollection>(), Arg.Any<CancellationToken>());

        await userCollectionRepository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static ImportComicVineVolumeHandler CreateHandler(
        IComicVineClient? comicVineClient = null,
        ISeriesRepository? seriesRepository = null,
        IEditionRepository? editionRepository = null,
        ITomeRepository? tomeRepository = null,
        IUserCollectionRepository? userCollectionRepository = null)
    {
        return new ImportComicVineVolumeHandler(
            comicVineClient ?? Substitute.For<IComicVineClient>(),
            seriesRepository ?? Substitute.For<ISeriesRepository>(),
            editionRepository ?? Substitute.For<IEditionRepository>(),
            tomeRepository ?? Substitute.For<ITomeRepository>(),
            userCollectionRepository ?? Substitute.For<IUserCollectionRepository>());
    }

    private static ComicVineVolumeDetailDto CreateVolumeDetail(
        string apiDetailUrl,
        IReadOnlyCollection<ComicVineIssueSummaryDto> issues)
    {
        return new ComicVineVolumeDetailDto(
            ComicVineVolumeId: 21397,
            Name: "One Piece",
            PublisherName: "Shueisha",
            CountOfIssues: issues.Count,
            ImageUrl: "https://comicvine.gamespot.com/one-piece.jpg",
            StartYear: 1997,
            Description: "Japanese manga series.",
            SiteDetailUrl: "https://comicvine.gamespot.com/one-piece/4050-21397/",
            ApiDetailUrl: apiDetailUrl,
            Issues: issues);
    }

    private static IReadOnlyCollection<ComicVineIssueSummaryDto> CreateIssueSummaries(int count)
    {
        return Enumerable
            .Range(1, count)
            .Select(number => new ComicVineIssueSummaryDto(
                ComicVineIssueId: 100000 + number,
                IssueNumber: number.ToString(),
                NormalizedNumber: number,
                Title: $"Volume {number}",
                SiteDetailUrl: $"https://comicvine.gamespot.com/one-piece-{number}/4000-{100000 + number}/",
                ApiDetailUrl: $"https://comicvine.gamespot.com/api/issue/4000-{100000 + number}/"))
            .ToList();
    }
}