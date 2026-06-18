using CommandLine;
using Timetracker.Options;
using Timetracker.Services;
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
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine($"Unexpected error: {ex.Message}");
    Console.ResetColor();
    return 1;
}

async Task<int> ActivityTypeAction(ActivityTypeOptions opts)
{
    try
    {
        if (!ConfigService.ConfigExists())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Configuration not found. Please run the 'config' command first.");
            return 1;
        }

        if (opts.SyncActivities)
        {
            Console.WriteLine("Synchronizing activities...");

            await ActivityService.SeedActivities();
        }

        var activities = ActivityService.GetActivities();

        Console.ForegroundColor = ConsoleColor.Green;

        Console.WriteLine("The available activities are: ");

        foreach (var item in activities)
        {
            Console.WriteLine(item.Name);
        }

        return 0;
    }
    finally
    {
        Console.ResetColor();
    }
}

async Task<int> ConfigAction(ConfigOptions opts)
{
    try
    {
        var validator = new ConfigValidator();

        var result = validator.Validate(opts);

        if (!result.IsValid)
        {
            Console.ForegroundColor = ConsoleColor.Red;

            foreach (var error in result.Errors)
            {
                Console.WriteLine($"- {error.ErrorMessage}");
            }

            return 1;
        }

        // Gets user ID
        Console.WriteLine("Obtaining User Id...");

        var user = await HttpService.GetTimetrackerUser(opts.TimetrackerUrl, opts.TimetrackerBearerToken);

        Console.WriteLine("User ID obtained successfully.");

        // Creates config file
        Console.WriteLine("Creating config file...");

        ConfigService.SaveConfig(opts, user.Data.User.Id);

        Console.WriteLine("Config file created.");

        // Creates activity file
        Console.WriteLine("Creating activities...");

        await ActivityService.SeedActivities();

        Console.WriteLine("Activities file created.");

        return 0;
    }
    finally
    {
        Console.ResetColor();
    }
}

static async Task<int> AddActions(AddOptions opts)
{
    try
    {
        if (!ConfigService.ConfigExists())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Configuration not found. Please run the 'config' command first.");
            return 1;
        }

        var activities = ActivityService.GetActivities();

        var validator = new AddValidator(activities.Select(x => x.Name.ToUpper()));

        var result = validator.Validate(opts);

        if (!result.IsValid)
        {
            Console.ForegroundColor = ConsoleColor.Red;

            foreach (var error in result.Errors)
            {
                Console.WriteLine($"- {error.ErrorMessage}");
            }

            return 1;
        }

        var activityId = ActivityService.GetActivityId(opts.ActivityType, activities);

        await HttpService.RegisterActivity(opts, activityId);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Activity successfully created.");

        return 0;
    }
    finally
    {
        Console.ResetColor();
    }
}
