using Timetracker.Utils;

namespace Timetracker.Tests;

/// <summary>
/// Absolute-date tests for the clock-dependent resolvers, using a pinned <see cref="FixedClock"/>.
/// Restores the system clock after each test so no global state leaks.
/// </summary>
public sealed class ResolveDateTests : IDisposable
{
    public void Dispose()
    {
        ValidationUtils.Clock = TimeProvider.System;
        ValidationUtils.WeekStartOverride = null;
    }

    private static void PinTo(int year, int month, int day)
        => ValidationUtils.Clock = new FixedClock(year, month, day);

    [Fact]
    public void ResolveDate_Today_UsesTheClock()
    {
        PinTo(2026, 3, 15);

        Assert.Equal(new DateTime(2026, 3, 15), ValidationUtils.ResolveDate("today"));
        Assert.Equal(new DateTime(2026, 3, 15), ValidationUtils.ResolveDate(""));
    }

    [Fact]
    public void ResolveDate_Yesterday_UsesTheClock()
    {
        PinTo(2026, 3, 1);

        Assert.Equal(new DateTime(2026, 2, 28), ValidationUtils.ResolveDate("yesterday"));
    }

    [Fact]
    public void ResolveCurrentWeek_MondayStart_MidWeek_ReturnsMondayToSunday()
    {
        // 2026-03-18 is a Wednesday.
        PinTo(2026, 3, 18);
        ValidationUtils.WeekStartOverride = DayOfWeek.Monday;

        var (from, to) = ValidationUtils.ResolveCurrentWeek();

        Assert.Equal(new DateTime(2026, 3, 16), from); // Monday
        Assert.Equal(new DateTime(2026, 3, 22), to);   // Sunday
    }

    [Fact]
    public void ResolveCurrentWeek_MondayStart_OnSunday_ReturnsTheEndingWeek()
    {
        // 2026-03-22 is a Sunday — the week should still be 03-16..03-22.
        PinTo(2026, 3, 22);
        ValidationUtils.WeekStartOverride = DayOfWeek.Monday;

        var (from, to) = ValidationUtils.ResolveCurrentWeek();

        Assert.Equal(new DateTime(2026, 3, 16), from);
        Assert.Equal(new DateTime(2026, 3, 22), to);
    }

    [Fact]
    public void ResolveLastWeek_MondayStart_ReturnsPreviousMondayToSunday()
    {
        PinTo(2026, 3, 18);
        ValidationUtils.WeekStartOverride = DayOfWeek.Monday;

        var (from, to) = ValidationUtils.ResolveLastWeek();

        Assert.Equal(new DateTime(2026, 3, 9), from);
        Assert.Equal(new DateTime(2026, 3, 15), to);
    }

    [Fact]
    public void ResolveCurrentWeek_SundayStart_MidWeek_ReturnsSundayToSaturday()
    {
        // 2026-03-18 is a Wednesday; week should be 03-15 (Sun) .. 03-21 (Sat).
        PinTo(2026, 3, 18);
        ValidationUtils.WeekStartOverride = DayOfWeek.Sunday;

        var (from, to) = ValidationUtils.ResolveCurrentWeek();

        Assert.Equal(new DateTime(2026, 3, 15), from); // Sunday
        Assert.Equal(new DateTime(2026, 3, 21), to);   // Saturday
    }

    [Fact]
    public void ResolveCurrentWeek_SundayStart_OnSunday_StartsOnThatDay()
    {
        // 2026-03-15 is a Sunday — should be the start of the week.
        PinTo(2026, 3, 15);
        ValidationUtils.WeekStartOverride = DayOfWeek.Sunday;

        var (from, to) = ValidationUtils.ResolveCurrentWeek();

        Assert.Equal(new DateTime(2026, 3, 15), from);
        Assert.Equal(new DateTime(2026, 3, 21), to);
    }

    [Fact]
    public void ResolveCurrentWeek_SundayStart_OnSaturday_ReturnsCurrentWeek()
    {
        // 2026-03-21 is a Saturday — last day of the Sun-start week.
        PinTo(2026, 3, 21);
        ValidationUtils.WeekStartOverride = DayOfWeek.Sunday;

        var (from, to) = ValidationUtils.ResolveCurrentWeek();

        Assert.Equal(new DateTime(2026, 3, 15), from);
        Assert.Equal(new DateTime(2026, 3, 21), to);
    }

    [Fact]
    public void ResolveLastWeek_SundayStart_ReturnsPreviousSundayToSaturday()
    {
        PinTo(2026, 3, 18);
        ValidationUtils.WeekStartOverride = DayOfWeek.Sunday;

        var (from, to) = ValidationUtils.ResolveLastWeek();

        Assert.Equal(new DateTime(2026, 3, 8), from);  // Sunday
        Assert.Equal(new DateTime(2026, 3, 14), to);   // Saturday
    }

    [Fact]
    public void ResolveCurrentMonth_ReturnsFirstToLastDay()
    {
        PinTo(2026, 2, 10);

        var (from, to) = ValidationUtils.ResolveCurrentMonth();

        Assert.Equal(new DateTime(2026, 2, 1), from);
        Assert.Equal(new DateTime(2026, 2, 28), to); // non-leap
    }

    [Fact]
    public void ResolveLastMonth_AtStartOfYear_RollsBackToDecember()
    {
        PinTo(2026, 1, 5);

        var (from, to) = ValidationUtils.ResolveLastMonth();

        Assert.Equal(new DateTime(2025, 12, 1), from);
        Assert.Equal(new DateTime(2025, 12, 31), to);
    }
}
