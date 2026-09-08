using CommandLine;

namespace Timetracker.Options;

[Verb("interactive", HelpText = "Interactively browse, edit and delete time entries.")]
public class InteractiveOptions : IPeriodOptions
{
    [Option('p', "period", Required = false, HelpText = "Specific month in YYYY/MM format (e.g., 2026/06).")]
    public string Period { get; set; }

    // Interactive mode does not support explicit date ranges.
    string IPeriodOptions.From => null;
    string IPeriodOptions.To => null;

    [Option('w', "work-item", Required = false, HelpText = "Filter entries by Work Item ID.")]
    public int? WorkItemId { get; set; }

    [Option("today", Required = false, HelpText = "Show entries for today (default).")]
    public bool Today { get; set; }

    [Option("yesterday", Required = false, HelpText = "Show entries for yesterday.")]
    public bool Yesterday { get; set; }

    [Option("week", Required = false, HelpText = "Show entries for the current week.")]
    public bool Week { get; set; }

    [Option("current-week", Required = false, HelpText = "Alias for --week. Show entries for the current week.")]
    public bool CurrentWeek { get; set; }

    [Option("last-week", Required = false, HelpText = "Show entries for the previous week.")]
    public bool LastWeek { get; set; }

    [Option("month", Required = false, HelpText = "Show entries for the current month.")]
    public bool Month { get; set; }

    [Option("current-month", Required = false, HelpText = "Alias for --month. Show entries for the current month.")]
    public bool CurrentMonth { get; set; }

    [Option("last-month", Required = false, HelpText = "Show entries for the previous month.")]
    public bool LastMonth { get; set; }
}
