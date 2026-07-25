using Timetracker.Options;
using Timetracker.Validators;

namespace Timetracker.Tests;

public class UpdateValidatorTests
{
    private static readonly string[] Activities = ["DEVELOPMENT", "TESTING"];

    [Fact]
    public void NoFields_Fails()
    {
        var result = new UpdateValidator(Activities).Validate(new UpdateOptions());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("At least one field"));
    }

    [Fact]
    public void SingleValidField_IsValid()
    {
        var opts = new UpdateOptions { ActivityLength = 3m };

        Assert.True(new UpdateValidator(Activities).Validate(opts).IsValid);
    }

    [Fact]
    public void ProvidedButInvalidDate_Fails()
    {
        var opts = new UpdateOptions { ActivityDate = "2026-06-15" };

        Assert.False(new UpdateValidator(Activities).Validate(opts).IsValid);
    }

    [Fact]
    public void ProvidedButUnknownType_Fails()
    {
        var opts = new UpdateOptions { ActivityType = "Meeting" };

        Assert.False(new UpdateValidator(Activities).Validate(opts).IsValid);
    }

    [Fact]
    public void ProvidedButNonPositiveWorkItem_Fails()
    {
        var opts = new UpdateOptions { WorkItemId = 0 };

        Assert.False(new UpdateValidator(Activities).Validate(opts).IsValid);
    }
}
