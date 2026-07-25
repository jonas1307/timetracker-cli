using Timetracker.Utils;

namespace Timetracker.Tests;

public class ValidationUtilsTests
{
    [Theory]
    [InlineData("today", true)]
    [InlineData("TODAY", true)]
    [InlineData("yesterday", true)]
    [InlineData("2026/06/15", true)]
    [InlineData("2026/6/15", false)]   // needs zero-padded MM/DD
    [InlineData("2026-06-15", false)]  // wrong separator
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("tomorrow", false)]
    public void ValidActivityDate(string input, bool expected)
        => Assert.Equal(expected, ValidationUtils.ValidActivityDate(input));

    [Theory]
    [InlineData("https://acme.timehub.7pace.com", true)]
    [InlineData("http://acme.timehub.7pace.com", false)] // HTTPS enforced
    [InlineData("ftp://acme.com", false)]
    [InlineData("not-a-url", false)]
    [InlineData("", false)]
    public void ValidUrl(string input, bool expected)
        => Assert.Equal(expected, ValidationUtils.ValidUrl(input));

    [Theory]
    [InlineData("2026/06", true, 2026, 6, 30)]
    [InlineData("2026/02", true, 2026, 2, 28)]
    [InlineData("2024/02", true, 2024, 2, 29)] // leap year
    [InlineData("2026/13", false, 0, 0, 0)]
    [InlineData("2026-06", false, 0, 0, 0)]
    [InlineData("June", false, 0, 0, 0)]
    public void TryResolveMonth(string input, bool expectedOk, int year, int month, int lastDay)
    {
        var ok = ValidationUtils.TryResolveMonth(input, out var first, out var last);

        Assert.Equal(expectedOk, ok);
        if (expectedOk)
        {
            Assert.Equal(new DateTime(year, month, 1), first);
            Assert.Equal(new DateTime(year, month, lastDay), last);
        }
    }

    [Theory]
    [InlineData("development", true)]  // input is upper-cased before matching
    [InlineData("Development", true)]
    [InlineData("DEVELOPMENT", true)]
    [InlineData("meeting", false)]
    [InlineData("", false)]
    [InlineData(null, false)]          // null is not a valid type (guards the flag-less flow)
    public void ValidType_IsCaseInsensitiveAndUpperCases(string input, bool expected)
    {
        var activities = new[] { "DEVELOPMENT", "TESTING" };

        Assert.Equal(expected, ValidationUtils.ValidType(activities, input));
    }
}
