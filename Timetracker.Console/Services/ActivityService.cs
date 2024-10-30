using Newtonsoft.Json;

namespace Timetracker.Services;

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