namespace MangaTracker.Infrastructure.ExternalServices.ComicVine;

// Comic Vine throttles by request velocity: at most about one request per second, and
// going faster gets the key restricted harder the more it happens
// (https://comicvine.gamespot.com/forums/api-developers-2334/api-rate-limiting-1746419/).
// This gate, shared by the whole process, hands out send times at least one interval
// apart, whichever user or request asks for them.
public sealed class ComicVineRequestGate
{
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(1);

    // Waiting longer than this means too many requests are already queued. The attempt
    // fails like a network error: the resilience pipeline retries it after a backoff and,
    // if the queue does not drain, the API answers 503 instead of holding the request.
    // Kept well below the per-attempt timeout, which includes this wait.
    public static readonly TimeSpan DefaultMaxWait = TimeSpan.FromSeconds(5);

    private readonly object _lock = new();
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _maxWait;
    private DateTimeOffset _nextSlot = DateTimeOffset.MinValue;

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

    // Reserves the next free slot and waits for it. The lock only covers the
    // reservation, so concurrent callers queue up without holding it while they wait.
    public async Task WaitForTurnAsync(CancellationToken cancellationToken)
    {
        TimeSpan wait;

        lock (_lock)
        {
            var now = _timeProvider.GetUtcNow();
            var slot = _nextSlot > now ? _nextSlot : now;
            wait = slot - now;

            if (wait > _maxWait)
            {
                throw new HttpRequestException(
                    $"Too many Comic Vine requests queued (next slot in {wait.TotalSeconds:0} s).");
            }

            _nextSlot = slot + _interval;
        }

        if (wait > TimeSpan.Zero)
        {
            await Task.Delay(wait, _timeProvider, cancellationToken);
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
