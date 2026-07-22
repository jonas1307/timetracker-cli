using CommandLine;

namespace Timetracker.Options;

[Verb("summary", HelpText = "Show a daily summary of logged hours for a given period.")]
public class SummaryOptions : IPeriodOptions
{
    [Option('f', "from", Required = false, HelpText = "Start date in the format YYYY/MM/DD, 'today' or 'yesterday'. Defaults to today.")]
    public string From { get; set; }

    [Option('t', "to", Required = false, HelpText = "End date in the format YYYY/MM/DD, 'today' or 'yesterday'. Defaults to today.")]
    public string To { get; set; }

    [Option('p', "period", Required = false, HelpText = "Summarize a specific month in the format YYYY/MM (e.g., 2026/06). Cannot be used with --from, --to or week/month shortcuts.")]
    public string Period { get; set; }

    [Option("month", Required = false, HelpText = "Summarize the current month. Cannot be used with --from, --to, --period or other shortcuts.")]
    public bool Month { get; set; }

    [Option("last-month", Required = false, HelpText = "Summarize the previous month. Cannot be used with --from, --to, --period or other shortcuts.")]
    public bool LastMonth { get; set; }

    [Option("today", Required = false, HelpText = "Summarize today. Cannot be used with --from, --to or other shortcuts.")]
    public bool Today { get; set; }

    [Option("yesterday", Required = false, HelpText = "Summarize yesterday. Cannot be used with --from, --to or other shortcuts.")]
    public bool Yesterday { get; set; }

    [Option("week", Required = false, HelpText = "Summarize the current week (Monday to Sunday). Cannot be used with --from, --to or --period.")]
    public bool Week { get; set; }

    [Option("last-week", Required = false, HelpText = "Summarize the previous week (Monday to Sunday). Cannot be used with --from, --to or --period.")]
    public bool LastWeek { get; set; }

    [Option('w', "work-item", Required = false, HelpText = "Filter entries by Work Item ID.")]
    public int? WorkItemId { get; set; }
}
