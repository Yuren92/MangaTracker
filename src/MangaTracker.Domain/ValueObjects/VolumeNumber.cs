using MangaTracker.Domain.Common;

namespace MangaTracker.Domain.ValueObjects;

public sealed record VolumeNumber
{
    public int Value { get; }

    public VolumeNumber(int value)
    {
        if (value <= 0)
        {
            throw new DomainException("Volume number must be greater than zero.");
        }

        Value = value;
    }
}