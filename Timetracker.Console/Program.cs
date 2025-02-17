using CommandLine;
using Timetracker.Options;
using Timetracker.Services;
using Timetracker.Validators;

await Parser.Default.ParseArguments<ConfigOptions, ActivityTypeOptions, AddOptions>(args)
    .MapResult(
        async (ConfigOptions opts) => await ConfigAction(opts),
        async (AddOptions opts) => await AddActions(opts),
        async (ActivityTypeOptions opts) => await ActivitiyTypeAction(opts),
        errs => Task.FromResult(0)
    );

async Task ActivitiyTypeAction(ActivityTypeOptions opts)
{
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

    Console.ResetColor();
}

async Task ConfigAction(ConfigOptions opts)
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

            return;
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
    }
    finally
    {
        Console.ResetColor();
    }
}

static async Task AddActions(AddOptions opts)
{
    try
    {
        var activities = ActivityService.GetActivities()
            .Select(x => x.Name.ToUpper());

        var validator = new AddValidator(activities);

        var result = validator.Validate(opts);

        if (!result.IsValid)
        {
            Console.ForegroundColor = ConsoleColor.Red;

            foreach (var error in result.Errors)
            {
                Console.WriteLine($"- {error.ErrorMessage}");
            }

            return;
        }

        await HttpService.RegisterActivity(opts);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Activity successfully created.");
    }
    finally
    {
        Console.ResetColor();
    }
}
