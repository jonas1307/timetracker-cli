using CommandLine;
using Timetracker.Options;
using Timetracker.Services;
using Timetracker.Utils;
using Timetracker.Validators;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    return await Parser.Default.ParseArguments<ConfigOptions, ActivityTypeOptions, AddOptions, DeleteOptions>(args)
        .MapResult(
            async (ConfigOptions opts) => await ConfigAction(opts, cts.Token),
            async (AddOptions opts) => await AddActions(opts, cts.Token),
            async (ActivityTypeOptions opts) => await ActivityTypeAction(opts, cts.Token),
            async (DeleteOptions opts) => await DeleteAction(opts, cts.Token),
            errs => Task.FromResult(1)
        );
}
catch (OperationCanceledException)
{
    ConsoleHelper.WriteError("Operation cancelled.");
    return 1;
}
catch (Exception ex)
{
    ConsoleHelper.WriteError($"Unexpected error: {ex.Message}");
    return 1;
}

async Task<int> ActivityTypeAction(ActivityTypeOptions opts, CancellationToken cancellationToken)
{
    if (!ConfigService.ConfigExists())
    {
        ConsoleHelper.WriteError("Configuration not found. Please run the 'config' command first.");
        return 1;
    }

    if (opts.SyncActivities)
    {
        Console.WriteLine("Synchronizing activities...");

        await ActivityService.SeedActivities(cancellationToken);
    }

    var activities = ActivityService.GetActivities();

    ConsoleHelper.WriteSuccess("The available activities are: ");

    foreach (var item in activities)
    {
        Console.WriteLine(item.Name);
    }

    return 0;
}

async Task<int> ConfigAction(ConfigOptions opts, CancellationToken cancellationToken)
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

    var user = await HttpService.GetTimetrackerUser(opts.TimetrackerUrl, opts.TimetrackerBearerToken, cancellationToken);

    Console.WriteLine("User ID obtained successfully.");

    Console.WriteLine("Creating config file...");

    ConfigService.SaveConfig(opts, user.Data.User.Id);

    Console.WriteLine("Config file created.");

    Console.WriteLine("Creating activities...");

    await ActivityService.SeedActivities(cancellationToken);

    Console.WriteLine("Activities file created.");

    return 0;
}

static async Task<int> DeleteAction(DeleteOptions opts, CancellationToken cancellationToken)
{
    if (!ConfigService.ConfigExists())
    {
        ConsoleHelper.WriteError("Configuration not found. Please run the 'config' command first.");
        return 1;
    }

    await HttpService.DeleteWorkLog(opts.WorkLogId, cancellationToken);

    ConsoleHelper.WriteSuccess($"Time entry '{opts.WorkLogId}' deleted successfully.");

    return 0;
}

static async Task<int> AddActions(AddOptions opts, CancellationToken cancellationToken)
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

    await HttpService.RegisterActivity(opts, activityId, cancellationToken);

    ConsoleHelper.WriteSuccess("Activity successfully created.");

    return 0;
}
