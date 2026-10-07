using AwesomeAssertions;
using MangaTracker.Infrastructure.ExternalServices.ComicVine;
using ManualClock = Microsoft.Extensions.Time.Testing.FakeTimeProvider;

namespace MangaTracker.Tests.Infrastructure.ComicVine;

// Driven by a manual clock: time only moves when the test advances it, so the results
// do not depend on how fast or busy the machine running the tests is.
public sealed class ComicVineRequestGateTests
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private readonly ManualClock _clock = new();

    [Fact]
    public async Task Each_caller_should_go_through_one_full_interval_after_the_previous_one()
    {
        var gate = new ComicVineRequestGate(_clock, Interval, maxWait: TimeSpan.FromSeconds(10));

        var first = gate.WaitForTurnAsync(CancellationToken.None);
        var second = gate.WaitForTurnAsync(CancellationToken.None);
        var third = gate.WaitForTurnAsync(CancellationToken.None);

        await first.WaitAsync(TimeSpan.FromSeconds(5));
        second.IsCompleted.Should().BeFalse("the first caller just went out");

        _clock.Advance(Interval - TimeSpan.FromMilliseconds(1));
        await Task.Yield();
        second.IsCompleted.Should().BeFalse("a full interval has not passed yet");

        _clock.Advance(TimeSpan.FromMilliseconds(1));
        await second.WaitAsync(TimeSpan.FromSeconds(5));
        third.IsCompleted.Should().BeFalse("the third caller waits from when the second went out");

        _clock.Advance(Interval);
        await third.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task A_late_timer_should_not_let_the_next_caller_go_out_right_behind()
    {
        var gate = new ComicVineRequestGate(_clock, Interval, maxWait: TimeSpan.FromSeconds(10));

        await gate.WaitForTurnAsync(CancellationToken.None);
        var second = gate.WaitForTurnAsync(CancellationToken.None);
        var third = gate.WaitForTurnAsync(CancellationToken.None);

        // The clock jumps far beyond the second caller's turn, as if its timer fired late.
        _clock.Advance(TimeSpan.FromSeconds(3));
        await second.WaitAsync(TimeSpan.FromSeconds(5));

        await Task.Yield();
        third.IsCompleted.Should().BeFalse("spacing counts from when the second caller really went out");

        _clock.Advance(Interval);
        await third.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task A_caller_with_too_many_requests_ahead_should_fail_fast()
    {
        var gate = new ComicVineRequestGate(_clock, Interval, maxWait: TimeSpan.FromSeconds(1));

        await gate.WaitForTurnAsync(CancellationToken.None);
        var second = gate.WaitForTurnAsync(CancellationToken.None);
        var third = gate.WaitForTurnAsync(CancellationToken.None);
        var fourth = () => gate.WaitForTurnAsync(CancellationToken.None);

        // Two callers already queued means at least two intervals of waiting: over the limit.
        await fourth.Should().ThrowAsync<HttpRequestException>();

        // The rejected caller took no turn: the two queued ones still go out, a second apart.
        _clock.Advance(Interval);
        await second.WaitAsync(TimeSpan.FromSeconds(5));
        _clock.Advance(Interval);
        await third.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Waiting_should_stop_when_the_caller_cancels()
    {
        var gate = new ComicVineRequestGate(_clock, Interval, maxWait: TimeSpan.FromSeconds(5));
        await gate.WaitForTurnAsync(CancellationToken.None);

        using var cancellation = new CancellationTokenSource();
        var waiting = gate.WaitForTurnAsync(cancellation.Token);
        cancellation.Cancel();

        var act = () => waiting;
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
