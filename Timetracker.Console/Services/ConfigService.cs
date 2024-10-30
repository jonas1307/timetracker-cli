using Newtonsoft.Json;
using Timetracker.Options;

namespace Timetracker.Services;

public record Config
{
    public string TimetrackerUrl { get; set; }
    public string TimetrackerBearerToken { get; set; }
    public string TimetrackerUserId { get; set; }
}

public static class ConfigService
{
    private const string APPLICATION_NAME = "Timetracker.Console";
    private const string JSON_FILE_NAME = "config.json";

    private static string GetConfigPath()
    {
        string folderPath;

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), APPLICATION_NAME);
        }
        else
        {
            folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", APPLICATION_NAME);
        }

        return Path.Combine(folderPath, JSON_FILE_NAME);
    }

    public static string LoadSetting(string setting)
    {
        var configPath = GetConfigPath();

        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"{JSON_FILE_NAME} does not exist. Make sure you already executed the config method.");
        }

        var type = typeof(Config);

        var prop = type.GetProperty(setting);

        if (prop != null)
        {
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
            return (string)prop.GetValue(config);
        }

        return null;
    }

    public static void SaveConfig(ConfigOptions opts, string userId)
    {
        var configPath = GetConfigPath();
        var folderPath = Path.GetDirectoryName(configPath);

        var config = new Config
        {
            TimetrackerBearerToken = opts.TimetrackerBearerToken,
            TimetrackerUrl = opts.TimetrackerUrl,
            TimetrackerUserId = userId
        };

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
    }
}
