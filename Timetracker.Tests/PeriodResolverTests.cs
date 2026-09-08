using Timetracker.Utils;

namespace Timetracker.Tests;

public class PeriodResolverTests
{
    // --- Error paths -------------------------------------------------------

    [Fact]
    public void MutuallyExclusiveFlags_Fail()
    {
        var opts = new FakePeriodOptions { Today = true, Week = true };

        var ok = PeriodResolver.TryResolve(opts, out _, out _, out var error);

        Assert.False(ok);
        Assert.Contains("mutually exclusive", error);
    }

    [Fact]
    public void PeriodPlusExplicitPeriod_Fail()
    {
        var opts = new FakePeriodOptions { Month = true, Period = "2026/06" };

        var ok = PeriodResolver.TryResolve(opts, out _, out _, out var error);

        Assert.False(ok);
        Assert.Contains("mutually exclusive", error);
    }

    [Fact]
    public void ShortcutWithFromOrTo_Fail()
    {
        var opts = new FakePeriodOptions { Week = true, From = "2026/06/01" };

        var ok = PeriodResolver.TryResolve(opts, out _, out _, out var error);

        Assert.False(ok);
        Assert.Contains("shortcuts cannot be used", error);
    }

    [Fact]
    public void PeriodWithFromOrTo_Fail()
    {
        var opts = new FakePeriodOptions { Period = "2026/06", To = "2026/06/15" };

        var ok = PeriodResolver.TryResolve(opts, out _, out _, out var error);

        Assert.False(ok);
        Assert.Contains("--period cannot be used", error);
    }

    [Theory]
    [InlineData("2026-06")]
    [InlineData("2026/13")]
    [InlineData("June")]
    [InlineData("26/06")]
    public void InvalidPeriodFormat_Fail(string period)
    {
        var opts = new FakePeriodOptions { Period = period };

        var ok = PeriodResolver.TryResolve(opts, out _, out _, out var error);

        Assert.False(ok);
        Assert.Contains("Invalid period format", error);
    }

    [Fact]
    public void FromAfterTo_Fail()
    {
        var opts = new FakePeriodOptions { From = "2026/06/30", To = "2026/06/01" };

        var ok = PeriodResolver.TryResolve(opts, out _, out _, out var error);

        Assert.False(ok);
        Assert.Contains("must be earlier than or equal", error);
    }

    // --- Success paths -----------------------------------------------------

    [Fact]
    public void NoOptions_DefaultsToToday()
    {
        var ok = PeriodResolver.TryResolve(new FakePeriodOptions(), out var from, out var to, out _);

        Assert.True(ok);
        Assert.Equal(DateTime.Today, from);
        Assert.Equal(DateTime.Today, to);
    }

    [Fact]
    public void Today_ResolvesToToday()
    {
        var ok = PeriodResolver.TryResolve(new FakePeriodOptions { Today = true }, out var from, out var to, out _);

        Assert.True(ok);
        Assert.Equal(DateTime.Today, from);
        Assert.Equal(DateTime.Today, to);
    }

    [Fact]
    public void Yesterday_ResolvesToYesterday()
    {
        var ok = PeriodResolver.TryResolve(new FakePeriodOptions { Yesterday = true }, out var from, out var to, out _);

        Assert.True(ok);
        Assert.Equal(DateTime.Today.AddDays(-1), from);
        Assert.Equal(DateTime.Today.AddDays(-1), to);
    }

    [Fact]
    public void ExplicitRange_IsPreserved()
    {
        var opts = new FakePeriodOptions { From = "2026/06/10", To = "2026/06/20" };

        var ok = PeriodResolver.TryResolve(opts, out var from, out var to, out _);

        Assert.True(ok);
        Assert.Equal(new DateTime(2026, 6, 10), from);
        Assert.Equal(new DateTime(2026, 6, 20), to);
    }

    [Fact]
    public void Period_ResolvesToWholeMonth()
    {
        var opts = new FakePeriodOptions { Period = "2026/02" };

        var ok = PeriodResolver.TryResolve(opts, out var from, out var to, out _);

        Assert.True(ok);
        Assert.Equal(new DateTime(2026, 2, 1), from);
        Assert.Equal(new DateTime(2026, 2, 28), to); // 2026 is not a leap year
    }

    // --- Time-relative invariants (independent of the actual "today") ------

