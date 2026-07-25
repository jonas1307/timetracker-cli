using CommandLine;

namespace Timetracker.Options;

[Verb("list", HelpText = "List time entries for a given period.")]
public class ListOptions : IPeriodOptions
{
    [Option('f', "from", Required = false, HelpText = "Start date in the format YYYY/MM/DD, 'today' or 'yesterday'. Defaults to today.")]
    public string From { get; set; }

    [Option('t', "to", Required = false, HelpText = "End date in the format YYYY/MM/DD, 'today' or 'yesterday'. Defaults to today.")]
    public string To { get; set; }

    [Option('p', "period", Required = false, HelpText = "Show entries for a specific month in the format YYYY/MM (e.g., 2026/06). Cannot be used with --from, --to or week/month shortcuts.")]
    public string Period { get; set; }

    [Option("month", Required = false, HelpText = "Show entries for the current month. Cannot be used with --from, --to, --period or other shortcuts.")]
    public bool Month { get; set; }

    [Option("last-month", Required = false, HelpText = "Show entries for the previous month. Cannot be used with --from, --to, --period or other shortcuts.")]
    public bool LastMonth { get; set; }

    [Option('w', "work-item", Required = false, HelpText = "Filter entries by Work Item ID.")]
    public int? WorkItemId { get; set; }

    [Option('o', "output", Required = false, HelpText = "Output format. Use 'json' to export entries in batch-upload format.")]
    public string Output { get; set; }

    [Option("ids", Required = false, HelpText = "Show entry IDs instead of comments. Useful for identifying entries to delete.")]
    public bool ShowIds { get; set; }

    [Option("today", Required = false, HelpText = "Show entries for today. Cannot be used with --from, --to, --week, --yesterday, --last-week or --period.")]
    public bool Today { get; set; }

    [Option("yesterday", Required = false, HelpText = "Show entries for yesterday. Cannot be used with --from, --to, --today, --week, --last-week or --period.")]
    public bool Yesterday { get; set; }

    [Option("week", Required = false, HelpText = "Show entries for the current week (Monday to Sunday). Cannot be used with --from, --to or --period.")]
    public bool Week { get; set; }

    [Option("last-week", Required = false, HelpText = "Show entries for the previous week (Monday to Sunday). Cannot be used with --from, --to or --period.")]
    public bool LastWeek { get; set; }
}
