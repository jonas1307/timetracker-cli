using CommandLine;

namespace Timetracker.Options;

[Verb("copy", HelpText = "Duplicate an existing time entry to a target date (defaults to today).")]
public class CopyOptions
{
    [Option('i', "id", Required = true, HelpText = "ID of the time entry to copy.")]
    public string WorkLogId { get; set; }

    [Option('d', "date", Required = false, HelpText = "Target date in the format YYYY/MM/DD, 'today' or 'yesterday'. Defaults to today.")]
    public string ActivityDate { get; set; }
}
