using CommandLine;
using Newtonsoft.Json;
using Timetracker.Options;
using Timetracker.Requests;
using Timetracker.Services;
using Timetracker.Utils;
using Timetracker.Validators;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    return await Parser.Default.ParseArguments<ConfigOptions, ActivitiesOptions, AddOptions, ListOptions, DeleteOptions, UpdateOptions, CopyOptions, ImportOptions>(args)
        .MapResult(
            async (ConfigOptions opts) => await ConfigAction(opts, cts.Token),
            async (AddOptions opts) => await AddActions(opts, cts.Token),
            async (ActivitiesOptions opts) => await ActivitiesAction(opts, cts.Token),
            async (ListOptions opts) => await ListActions(opts, cts.Token),
            async (DeleteOptions opts) => await DeleteAction(opts, cts.Token),
            async (UpdateOptions opts) => await UpdateAction(opts, cts.Token),
            async (CopyOptions opts) => await CopyAction(opts, cts.Token),
            async (ImportOptions opts) => await ImportAction(opts, cts.Token),
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

async Task<int> ActivitiesAction(ActivitiesOptions opts, CancellationToken cancellationToken)
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

        Console.WriteLine($"Logged in as: {config.DisplayName} ({config.Email}) · {config.AccountName}");
        Console.WriteLine();
        Console.WriteLine($"URL:    {config.TimetrackerUrl}");
        Console.WriteLine($"Token:  {maskedToken}");
        Console.WriteLine($"UserId: {config.TimetrackerUserId}");

        return 0;
    }

    if (opts.Reset)
    {
        ConfigService.DeleteConfig();
        ActivityService.DeleteActivities();
        
        ConsoleHelper.WriteSuccess("Configuration reset successfully. Run 'config --url <url> --token <token>' to reconfigure.");
        
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

    Console.WriteLine("Obtaining user info...");

    var user = await HttpService.GetTimetrackerUser(opts.TimetrackerUrl, opts.TimetrackerBearerToken, cancellationToken);

    Console.WriteLine("User info obtained successfully.");

    Console.WriteLine("Creating config file...");

    ConfigService.SaveConfig(
        opts,
        user.Data.User.Id,
        user.Data.User.DisplayName,
        user.Data.User.Email,
        user.Data.Account.Name
    );

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

    var ids = opts.WorkLogIds.ToList();

    if (!opts.Force)
    {
        if (ids.Count == 1)
        {
            Console.Write($"Delete time entry '{ids[0]}'? [y/N] ");
        }
        else
        {
            Console.WriteLine($"Delete {ids.Count} time entries?");
            foreach (var id in ids)
                Console.WriteLine($"  - {id}");
            Console.Write("[y/N] ");
        }

        var answer = Console.ReadLine();
        if (!string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Cancelled.");
            return 0;
        }
    }

    foreach (var id in ids)
    {
        await HttpService.DeleteWorkLog(id, cancellationToken);
        Console.WriteLine($"Deleted: {id}");
    }

    Console.WriteLine();
    ConsoleHelper.WriteSuccess($"{ids.Count} {(ids.Count == 1 ? "entry" : "entries")} deleted successfully.");

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

    var periodFlags = new[] { opts.Today, opts.Yesterday, opts.Week, opts.LastWeek, opts.ThisMonth, opts.LastMonth, !string.IsNullOrEmpty(opts.Month) }.Count(x => x);
    if (periodFlags > 1)
    {
        ConsoleHelper.WriteError("--today, --yesterday, --week, --last-week, --this-month, --last-month and --month are mutually exclusive.");
        return 1;
    }

    if ((opts.Today || opts.Yesterday || opts.Week || opts.LastWeek || opts.ThisMonth || opts.LastMonth) && (!string.IsNullOrEmpty(opts.From) || !string.IsNullOrEmpty(opts.To)))
    {
        ConsoleHelper.WriteError("Period shortcuts cannot be used together with --from or --to.");
        return 1;
    }

    if (opts.Today)
    {
        from = to = DateTime.Today;
    }
    else if (opts.Yesterday)
    {
        from = to = DateTime.Today.AddDays(-1);
    }
    else if (opts.Week)
    {
        (from, to) = ValidationUtils.ResolveCurrentWeek();
    }
    else if (opts.LastWeek)
    {
        (from, to) = ValidationUtils.ResolveLastWeek();
    }
    else if (opts.ThisMonth)
    {
        (from, to) = ValidationUtils.ResolveCurrentMonth();
    }
    else if (opts.LastMonth)
    {
        (from, to) = ValidationUtils.ResolveLastMonth();
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

    var result = await HttpService.ListWorkLogs(from, to, opts.WorkItemId, cancellationToken);
    var workLogs = result.Data;

    if (workLogs is null || workLogs.Count == 0)
    {
        Console.WriteLine("No time entries found for the specified period.");
        return 0;
    }

    if (string.Equals(opts.Output, "json", StringComparison.OrdinalIgnoreCase))
    {
        var config = ConfigService.LoadConfig();
        var batch = workLogs.OrderBy(x => x.TimeStamp).Select(log => new TimetrackerWorklogRequest
        {
            TimeStamp = log.TimeStamp,
            Length = log.Length,
            BillableLength = null,
            WorkItemId = log.WorkItemId,
            Comment = log.Comment,
            UserId = config.TimetrackerUserId,
            ActivityTypeId = log.ActivityType?.Id
        });

        Console.WriteLine(JsonConvert.SerializeObject(batch, Formatting.Indented));

        return 0;
    }

    if (string.Equals(opts.Output, "csv", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("id,date,time,work_item_id,hours,activity_type,comment");

        foreach (var log in workLogs.OrderBy(x => x.TimeStamp))
        {
            var hours = Math.Round(log.Length / 3600m, 2);
            var type = log.ActivityType?.Name ?? string.Empty;
            var comment = log.Comment ?? string.Empty;

            Console.WriteLine($"{log.Id},{log.TimeStamp:yyyy/MM/dd},{log.TimeStamp:HH:mm},{log.WorkItemId},{hours},{EscapeCsv(type)},{EscapeCsv(comment)}");
        }

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
            Console.WriteLine($"  {day.Key:yyyy/MM/dd} | {day.Key.DayOfWeek,9} | {dayHours,4}h | {count} {(count == 1 ? "entry" : "entries")}");
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
            var comment = string.IsNullOrEmpty(log.Comment) ? "-" : (log.Comment.Length > 30 ? log.Comment[..30] + "..." : log.Comment);
            var lastColumn = opts.ShowIds ? log.Id : comment;

            Console.WriteLine($"  {log.TimeStamp:yyyy/MM/dd HH:mm} | {log.TimeStamp.DayOfWeek,9} | {log.WorkItemId,-7} | {hours,4}h | {type,-20} | {lastColumn}");
        }
    }

    Console.WriteLine();
    ConsoleHelper.WriteSuccess($"Total: {totalHours}h across {workLogs.Count} {(workLogs.Count == 1 ? "entry" : "entries")}.");

    return 0;
}

static async Task<int> UpdateAction(UpdateOptions opts, CancellationToken cancellationToken)
{
    if (!ConfigService.ConfigExists())
    {
        ConsoleHelper.WriteError("Configuration not found. Please run the 'config' command first.");
        return 1;
    }

    var activities = ActivityService.GetActivities();
    var validator = new UpdateValidator(activities.Select(x => x.Name.ToUpper()));
    var result = validator.Validate(opts);

    if (!result.IsValid)
    {
        foreach (var error in result.Errors)
        {
            ConsoleHelper.WriteError($"- {error.ErrorMessage}");
        }

        return 1;
    }

    var existing = await HttpService.GetWorkLog(opts.WorkLogId, cancellationToken);

    var date = opts.ActivityDate != null
        ? ValidationUtils.ResolveDate(opts.ActivityDate)
        : existing.TimeStamp.Date;

    var time = opts.ActivityStartHour != null
        ? TimeSpan.Parse(opts.ActivityStartHour)
        : existing.TimeStamp.TimeOfDay;

    var activityTypeId = opts.ActivityType != null
        ? ActivityService.GetActivityId(opts.ActivityType, activities)
        : existing.ActivityType.Id;

    var config = ConfigService.LoadConfig();

    var updated = new TimetrackerWorklogRequest
    {
        TimeStamp = date.Add(time),
        Length = opts.ActivityLength.HasValue ? (int)Math.Round(opts.ActivityLength.Value * 3600) : existing.Length,
        BillableLength = null,
        WorkItemId = opts.WorkItemId ?? existing.WorkItemId,
        Comment = opts.ActivityComment ?? existing.Comment,
        UserId = config.TimetrackerUserId,
        ActivityTypeId = activityTypeId
    };

    await HttpService.UpdateWorkLog(opts.WorkLogId, updated, cancellationToken);

    ConsoleHelper.WriteSuccess($"Time entry '{opts.WorkLogId}' updated successfully.");

    return 0;
}

static async Task<int> CopyAction(CopyOptions opts, CancellationToken cancellationToken)
{
    if (!ConfigService.ConfigExists())
    {
        ConsoleHelper.WriteError("Configuration not found. Please run the 'config' command first.");
        return 1;
    }

    if (opts.ActivityDate != null && !ValidationUtils.ValidActivityDate(opts.ActivityDate))
    {
        ConsoleHelper.WriteError("Invalid date. Use YYYY/MM/DD, 'today' or 'yesterday'.");
        return 1;
    }

    var source = await HttpService.GetWorkLog(opts.WorkLogId, cancellationToken);
    var targetDate = ValidationUtils.ResolveDate(opts.ActivityDate);
    var config = ConfigService.LoadConfig();

    var copy = new TimetrackerWorklogRequest
    {
        TimeStamp = targetDate.Add(source.TimeStamp.TimeOfDay),
        Length = source.Length,
        BillableLength = null,
        WorkItemId = source.WorkItemId,
        Comment = source.Comment,
        UserId = config.TimetrackerUserId,
        ActivityTypeId = source.ActivityType.Id
    };

    var createdId = await HttpService.PostWorkLog(copy, cancellationToken);

    ConsoleHelper.WriteSuccess($"Time entry copied successfully. New ID: {createdId}");

    return 0;
}

static async Task<int> ImportAction(ImportOptions opts, CancellationToken cancellationToken)
{
    if (!ConfigService.ConfigExists())
    {
        ConsoleHelper.WriteError("Configuration not found. Please run the 'config' command first.");
        return 1;
    }

    if (!File.Exists(opts.FilePath))
    {
        ConsoleHelper.WriteError($"File not found: {opts.FilePath}");
        return 1;
    }

    List<TimetrackerWorklogRequest> entries;

    try
    {
        var json = await File.ReadAllTextAsync(opts.FilePath, cancellationToken);
        entries = JsonConvert.DeserializeObject<List<TimetrackerWorklogRequest>>(json);
    }
    catch (JsonException ex)
    {
        ConsoleHelper.WriteError($"Invalid JSON file: {ex.Message}");
        return 1;
    }

    if (entries is null || entries.Count == 0)
    {
        ConsoleHelper.WriteError("The file contains no entries to import.");
        return 1;
    }

    if (opts.DryRun)
    {
        Console.WriteLine($"[Dry run] {entries.Count} {(entries.Count == 1 ? "entry" : "entries")} would be imported:");
        Console.WriteLine();
        foreach (var entry in entries.OrderBy(x => x.TimeStamp))
        {
            var hours = Math.Round(entry.Length / 3600m, 2);
            Console.WriteLine($"  {entry.TimeStamp:yyyy/MM/dd HH:mm} | {entry.WorkItemId,-7} | {hours,4}h | {(string.IsNullOrEmpty(entry.Comment) ? "-" : entry.Comment)}");
        }
        Console.WriteLine();
        ConsoleHelper.WriteSuccess($"Dry run complete. No entries were submitted.");
        return 0;
    }

    Console.WriteLine($"Importing {entries.Count} {(entries.Count == 1 ? "entry" : "entries")}...");
    var created = await HttpService.ImportWorkLogs(entries, cancellationToken);
    Console.WriteLine();
    ConsoleHelper.WriteSuccess($"Successfully imported {created.Count} {(created.Count == 1 ? "entry" : "entries")}.");

    return 0;
}

static string EscapeCsv(string value)
{
    if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        return $"\"{value.Replace("\"", "\"\"")}\"";
    return value;
}
