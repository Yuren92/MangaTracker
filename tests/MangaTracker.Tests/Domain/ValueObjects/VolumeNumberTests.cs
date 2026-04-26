using MangaTracker.Domain.Common;
using MangaTracker.Domain.ValueObjects;

namespace MangaTracker.Tests.Domain.ValueObjects;

public class VolumeNumberTests
{
    [Fact]
    public void Constructor_WithPositiveValue_ShouldCreateVolumeNumber()
    {
        // Arrange
        var value = 1;

        // Act
        var volumeNumber = new VolumeNumber(value);

        // Assert
        Assert.Equal(value, volumeNumber.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Constructor_WithZeroOrNegativeValue_ShouldThrowDomainException(int value)
    {
        // Act
        var exception = Assert.Throws<DomainException>(() => new VolumeNumber(value));

        // Assert
        Assert.Equal("Volume number must be greater than zero.", exception.Message);
    }
}