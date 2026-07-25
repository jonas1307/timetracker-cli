using Timetracker.Options;
using Timetracker.Services;

namespace Timetracker.Tests;

public class ConfigServiceTests
{
    [Fact]
    public void ConfigExists_FalseWhenEmpty_TrueAfterSave()
    {
        using var dir = new TempConfigDir();

        Assert.False(ConfigService.ConfigExists());

        ConfigService.SaveConfig(new ConfigOptions
        {
            TimetrackerUrl = "https://acme.timehub.7pace.com",
            TimetrackerBearerToken = "token-123",
        });

        Assert.True(ConfigService.ConfigExists());
    }

    [Fact]
    public void SaveThenLoad_RoundTripsValues()
    {
        using var dir = new TempConfigDir();

        ConfigService.SaveConfig(
            new ConfigOptions
            {
                TimetrackerUrl = "https://acme.timehub.7pace.com",
                TimetrackerBearerToken = "secret-token",
                Border = "square",
            },
            userId: "u-1",
            displayName: "Jane",
            email: "jane@acme.com",
            accountName: "acme");

        var config = ConfigService.LoadConfig();

        Assert.Equal("https://acme.timehub.7pace.com", config.TimetrackerUrl);
        Assert.Equal("secret-token", config.TimetrackerBearerToken); // decrypted round-trip
        Assert.Equal("u-1", config.TimetrackerUserId);
        Assert.Equal("Jane", config.DisplayName);
        Assert.Equal("jane@acme.com", config.Email);
        Assert.Equal("acme", config.AccountName);
        Assert.Equal("square", config.TableBorder);
    }

    [Fact]
    public void SaveConfig_IsNonDestructive_PreservesUnsuppliedValues()
    {
        using var dir = new TempConfigDir();

        ConfigService.SaveConfig(new ConfigOptions
        {
            TimetrackerUrl = "https://acme.timehub.7pace.com",
            TimetrackerBearerToken = "secret-token",
        });

        // Change only the border; url and token must survive.
        ConfigService.SaveConfig(new ConfigOptions { Border = "markdown" });

        var config = ConfigService.LoadConfig();

        Assert.Equal("https://acme.timehub.7pace.com", config.TimetrackerUrl);
        Assert.Equal("secret-token", config.TimetrackerBearerToken);
        Assert.Equal("markdown", config.TableBorder);
    }

    [Fact]
    public void GetTableBorder_ReturnsStoredBorderWithoutTouchingToken()
    {
        using var dir = new TempConfigDir();

        ConfigService.SaveConfig(new ConfigOptions
        {
            TimetrackerUrl = "https://acme.timehub.7pace.com",
            TimetrackerBearerToken = "secret-token",
            Border = "minimal",
        });

        Assert.Equal("minimal", ConfigService.GetTableBorder());
    }

    [Fact]
    public void DeleteConfig_RemovesTheFile()
    {
        using var dir = new TempConfigDir();

        ConfigService.SaveConfig(new ConfigOptions
        {
            TimetrackerUrl = "https://acme.timehub.7pace.com",
            TimetrackerBearerToken = "secret-token",
        });
        Assert.True(ConfigService.ConfigExists());

        ConfigService.DeleteConfig();

        Assert.False(ConfigService.ConfigExists());
    }
}
