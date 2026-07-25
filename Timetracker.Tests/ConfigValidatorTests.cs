using Timetracker.Options;
using Timetracker.Validators;

namespace Timetracker.Tests;

// Only the rules that do not depend on ConfigService.ConfigExists() are covered here.
// The first-time-setup credential rules touch the real config store and are deferred to
// Phase 2, once that existence check can be injected.
public class ConfigValidatorTests
{
    [Fact]
    public void NoOptions_Fails()
    {
        var result = new ConfigValidator().Validate(new ConfigOptions());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("at least one option"));
    }

    [Theory]
    [InlineData("minimal")]
    [InlineData("square")]
    [InlineData("markdown")]
    [InlineData("MARKDOWN")]
    public void ValidBorder_IsValid(string border)
    {
        // Show bypasses the first-time credential rules, isolating the border rule.
        var opts = new ConfigOptions { Show = true, Border = border };

        Assert.True(new ConfigValidator().Validate(opts).IsValid);
    }

    [Fact]
    public void InvalidBorder_Fails()
    {
        var opts = new ConfigOptions { Show = true, Border = "fancy" };

        Assert.False(new ConfigValidator().Validate(opts).IsValid);
    }

    [Fact]
    public void NonHttpsUrl_Fails()
    {
        var opts = new ConfigOptions { Show = true, TimetrackerUrl = "http://acme.timehub.7pace.com" };

        Assert.False(new ConfigValidator().Validate(opts).IsValid);
    }

    [Fact]
    public void HttpsUrl_IsValid()
    {
        var opts = new ConfigOptions { Show = true, TimetrackerUrl = "https://acme.timehub.7pace.com" };

        Assert.True(new ConfigValidator().Validate(opts).IsValid);
    }
}
