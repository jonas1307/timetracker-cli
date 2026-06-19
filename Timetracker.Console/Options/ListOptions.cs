using CommandLine;

namespace Timetracker.Options;

[Verb("list", HelpText = "List time entries for a given period.")]
public class ListOptions
{
    [Option('f', "from", Required = false, HelpText = "Start date in the format YYYY/MM/DD, 'today' or 'yesterday'. Defaults to today.")]
    public string From { get; set; }

    [Option('t', "to", Required = false, HelpText = "End date in the format YYYY/MM/DD, 'today' or 'yesterday'. Defaults to today.")]
    public string To { get; set; }

    [Option('s', "summary", Required = false, HelpText = "Display a daily summary instead of individual entries.")]
    public bool Summary { get; set; }

    [Option('i', "ids", Required = false, HelpText = "Show entry IDs instead of comments. Useful for identifying entries to delete.")]
    public bool ShowIds { get; set; }

    [Option('m', "month", Required = false, HelpText = "Show the monthly summary for a specific month in the format YYYY/MM (e.g., 2026/06). Cannot be used with --from or --to.")]
    public string Month { get; set; }
}
