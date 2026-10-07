using System.Diagnostics;
using AwesomeAssertions;
using MangaTracker.Infrastructure.ExternalServices.ComicVine;

namespace MangaTracker.Tests.Infrastructure.ComicVine;

public sealed class ComicVineRequestGateTests
{
    [Fact]
    public async Task Concurrent_callers_should_be_spaced_at_least_one_interval_apart()
    {
        var interval = TimeSpan.FromMilliseconds(100);
        var gate = new ComicVineRequestGate(TimeProvider.System, interval, maxWait: TimeSpan.FromSeconds(5));
        var stopwatch = Stopwatch.StartNew();

        var turns = await Task.WhenAll(Enumerable.Range(0, 4).Select(async _ =>
        {
            await gate.WaitForTurnAsync(CancellationToken.None);
            return stopwatch.Elapsed;
        }));

        var ordered = turns.Order().ToList();
        ordered[0].Should().BeLessThan(interval, "the first caller does not wait");

        for (var i = 1; i < ordered.Count; i++)
        {
            // A few milliseconds of tolerance for timer resolution.
            (ordered[i] - ordered[i - 1]).Should().BeGreaterThan(interval - TimeSpan.FromMilliseconds(20));
        }
    }

    [Fact]
    public async Task A_caller_that_would_wait_too_long_should_fail_fast_without_taking_a_slot()
    {
        var gate = new ComicVineRequestGate(TimeProvider.System, TimeSpan.FromSeconds(10), maxWait: TimeSpan.FromSeconds(1));

        await gate.WaitForTurnAsync(CancellationToken.None);

        var act = () => gate.WaitForTurnAsync(CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task Waiting_should_stop_when_the_caller_cancels()
    {
        var gate = new ComicVineRequestGate(TimeProvider.System, TimeSpan.FromSeconds(3), maxWait: TimeSpan.FromSeconds(5));
        await gate.WaitForTurnAsync(CancellationToken.None);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var act = () => gate.WaitForTurnAsync(cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
