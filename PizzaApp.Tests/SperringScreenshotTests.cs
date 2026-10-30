using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PizzaApp.Api.Services;

namespace PizzaApp.Tests;

// CACHE OCH FEL: testerna använder en bildkälla utan nätverk eller webbläsare.
public class SperringScreenshotTests
{
    [Fact]
    public async Task Shares_image_until_refresh_interval_and_then_captures_again()
    {
        var source = new Capture();
        var clock = new Clock();
        using var service = Create(source, clock);

        var first = await service.GetAsync(default);
        Assert.Equal(first, await service.GetAsync(default));
        Assert.Equal(1, source.Calls);

        clock.Now = clock.Now.AddHours(6);
        var updated = await service.GetAsync(default);
        Assert.Equal(2, source.Calls);
        Assert.True(updated.CapturedAt > first.CapturedAt);
        Assert.False(updated.IsStale);
    }

    [Fact]
    public async Task Keeps_last_image_on_failure_and_retries_after_fifteen_minutes()
    {
        var source = new Capture();
        var clock = new Clock();
        using var service = Create(source, clock);
        var first = await service.GetAsync(default);
        clock.Now = clock.Now.AddHours(6);
        source.Fail = true;

        var stale = await service.GetAsync(default);
        Assert.Equal(first.DataUrl, stale.DataUrl);
        Assert.Equal(first.CapturedAt, stale.CapturedAt);
        Assert.True(stale.IsStale);
        await service.GetAsync(default);
        Assert.Equal(2, source.Calls);

        clock.Now = clock.Now.AddMinutes(15);
        source.Fail = false;
        Assert.False((await service.GetAsync(default)).IsStale);
        Assert.Equal(3, source.Calls);
    }

    [Fact]
    public async Task Initial_failure_does_not_launch_browser_on_every_visit()
    {
        var source = new Capture { Fail = true };
        using var service = Create(source, new Clock());
        await Assert.ThrowsAsync<InvalidDataException>(() => service.GetAsync(default));
        await Assert.ThrowsAsync<InvalidDataException>(() => service.GetAsync(default));
        Assert.Equal(1, source.Calls);
    }

    [Fact]
    public async Task Concurrent_visits_share_one_capture()
    {
        var source = new Capture { Pending = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        using var service = Create(source, new Clock());
        var first = service.GetAsync(default);
        var second = service.GetAsync(default);
        source.Pending.SetResult([137, 80, 78, 71]);

        var results = await Task.WhenAll(first, second);
        Assert.Equal(results[0], results[1]);
        Assert.Equal(1, source.Calls);
    }

    // TESTDUBBLAR: styr tiden och simulera långsam eller otillgänglig bildkälla.
    private static SperringScreenshotService Create(Capture source, Clock clock) =>
        new(source, clock, Options.Create(new SperringScreenshotOptions()), NullLogger<SperringScreenshotService>.Instance);

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.Parse("2026-10-30T12:00:00Z");
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Capture : ISperringScreenshotCapture
    {
        public int Calls;
        public bool Fail;
        public TaskCompletionSource<byte[]>? Pending;
        public Task<byte[]> CaptureAsync(CancellationToken cancellationToken)
        {
            Calls++;
            if (Fail) throw new InvalidDataException("Simulerat källfel");
            return Pending?.Task ?? Task.FromResult<byte[]>([137, 80, 78, 71]);
        }
    }
}
