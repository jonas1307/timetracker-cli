using CommandLine;
using Timetracker.Options;
using Timetracker.Services;
using Timetracker.Utils;
using Timetracker.Validators;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    return await Parser.Default.ParseArguments<ConfigOptions, ActivityTypeOptions, AddOptions, ListOptions, DeleteOptions>(args)
        .MapResult(
            async (ConfigOptions opts) => await ConfigAction(opts, cts.Token),
            async (AddOptions opts) => await AddActions(opts, cts.Token),
            async (ActivityTypeOptions opts) => await ActivityTypeAction(opts, cts.Token),
            async (ListOptions opts) => await ListActions(opts, cts.Token),
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
    if (opts.Show)
    {
        if (!ConfigService.ConfigExists())
        {
            ConsoleHelper.WriteError("Configuration not found. Please run the 'config' command first.");
            return 1;
        }

        var config = ConfigService.LoadConfig();
        var maskedToken = config.TimetrackerBearerToken.Length > 8
            ? $"{config.TimetrackerBearerToken[..4]}{"*".PadRight(config.TimetrackerBearerToken.Length - 8, '*')}{config.TimetrackerBearerToken[^4..]}"
            : "****";

        Console.WriteLine($"URL:    {config.TimetrackerUrl}");
        Console.WriteLine($"Token:  {maskedToken}");
        Console.WriteLine($"UserId: {config.TimetrackerUserId}");

        return 0;
    }

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
    var resolvedDate = ValidationUtils.ResolveDate(opts.ActivityDate);

    if (opts.DryRun)
    {
        Console.WriteLine("[Dry run] Entry that would be submitted:");
        Console.WriteLine();
        Console.WriteLine($"  Date:        {resolvedDate:yyyy/MM/dd} {opts.ActivityStartHour}");
        Console.WriteLine($"  Work Item:   {opts.WorkItemId}");
        Console.WriteLine($"  Duration:    {opts.ActivityLength}h ({(int)Math.Round(opts.ActivityLength * 3600)}s)");
        Console.WriteLine($"  Type:        {opts.ActivityType}");
        Console.WriteLine($"  Comment:     {(string.IsNullOrEmpty(opts.ActivityComment) ? "-" : opts.ActivityComment)}");
        Console.WriteLine();
        ConsoleHelper.WriteSuccess("Dry run complete. No entry was submitted.");
        return 0;
    }

    var createdId = await HttpService.RegisterActivity(opts, activityId, cancellationToken);

    ConsoleHelper.WriteSuccess($"Activity successfully created. ID: {createdId}");

    return 0;
}

static async Task<int> ListActions(ListOptions opts, CancellationToken cancellationToken)
{
    if (!ConfigService.ConfigExists())
    {
        ConsoleHelper.WriteError("Configuration not found. Please run the 'config' command first.");
        return 1;
    }

    DateTime from, to;

    if (opts.Week && !string.IsNullOrEmpty(opts.Month))
    {
        ConsoleHelper.WriteError("--week cannot be used together with --month.");
        return 1;
    }

    if (opts.Week)
    {
        if (!string.IsNullOrEmpty(opts.From) || !string.IsNullOrEmpty(opts.To))
        {
            ConsoleHelper.WriteError("--week cannot be used together with --from or --to.");
            return 1;
        }

        (from, to) = ValidationUtils.ResolveCurrentWeek();
    }
    else if (!string.IsNullOrEmpty(opts.Month))
    {
        if (!string.IsNullOrEmpty(opts.From) || !string.IsNullOrEmpty(opts.To))
        {
            ConsoleHelper.WriteError("--month cannot be used together with --from or --to.");
            return 1;
        }

        if (!ValidationUtils.TryResolveMonth(opts.Month, out from, out to))
        {
            ConsoleHelper.WriteError("Invalid month format. Use YYYY/MM (e.g., 2026/06).");
            return 1;
        }
    }
    else
    {
        from = ValidationUtils.ResolveDate(opts.From);
        to = ValidationUtils.ResolveDate(opts.To);

        if (from > to)
        {
            ConsoleHelper.WriteError("The 'from' date must be earlier than or equal to the 'to' date.");
            return 1;
        }
    }

    var result = await HttpService.ListWorkLogs(from, to, cancellationToken);
    var workLogs = result.Data;

    if (workLogs is null || workLogs.Count == 0)
    {
        Console.WriteLine("No time entries found for the specified period.");
        return 0;
    }

    var totalHours = Math.Round(workLogs.Sum(x => x.Length) / 3600m, 2);

    if (opts.Summary)
    {
        Console.WriteLine($"Summary from {from:yyyy/MM/dd} to {to:yyyy/MM/dd}:");
        Console.WriteLine();

        var byDay = workLogs
            .GroupBy(x => x.TimeStamp.Date)
            .OrderBy(g => g.Key);

        foreach (var day in byDay)
        {
            var dayHours = Math.Round(day.Sum(x => x.Length) / 3600m, 2);
            var count = day.Count();
            Console.WriteLine($"  {day.Key:yyyy/MM/dd} | {dayHours,4}h | {count} {(count == 1 ? "entry" : "entries")}");
        }
    }
    else
    {
        Console.WriteLine($"Time entries from {from:yyyy/MM/dd} to {to:yyyy/MM/dd}:");
        Console.WriteLine();

        foreach (var log in workLogs.OrderBy(x => x.TimeStamp))
        {
            var hours = Math.Round(log.Length / 3600m, 2);
            var type = log.ActivityType?.Name ?? "-";
            var lastColumn = opts.ShowIds ? log.Id : (string.IsNullOrEmpty(log.Comment) ? "-" : log.Comment);

            Console.WriteLine($"  {log.TimeStamp:yyyy/MM/dd HH:mm} | {log.WorkItemId,-7} | {hours,4}h | {type,-20} | {lastColumn}");
        }
    }

    Console.WriteLine();
    ConsoleHelper.WriteSuccess($"Total: {totalHours}h across {workLogs.Count} {(workLogs.Count == 1 ? "entry" : "entries")}.");

    return 0;
}
