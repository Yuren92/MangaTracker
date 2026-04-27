using MangaTracker.Domain.Common;
using MangaTracker.Domain.Entities;
using MangaTracker.Domain.ValueObjects;

namespace MangaTracker.Tests.Domain.Entities;

public class MangaCollectionItemTests
{
    [Fact]
    public void AddOwnedVolume_WithValidVolume_ShouldAddVolume()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        var manga = new MangaCollectionItem(
            userId: userId,
            malId: 2,
            title: "Berserk",
            imageUrl: null,
            malTotalVolumes: null,
            customTotalVolumes: 5);

        var volumeNumber = new VolumeNumber(1);

        // Act
        manga.AddOwnedVolume(volumeNumber);

        // Assert
        Assert.Single(manga.OwnedVolumes);
        Assert.Contains(manga.OwnedVolumes, volume =>
            volume.Number.Value == 1);
    }

    [Fact]
    public void AddOwnedVolume_WithDuplicatedVolume_ShouldThrowDomainException()
    {
        // Arrange

        Guid userId = Guid.NewGuid();
        var manga = new MangaCollectionItem(
            userId: userId, malId: 2,
            title: "Berserk",
            imageUrl: null,
            malTotalVolumes: null,
            customTotalVolumes: 5);

        var volumeNumber = new VolumeNumber(1);

        manga.AddOwnedVolume(volumeNumber);

        // Act
        var exception = Assert.Throws<DomainException>(() =>
            manga.AddOwnedVolume(volumeNumber));

        // Assert
        Assert.Equal("Volume is already owned.", exception.Message);
    }

    [Fact]
    public void AddOwnedVolume_WhenVolumeIsGreaterThanKnownTotal_ShouldThrowDomainException()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        var manga = new MangaCollectionItem(
            userId: userId, malId: 2,
            title: "Berserk",
            imageUrl: null,
            malTotalVolumes: null,
            customTotalVolumes: 5);

        var volumeNumber = new VolumeNumber(6);

        // Act
        var exception = Assert.Throws<DomainException>(() =>
            manga.AddOwnedVolume(volumeNumber));

        // Assert
        Assert.Equal("Volume number cannot be greater than total volumes.", exception.Message);
    }

    [Fact]
    public void AddOwnedVolume_WhenTotalIsUnknown_ShouldAllowAnyPositiveVolume()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        var manga = new MangaCollectionItem(
            userId: userId, malId: 2,
            title: "Berserk",
            imageUrl: null,
            malTotalVolumes: null,
            customTotalVolumes: null);

        var volumeNumber = new VolumeNumber(20);

        // Act
        manga.AddOwnedVolume(volumeNumber);

        // Assert
        Assert.Single(manga.OwnedVolumes);
        Assert.Contains(manga.OwnedVolumes, volume =>
            volume.Number.Value == 20);
    }

    [Fact]
    public void GetMissingVolumeNumbers_WhenTotalIsKnown_ShouldReturnMissingVolumesUntilTotal()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        var manga = new MangaCollectionItem(
            userId: userId, malId: 2,
            title: "Berserk",
            imageUrl: null,
            malTotalVolumes: null,
            customTotalVolumes: 5);

        manga.AddOwnedVolume(new VolumeNumber(1));
        manga.AddOwnedVolume(new VolumeNumber(2));
        manga.AddOwnedVolume(new VolumeNumber(4));

        // Act
        var missingVolumes = manga.GetMissingVolumeNumbers();

        // Assert
        Assert.Equal([3, 5], missingVolumes.Select(volume => volume.Value));
    }

    [Fact]
    public void GetMissingVolumeNumbers_WhenTotalIsUnknown_ShouldReturnOnlyGapsUntilHighestOwnedVolume()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        var manga = new MangaCollectionItem(
            userId: userId, malId: 2,
            title: "Berserk",
            imageUrl: null,
            malTotalVolumes: null,
            customTotalVolumes: null);

        manga.AddOwnedVolume(new VolumeNumber(1));
        manga.AddOwnedVolume(new VolumeNumber(2));
        manga.AddOwnedVolume(new VolumeNumber(4));
        manga.AddOwnedVolume(new VolumeNumber(5));

        // Act
        var missingVolumes = manga.GetMissingVolumeNumbers();

        // Assert
        Assert.Equal([3], missingVolumes.Select(volume => volume.Value));
    }

    [Fact]
    public void GetMissingVolumeNumbers_WhenNoVolumesAreOwned_ShouldReturnEmptyCollection()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        var manga = new MangaCollectionItem(
            userId: userId, malId: 2,
            title: "Berserk",
            imageUrl: null,
            malTotalVolumes: null,
            customTotalVolumes: 5);

        // Act
        var missingVolumes = manga.GetMissingVolumeNumbers();

        // Assert
        Assert.Empty(missingVolumes);
    }

    [Fact]
    public void RemoveOwnedVolume_WhenVolumeExists_ShouldRemoveVolume()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        var manga = new MangaCollectionItem(
            userId: userId, malId: 2,
            title: "Berserk",
            imageUrl: null,
            malTotalVolumes: null,
            customTotalVolumes: 5);

        manga.AddOwnedVolume(new VolumeNumber(1));

        // Act
        manga.RemoveOwnedVolume(new VolumeNumber(1));

        // Assert
        Assert.Empty(manga.OwnedVolumes);
    }

    [Fact]
    public void RemoveOwnedVolume_WhenVolumeDoesNotExist_ShouldThrowDomainException()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        var manga = new MangaCollectionItem(
            userId: userId, malId: 2,
            title: "Berserk",
            imageUrl: null,
            malTotalVolumes: null,
            customTotalVolumes: 5);

        // Act
        var exception = Assert.Throws<DomainException>(() =>
            manga.RemoveOwnedVolume(new VolumeNumber(1)));

        // Assert
        Assert.Equal("Volume is not owned.", exception.Message);
    }

    [Fact]
    public void SetCustomTotalVolumes_WithValidTotal_ShouldUpdateCustomTotalVolumes()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        var manga = new MangaCollectionItem(
            userId: userId, malId: 2,
            title: "Berserk",
            imageUrl: null,
            malTotalVolumes: null,
            customTotalVolumes: null);

        // Act
        manga.SetCustomTotalVolumes(5);

        // Assert
        Assert.Equal(5, manga.CustomTotalVolumes);
        Assert.Equal(5, manga.EffectiveTotalVolumes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetCustomTotalVolumes_WithNullZeroOrNegativeValue_ShouldSetTotalAsUnknown(int? totalVolumes)
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        var manga = new MangaCollectionItem(
            userId: userId, malId: 2,
            title: "Berserk",
            imageUrl: null,
            malTotalVolumes: null,
            customTotalVolumes: 5);

        // Act
        manga.SetCustomTotalVolumes(totalVolumes);

        // Assert
        Assert.Null(manga.CustomTotalVolumes);
        Assert.Null(manga.EffectiveTotalVolumes);
    }

    [Fact]
    public void SetCustomTotalVolumes_WhenTotalIsLowerThanHighestOwnedVolume_ShouldThrowDomainException()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        var manga = new MangaCollectionItem(
            userId: userId, malId: 2,
            title: "Berserk",
            imageUrl: null,
            malTotalVolumes: null,
            customTotalVolumes: null);

        manga.AddOwnedVolume(new VolumeNumber(5));

        // Act
        var exception = Assert.Throws<DomainException>(() =>
            manga.SetCustomTotalVolumes(4));

        // Assert
        Assert.Equal("Total volumes cannot be lower than the highest owned volume.", exception.Message);
    }
}