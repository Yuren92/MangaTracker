namespace MangaTracker.Infrastructure.ExternalServices.ComicVine;

// Comic Vine throttles by request velocity: at most about one request per second, and
// going faster gets the key restricted harder the more it happens
// (https://comicvine.gamespot.com/forums/api-developers-2334/api-rate-limiting-1746419/).
// This gate, shared by the whole process, lets requests through one at a time and at
// least one interval after the previous one actually went out, whichever user or
// request they belong to.
public sealed class ComicVineRequestGate
{
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(1);

    // Waiting longer than this means too many requests are already queued. The attempt
    // fails like a network error: the resilience pipeline retries it after a backoff and,
    // if the queue does not drain, the API answers 503 instead of holding the request.
    // Kept well below the per-attempt timeout, which includes this wait.
    public static readonly TimeSpan DefaultMaxWait = TimeSpan.FromSeconds(5);

    private readonly SemaphoreSlim _turn = new(1, 1);
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _maxWait;
    private DateTimeOffset _lastRelease = DateTimeOffset.MinValue;
    private int _waiting;

    public ComicVineRequestGate(TimeProvider timeProvider)
        : this(timeProvider, DefaultInterval, DefaultMaxWait)
    {
    }

    public ComicVineRequestGate(TimeProvider timeProvider, TimeSpan interval, TimeSpan maxWait)
    {
        _timeProvider = timeProvider;
        _interval = interval;
        _maxWait = maxWait;
    }

    // Spacing is measured from when the previous caller was really let through, not
    // from a time reserved in advance: if a timer fires late (a busy machine), the next
    // caller still waits a full interval instead of going out right behind it.
    public async Task WaitForTurnAsync(CancellationToken cancellationToken)
    {
        var ahead = Interlocked.Increment(ref _waiting) - 1;

        try
        {
            if (ahead * _interval > _maxWait)
            {
                throw new HttpRequestException(
                    $"Too many Comic Vine requests queued ({ahead} ahead of this one).");
            }

            await _turn.WaitAsync(cancellationToken);

            try
            {
                var wait = _lastRelease + _interval - _timeProvider.GetUtcNow();

                if (wait > TimeSpan.Zero)
                {
                    await Task.Delay(wait, _timeProvider, cancellationToken);
                }

                _lastRelease = _timeProvider.GetUtcNow();
            }
            finally
            {
                _turn.Release();
            }
        }
        finally
        {
            Interlocked.Decrement(ref _waiting);
        }
    }
}

public sealed class ComicVineThrottlingHandler : DelegatingHandler
{
    private readonly ComicVineRequestGate _gate;

    public ComicVineThrottlingHandler(ComicVineRequestGate gate)
    {
        _gate = gate;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await _gate.WaitForTurnAsync(cancellationToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
