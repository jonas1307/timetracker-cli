namespace Timetracker.Tests;

/// <summary>
/// A <see cref="TimeProvider"/> pinned to a fixed local date, with the local time zone
/// forced to UTC so <c>GetLocalNow().Date</c> is deterministic regardless of the host.
/// </summary>
internal sealed class FixedClock : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FixedClock(int year, int month, int day)
        => _now = new DateTimeOffset(year, month, day, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
