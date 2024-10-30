using Newtonsoft.Json;

namespace Timetracker.Services;

public record Config
{
    public string TimetrackerUrl { get; set; }
    public string TimetrackerBearerToken { get; set; }
    public string TimetrackerUserId { get; set; }
}

public static class FileService
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

    public static Config LoadConfig()
    {
        var configPath = GetConfigPath();

        if (File.Exists(configPath))
        {
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
        }

        return new Config();
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

    public static void SaveConfig(Config config)
    {
        var configPath = GetConfigPath();
        var folderPath = Path.GetDirectoryName(configPath);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
    }
}

public record Activity
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public static class ActivityService
{
    private const string APPLICATION_NAME = "Timetracker.Console";
    private const string JSON_FILE_NAME = "activities.json";

    private static string GetActivityPath()
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

    public static IList<Activity> GetActivities()
    {
        var filePath = GetActivityPath();

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"{JSON_FILE_NAME} does not exist. Make sure you already executed the config method.");
        }

        return JsonConvert.DeserializeObject<IList<Activity>>(File.ReadAllText(filePath));
    }

    public async static Task SeedActivities()
    {
        var filePath = GetActivityPath();

        var activities = await HttpService.ListActivityTypes();

        File.WriteAllText(filePath, JsonConvert.SerializeObject(activities.Data.ActivityTypes, Formatting.Indented));
    }
}