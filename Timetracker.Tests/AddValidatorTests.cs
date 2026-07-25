using Timetracker.Options;
using Timetracker.Validators;

namespace Timetracker.Tests;

public class AddValidatorTests
{
    private static readonly string[] Activities = ["DEVELOPMENT", "TESTING"];

    private static AddOptions ValidOptions() => new()
    {
        ActivityDate = "today",
        WorkItemId = 12345,
        ActivityLength = 2m,
        ActivityType = "Development",
        ActivityStartHour = "09:00",
        ActivityComment = null,
    };

    [Fact]
    public void HappyPath_IsValid()
    {
        var result = new AddValidator(Activities).Validate(ValidOptions());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("2026-06-15")]
    [InlineData("tomorrow")]
    [InlineData("")]
    public void InvalidDate_Fails(string date)
    {
        var opts = ValidOptions();
        opts.ActivityDate = date;

        Assert.False(new AddValidator(Activities).Validate(opts).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveWorkItem_Fails(int id)
    {
        var opts = ValidOptions();
        opts.WorkItemId = id;

        Assert.False(new AddValidator(Activities).Validate(opts).IsValid);
    }

    [Fact]
    public void NonPositiveLength_Fails()
    {
        var opts = ValidOptions();
        opts.ActivityLength = 0m;

        Assert.False(new AddValidator(Activities).Validate(opts).IsValid);
    }

    [Fact]
    public void UnknownActivityType_Fails()
    {
        var opts = ValidOptions();
        opts.ActivityType = "Meeting";

        Assert.False(new AddValidator(Activities).Validate(opts).IsValid);
    }

    [Theory]
    [InlineData("24:00")]
    [InlineData("9:60")]
    [InlineData("noon")]
    public void InvalidStartHour_Fails(string hour)
    {
        var opts = ValidOptions();
        opts.ActivityStartHour = hour;

        Assert.False(new AddValidator(Activities).Validate(opts).IsValid);
    }

    [Fact]
    public void ShortComment_Fails()
    {
        var opts = ValidOptions();
        opts.ActivityComment = "ab";

        Assert.False(new AddValidator(Activities).Validate(opts).IsValid);
    }
}
