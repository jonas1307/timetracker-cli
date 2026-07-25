using Timetracker.Options;
using Timetracker.Services;
using Timetracker.Validators;

namespace Timetracker.Tests;

// Exercises the first-time-setup credential rules, which fire only when no config exists.
// The TempConfigDir seam makes ConfigService.ConfigExists() deterministic.
public class ConfigValidatorFirstTimeTests
{
    [Fact]
    public void FirstTime_MissingToken_Fails()
    {
        using var dir = new TempConfigDir(); // empty -> ConfigExists() == false

        var opts = new ConfigOptions { TimetrackerUrl = "https://acme.timehub.7pace.com" };

        var result = new ConfigValidator().Validate(opts);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Bearer token is required"));
    }

    [Fact]
    public void FirstTime_MissingUrl_Fails()
    {
        using var dir = new TempConfigDir();

        var opts = new ConfigOptions { TimetrackerBearerToken = "token-123" };

        var result = new ConfigValidator().Validate(opts);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Timetracker URL is required"));
    }

    [Fact]
    public void FirstTime_UrlAndToken_IsValid()
    {
        using var dir = new TempConfigDir();

        var opts = new ConfigOptions
        {
            TimetrackerUrl = "https://acme.timehub.7pace.com",
            TimetrackerBearerToken = "token-123",
        };

        Assert.True(new ConfigValidator().Validate(opts).IsValid);
    }

    [Fact]
    public void AfterConfigured_BorderOnly_IsValid()
    {
        using var dir = new TempConfigDir();

        // Establish an existing config first.
        ConfigService.SaveConfig(new ConfigOptions
        {
            TimetrackerUrl = "https://acme.timehub.7pace.com",
            TimetrackerBearerToken = "token-123",
        });

        // Now a partial update with neither url nor token must be accepted.
        var result = new ConfigValidator().Validate(new ConfigOptions { Border = "square" });

        Assert.True(result.IsValid);
    }
}
