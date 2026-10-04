namespace MangaTracker.Tests.Integration.Infrastructure;

// Real time plus an offset, so integration tests can jump forward (for example past a
// token's expiry) while everything else keeps running on the real clock.
public sealed class AdjustableClock : TimeProvider
{
    private TimeSpan _offset;

    public override DateTimeOffset GetUtcNow() => base.GetUtcNow() + _offset;

    public IDisposable Advance(TimeSpan by)
    {
        _offset += by;
        return new Restore(() => _offset -= by);
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
