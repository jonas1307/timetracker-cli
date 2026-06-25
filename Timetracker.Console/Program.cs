using CommandLine;
using Newtonsoft.Json;
using Spectre.Console;
using Timetracker.Options;
using Timetracker.Requests;
using Timetracker.Services;
using Timetracker.Utils;
using Timetracker.Validators;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    return await Parser.Default.ParseArguments<ConfigOptions, ActivitiesOptions, AddOptions, ListOptions, DeleteOptions, UpdateOptions, CopyOptions, ImportOptions, InteractiveOptions>(args)
        .MapResult(
            async (ConfigOptions opts) => await ConfigAction(opts, cts.Token),
            async (AddOptions opts) => await AddActions(opts, cts.Token),
            async (ActivitiesOptions opts) => await ActivitiesAction(opts, cts.Token),
            async (ListOptions opts) => await ListActions(opts, cts.Token),
            async (DeleteOptions opts) => await DeleteAction(opts, cts.Token),
            async (UpdateOptions opts) => await UpdateAction(opts, cts.Token),
            async (CopyOptions opts) => await CopyAction(opts, cts.Token),
            async (ImportOptions opts) => await ImportAction(opts, cts.Token),
            async (InteractiveOptions opts) => await InteractiveAction(opts, cts.Token),
            errs => Task.FromResult(1)
        );
}
catch (OperationCanceledException)
{
    ConsoleHelper.WriteWarning(ConsoleHelper.OperationCancelled);
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
        ConsoleHelper.WriteError(ConsoleHelper.ConfigNotFound);
        return 1;
    }

    if (opts.SyncActivities)
    {
        Console.WriteLine("Synchronizing activities...");

        await ActivityService.SeedActivities(cancellationToken);
    }

    var activities = ActivityService.GetActivities();

    if (activities is null || activities.Count == 0)
    {
        ConsoleHelper.WriteWarning("No activities found. Run 'activities --sync' to fetch them from the server.");
        return 0;
    }

    Console.WriteLine("Available activities:");

    foreach (var item in activities)
    {
        Console.WriteLine($"  {item.Name}");
    }

    return 0;
}