    [Fact]
    public void Week_IsMondayToSunday()
    {
        var ok = PeriodResolver.TryResolve(new FakePeriodOptions { Week = true }, out var from, out var to, out _);

        Assert.True(ok);
        Assert.Equal(DayOfWeek.Monday, from.DayOfWeek);
        Assert.Equal(DayOfWeek.Sunday, to.DayOfWeek);
        Assert.Equal(6, (to - from).Days);
    }

    [Fact]
    public void LastWeek_IsExactlySevenDaysBeforeThisWeek()
    {
        PeriodResolver.TryResolve(new FakePeriodOptions { Week = true }, out var weekFrom, out var weekTo, out _);
        PeriodResolver.TryResolve(new FakePeriodOptions { LastWeek = true }, out var lastFrom, out var lastTo, out _);

        Assert.Equal(weekFrom.AddDays(-7), lastFrom);
        Assert.Equal(weekTo.AddDays(-7), lastTo);
    }

    [Fact]
    public void Month_CoversFirstToLastDayOfCurrentMonth()
    {
        var ok = PeriodResolver.TryResolve(new FakePeriodOptions { Month = true }, out var from, out var to, out _);

        Assert.True(ok);
        Assert.Equal(1, from.Day);
        Assert.Equal(DateTime.Today.Month, from.Month);
        Assert.Equal(from.AddMonths(1).AddDays(-1), to);
    }

    [Fact]
    public void LastMonth_CoversFirstToLastDayOfPreviousMonth()
    {
        var ok = PeriodResolver.TryResolve(new FakePeriodOptions { LastMonth = true }, out var from, out var to, out _);

        Assert.True(ok);
        Assert.Equal(1, from.Day);
        Assert.Equal(from.AddMonths(1).AddDays(-1), to);
        Assert.Equal(DateTime.Today.Month, from.AddMonths(1).Month);
    }

    // --- current-* aliases -------------------------------------------------

    [Fact]
    public void CurrentMonth_ResolvesIdenticallyToMonth()
    {
        PeriodResolver.TryResolve(new FakePeriodOptions { Month = true }, out var mFrom, out var mTo, out _);
        var ok = PeriodResolver.TryResolve(new FakePeriodOptions { CurrentMonth = true }, out var from, out var to, out _);

        Assert.True(ok);
        Assert.Equal(mFrom, from);
        Assert.Equal(mTo, to);
    }

    [Fact]
    public void CurrentWeek_ResolvesIdenticallyToWeek()
    {
        PeriodResolver.TryResolve(new FakePeriodOptions { Week = true }, out var wFrom, out var wTo, out _);
        var ok = PeriodResolver.TryResolve(new FakePeriodOptions { CurrentWeek = true }, out var from, out var to, out _);

        Assert.True(ok);
        Assert.Equal(wFrom, from);
        Assert.Equal(wTo, to);
    }

    [Fact]
    public void CurrentMonth_AndMonth_AreMutuallyExclusive()
    {
        var opts = new FakePeriodOptions { Month = true, CurrentMonth = true };

        var ok = PeriodResolver.TryResolve(opts, out _, out _, out var error);

        Assert.False(ok);
        Assert.Contains("mutually exclusive", error);
    }

    [Fact]
    public void CurrentWeek_AndWeek_AreMutuallyExclusive()
    {
        var opts = new FakePeriodOptions { Week = true, CurrentWeek = true };

        var ok = PeriodResolver.TryResolve(opts, out _, out _, out var error);

        Assert.False(ok);
        Assert.Contains("mutually exclusive", error);
    }

    [Fact]
    public void CurrentMonth_WithFromOrTo_Fail()
    {
        var opts = new FakePeriodOptions { CurrentMonth = true, From = "2026/06/01" };

        var ok = PeriodResolver.TryResolve(opts, out _, out _, out var error);

        Assert.False(ok);
        Assert.Contains("shortcuts cannot be used", error);
    }

    [Fact]
    public void CurrentWeek_WithFromOrTo_Fail()
    {
        var opts = new FakePeriodOptions { CurrentWeek = true, To = "2026/06/30" };

        var ok = PeriodResolver.TryResolve(opts, out _, out _, out var error);

        Assert.False(ok);
        Assert.Contains("shortcuts cannot be used", error);
    }
}
