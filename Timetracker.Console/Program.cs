using CommandLine;
using Timetracker.Options;
using Timetracker.Services;
using Timetracker.Utils;
using Timetracker.Validators;

try
{
    return await Parser.Default.ParseArguments<ConfigOptions, ActivityTypeOptions, AddOptions>(args)
        .MapResult(
            async (ConfigOptions opts) => await ConfigAction(opts),
            async (AddOptions opts) => await AddActions(opts),
            async (ActivityTypeOptions opts) => await ActivityTypeAction(opts),
            errs => Task.FromResult(1)
        );
}
catch (Exception ex)
{
    ConsoleHelper.WriteError($"Unexpected error: {ex.Message}");
    return 1;
}

async Task<int> ActivityTypeAction(ActivityTypeOptions opts)
{
    if (!ConfigService.ConfigExists())
    {
        ConsoleHelper.WriteError("Configuration not found. Please run the 'config' command first.");
        return 1;
    }

    if (opts.SyncActivities)
    {
        Console.WriteLine("Synchronizing activities...");

        await ActivityService.SeedActivities();
    }

    var activities = ActivityService.GetActivities();

    ConsoleHelper.WriteSuccess("The available activities are: ");

    foreach (var item in activities)
    {
        Console.WriteLine(item.Name);
    }

    return 0;
}

async Task<int> ConfigAction(ConfigOptions opts)
{
    var validator = new ConfigValidator();

    var result = validator.Validate(opts);

    if (!result.IsValid)
    {
        foreach (var error in result.Errors)
        {
            ConsoleHelper.WriteError($"- {error.ErrorMessage}");
        }

        return 1;
    }

    Console.WriteLine("Obtaining User Id...");

    var user = await HttpService.GetTimetrackerUser(opts.TimetrackerUrl, opts.TimetrackerBearerToken);

    Console.WriteLine("User ID obtained successfully.");

    Console.WriteLine("Creating config file...");

    ConfigService.SaveConfig(opts, user.Data.User.Id);

    Console.WriteLine("Config file created.");

    Console.WriteLine("Creating activities...");

    await ActivityService.SeedActivities();

    Console.WriteLine("Activities file created.");

    return 0;
}

static async Task<int> AddActions(AddOptions opts)
{
    if (!ConfigService.ConfigExists())
    {
        ConsoleHelper.WriteError("Configuration not found. Please run the 'config' command first.");
        return 1;
    }

    var activities = ActivityService.GetActivities();

    var validator = new AddValidator(activities.Select(x => x.Name.ToUpper()));

    var result = validator.Validate(opts);

    if (!result.IsValid)
    {
        foreach (var error in result.Errors)
        {
            ConsoleHelper.WriteError($"- {error.ErrorMessage}");
        }

        return 1;
    }

    var activityId = ActivityService.GetActivityId(opts.ActivityType, activities);

    await HttpService.RegisterActivity(opts, activityId);

    ConsoleHelper.WriteSuccess("Activity successfully created.");

    return 0;
}
