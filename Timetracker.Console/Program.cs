using CommandLine;
using Timetracker.Options;
using Timetracker.Services;
using Timetracker.Utils;
using Timetracker.Validators;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    return await Parser.Default.ParseArguments<ConfigOptions, ActivityTypeOptions, AddOptions, ListOptions>(args)
        .MapResult(
            async (ConfigOptions opts) => await ConfigAction(opts, cts.Token),
            async (AddOptions opts) => await AddActions(opts, cts.Token),
            async (ActivityTypeOptions opts) => await ActivityTypeAction(opts, cts.Token),
            async (ListOptions opts) => await ListActions(opts, cts.Token),
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

static async Task<int> ListActions(ListOptions opts, CancellationToken cancellationToken)
{
    if (!ConfigService.ConfigExists())
    {
        ConsoleHelper.WriteError("Configuration not found. Please run the 'config' command first.");
        return 1;
    }

    var from = ValidationUtils.ResolveDate(opts.From);
    var to = ValidationUtils.ResolveDate(opts.To);

    if (from > to)
    {
        ConsoleHelper.WriteError("The 'from' date must be earlier than or equal to the 'to' date.");
        return 1;
    }

    var result = await HttpService.ListWorkLogs(from, to, cancellationToken);
    var workLogs = result.Data;

    if (workLogs is null || workLogs.Count == 0)
    {
        Console.WriteLine("No time entries found for the specified period.");
        return 0;
    }

    Console.WriteLine($"Time entries from {from:yyyy/MM/dd} to {to:yyyy/MM/dd}:");
    Console.WriteLine();

    foreach (var log in workLogs.OrderBy(x => x.TimeStamp))
    {
        var hours = Math.Round(log.Length / 3600m, 2);
        var type = log.ActivityType?.Name ?? "-";
        var comment = string.IsNullOrEmpty(log.Comment) ? "-" : log.Comment;

        Console.WriteLine($"  {log.TimeStamp:yyyy/MM/dd HH:mm}  |  WI: {log.WorkItemId,-8}  |  {hours,5}h  |  {type,-20}  |  {comment}");
    }

    Console.WriteLine();

    var totalHours = Math.Round(workLogs.Sum(x => x.Length) / 3600m, 2);
    ConsoleHelper.WriteSuccess($"Total: {totalHours}h across {workLogs.Count} {(workLogs.Count == 1 ? "entry" : "entries")}.");

    return 0;
}