async Task<int> ConfigAction(ConfigOptions opts, CancellationToken cancellationToken)
{
    if (opts.Show)
    {
        if (!ConfigService.ConfigExists())
        {
            ConsoleHelper.WriteError(ConsoleHelper.ConfigNotFound);
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
        ConsoleHelper.WriteError(ConsoleHelper.ConfigNotFound);
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
            ConsoleHelper.WriteWarning(ConsoleHelper.OperationCancelled);
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
        ConsoleHelper.WriteError(ConsoleHelper.ConfigNotFound);
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
        ConsoleHelper.WriteError(ConsoleHelper.ConfigNotFound);
        return 1;
    }

    DateTime from, to;

    var periodFlags = new[] { opts.Today, opts.Yesterday, opts.Week, opts.LastWeek, opts.Month, opts.LastMonth, !string.IsNullOrEmpty(opts.Period) }.Count(x => x);
    if (periodFlags > 1)
    {
        ConsoleHelper.WriteError("--today, --yesterday, --week, --last-week, --month, --last-month and --period are mutually exclusive.");
        return 1;
    }

    if ((opts.Today || opts.Yesterday || opts.Week || opts.LastWeek || opts.Month || opts.LastMonth) && (!string.IsNullOrEmpty(opts.From) || !string.IsNullOrEmpty(opts.To)))
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
    else if (opts.Month)
    {
        (from, to) = ValidationUtils.ResolveCurrentMonth();
    }
    else if (opts.LastMonth)
    {
        (from, to) = ValidationUtils.ResolveLastMonth();
    }
    else if (!string.IsNullOrEmpty(opts.Period))
    {
        if (!string.IsNullOrEmpty(opts.From) || !string.IsNullOrEmpty(opts.To))
        {
            ConsoleHelper.WriteError("--period cannot be used together with --from or --to.");
            return 1;
        }

        if (!ValidationUtils.TryResolveMonth(opts.Period, out from, out to))
        {
            ConsoleHelper.WriteError("Invalid period format. Use YYYY/MM (e.g., 2026/06).");
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
        ConsoleHelper.WriteWarning(ConsoleHelper.NoTimeEntries);
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
    Console.WriteLine($"Total: {totalHours}h across {workLogs.Count} {(workLogs.Count == 1 ? "entry" : "entries")}.");

    return 0;
}

static async Task<int> UpdateAction(UpdateOptions opts, CancellationToken cancellationToken)
{
    if (!ConfigService.ConfigExists())
    {
        ConsoleHelper.WriteError(ConsoleHelper.ConfigNotFound);
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
        ConsoleHelper.WriteError(ConsoleHelper.ConfigNotFound);
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
        ConsoleHelper.WriteError(ConsoleHelper.ConfigNotFound);
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

static async Task<int> InteractiveAction(InteractiveOptions opts, CancellationToken ct)
{
    if (!ConfigService.ConfigExists())
    {
        ConsoleHelper.WriteError(ConsoleHelper.ConfigNotFound);
        return 1;
    }

    DateTime from, to;

    if (opts.Yesterday)
        from = to = DateTime.Today.AddDays(-1);
    else if (opts.Week)
        (from, to) = ValidationUtils.ResolveCurrentWeek();
    else if (opts.LastWeek)
        (from, to) = ValidationUtils.ResolveLastWeek();
    else if (opts.Month)
        (from, to) = ValidationUtils.ResolveCurrentMonth();
    else if (opts.LastMonth)
        (from, to) = ValidationUtils.ResolveLastMonth();
    else if (!string.IsNullOrEmpty(opts.Period))
    {
        if (!ValidationUtils.TryResolveMonth(opts.Period, out from, out to))
        {
            ConsoleHelper.WriteError("Invalid period format. Use YYYY/MM (e.g., 2026/06).");
            return 1;
        }
    }
    else
        from = to = DateTime.Today;

    var config = ConfigService.LoadConfig();
    var activities = ActivityService.GetActivities();

    while (true)
    {
        var response = await HttpService.ListWorkLogs(from, to, opts.WorkItemId, ct);
        var logs = response.Data?.OrderBy(x => x.TimeStamp).ToList() ?? [];

        if (logs.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(ConsoleHelper.NoTimeEntries)}[/]");
            return 0;
        }

        var choices = logs
            .Select(l => Markup.Escape($"{l.TimeStamp:yyyy/MM/dd HH:mm}  #{l.WorkItemId,-7}  {Math.Round(l.Length / 3600m, 2),4}h  {l.ActivityType?.Name ?? "-",-18}  {Truncate(l.Comment ?? "-", 35)}"))
            .ToList();
        choices.Add("── New entry ──");
        choices.Add("── Exit ──");

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold]Entries from [green]{from:yyyy/MM/dd}[/] to [green]{to:yyyy/MM/dd}[/][/] — use arrows to navigate, Enter to select:")
                .PageSize(15)
                .AddChoices(choices));

        if (selected == "── Exit ──")
            return 0;

        if (selected == "── New entry ──")
        {
            var newDateStr = AnsiConsole.Prompt(
                new TextPrompt<string>("Date (YYYY/MM/DD):")
                    .DefaultValue(from.ToString("yyyy/MM/dd")));

            var newHourStr = AnsiConsole.Prompt(
                new TextPrompt<string>("Start hour (HH:MM):")
                    .DefaultValue("09:00"));

            var newWorkItemId = AnsiConsole.Prompt(
                new TextPrompt<int>("Work Item ID:"));

            var newHoursStr = AnsiConsole.Prompt(
                new TextPrompt<string>("Duration in hours (e.g. 1 or 1.5):")
                    .Validate(v => decimal.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _)
                        ? ValidationResult.Success()
                        : ValidationResult.Error("Enter a valid number, e.g. 1 or 1.5")));
            var newHours = decimal.Parse(newHoursStr, System.Globalization.CultureInfo.InvariantCulture);

            var newActivity = AnsiConsole.Prompt(
                new SelectionPrompt<Activity>()
                    .Title("Activity type:")
                    .UseConverter(a => Markup.Escape(a.Name))
                    .AddChoices(activities));

            var newComment = AnsiConsole.Prompt(
                new TextPrompt<string>("Comment:")
                    .AllowEmpty());

            if (!DateTime.TryParse(newDateStr, out var newDate))
            {
                ConsoleHelper.WriteError("Invalid date.");
                continue;
            }

            if (!TimeSpan.TryParse(newHourStr, out var newTime))
            {
                ConsoleHelper.WriteError("Invalid start hour.");
                continue;
            }

            var newEntry = new TimetrackerWorklogRequest
            {
                TimeStamp = newDate.Add(newTime),
                Length = (int)Math.Round(newHours * 3600),
                BillableLength = null,
                WorkItemId = newWorkItemId,
                Comment = newComment,
                UserId = config.TimetrackerUserId,
                ActivityTypeId = newActivity.Id
            };

            await HttpService.PostWorkLog(newEntry, ct);
            AnsiConsole.MarkupLine("[green]Entry created.[/]");
            continue;
        }

        var log = logs[choices.IndexOf(selected)];

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold]{Markup.Escape(selected.Trim())}[/]")
                .AddChoices("Edit", "Copy", "Delete", "Back"));

        if (action == "Back")
            continue;

        if (action == "Copy")
        {
            var copyDateStr = AnsiConsole.Prompt(
                new TextPrompt<string>("Date (YYYY/MM/DD):")
                    .DefaultValue(DateTime.Today.ToString("yyyy/MM/dd")));

            var copyHourStr = AnsiConsole.Prompt(
                new TextPrompt<string>("Start hour (HH:MM):")
                    .DefaultValue(log.TimeStamp.ToString("HH:mm")));

            var copyWorkItemId = AnsiConsole.Prompt(
                new TextPrompt<int>("Work Item ID:")
                    .DefaultValue(log.WorkItemId));

            var copyHoursDefault = Math.Round(log.Length / 3600m, 2).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var copyHoursStr = AnsiConsole.Prompt(
                new TextPrompt<string>("Duration in hours (e.g. 1 or 1.5):")
                    .DefaultValue(copyHoursDefault)
                    .Validate(v => decimal.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _)
                        ? ValidationResult.Success()
                        : ValidationResult.Error("Enter a valid number, e.g. 1 or 1.5")));
            var copyHours = decimal.Parse(copyHoursStr, System.Globalization.CultureInfo.InvariantCulture);

            Activity copyActivity;
            var copyCurrentActivityName = log.ActivityType?.Name ?? "none";
            if (log.ActivityType == null || AnsiConsole.Confirm($"Change activity type? (current: {Markup.Escape(copyCurrentActivityName)})", defaultValue: false))
            {
                copyActivity = AnsiConsole.Prompt(
                    new SelectionPrompt<Activity>()
                        .Title("Activity type:")
                        .UseConverter(a => Markup.Escape(a.Name))
                        .AddChoices(activities));
            }
            else
            {
                copyActivity = activities.FirstOrDefault(a => a.Id == log.ActivityType.Id) ?? activities.First();
            }

            var copyComment = AnsiConsole.Prompt(
                new TextPrompt<string>("Comment:")
                    .AllowEmpty()
                    .DefaultValue(log.Comment ?? string.Empty));

            if (!DateTime.TryParse(copyDateStr, out var copyDate))
            {
                ConsoleHelper.WriteError("Invalid date.");
                continue;
            }

            if (!TimeSpan.TryParse(copyHourStr, out var copyTime))
            {
                ConsoleHelper.WriteError("Invalid start hour.");
                continue;
            }

            var copy = new TimetrackerWorklogRequest
            {
                TimeStamp = copyDate.Add(copyTime),
                Length = (int)Math.Round(copyHours * 3600),
                BillableLength = null,
                WorkItemId = copyWorkItemId,
                Comment = copyComment,
                UserId = config.TimetrackerUserId,
                ActivityTypeId = copyActivity.Id
            };

            await HttpService.PostWorkLog(copy, ct);
            AnsiConsole.MarkupLine("[green]Entry copied.[/]");
            continue;
        }

        if (action == "Delete")
        {
            var confirmed = AnsiConsole.Confirm($"Delete entry [red]{log.Id}[/]?", defaultValue: false);
            if (!confirmed) continue;

            await HttpService.DeleteWorkLog(log.Id, ct);
            AnsiConsole.MarkupLine("[green]Entry deleted.[/]");
            continue;
        }

        // Edit
        var dateStr = AnsiConsole.Prompt(
            new TextPrompt<string>("Date (YYYY/MM/DD):")
                .DefaultValue(log.TimeStamp.ToString("yyyy/MM/dd")));

        var hourStr = AnsiConsole.Prompt(
            new TextPrompt<string>("Start hour (HH:MM):")
                .DefaultValue(log.TimeStamp.ToString("HH:mm")));

        var workItemId = AnsiConsole.Prompt(
            new TextPrompt<int>("Work Item ID:")
                .DefaultValue(log.WorkItemId));

        var hoursDefault = Math.Round(log.Length / 3600m, 2).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var hoursStr = AnsiConsole.Prompt(
            new TextPrompt<string>("Duration in hours (e.g. 1 or 1.5):")
                .DefaultValue(hoursDefault)
                .Validate(v => decimal.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _)
                    ? ValidationResult.Success()
                    : ValidationResult.Error("Enter a valid number, e.g. 1 or 1.5")));
        var hours = decimal.Parse(hoursStr, System.Globalization.CultureInfo.InvariantCulture);

        Activity activity;
        var currentActivityName = log.ActivityType?.Name ?? "none";
        if (log.ActivityType == null || AnsiConsole.Confirm($"Change activity type? (current: {Markup.Escape(currentActivityName)})", defaultValue: false))
        {
            activity = AnsiConsole.Prompt(
                new SelectionPrompt<Activity>()
                    .Title("Activity type:")
                    .UseConverter(a => Markup.Escape(a.Name))
                    .AddChoices(activities));
        }
        else
        {
            activity = activities.FirstOrDefault(a => a.Id == log.ActivityType.Id) ?? activities.First();
        }

        var comment = AnsiConsole.Prompt(
            new TextPrompt<string>("Comment:")
                .AllowEmpty()
                .DefaultValue(log.Comment ?? string.Empty));

        if (!DateTime.TryParse(dateStr, out var date))
        {
            ConsoleHelper.WriteError("Invalid date.");
            continue;
        }

        if (!TimeSpan.TryParse(hourStr, out var time))
        {
            ConsoleHelper.WriteError("Invalid start hour.");
            continue;
        }

        var updated = new TimetrackerWorklogRequest
        {
            TimeStamp = date.Add(time),
            Length = (int)Math.Round(hours * 3600),
            BillableLength = null,
            WorkItemId = workItemId,
            Comment = comment,
            UserId = config.TimetrackerUserId,
            ActivityTypeId = activity.Id
        };

        await HttpService.UpdateWorkLog(log.Id, updated, ct);
        AnsiConsole.MarkupLine("[green]Entry updated.[/]");
    }
}

static string Truncate(string value, int max) =>
    value.Length > max ? value[..(max - 1)] + "…" : value;
