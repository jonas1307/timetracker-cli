using CommandLine;

namespace Timetracker.Options;

[Verb("update", HelpText = "Update an existing time entry.")]
public class UpdateOptions
{
    [Option('i', "id", Required = true, HelpText = "ID of the time entry to update.")]
    public string WorkLogId { get; set; }

    [Option('d', "date", Required = false, HelpText = "New date in the format YYYY/MM/DD, 'today' or 'yesterday'.")]
    public string ActivityDate { get; set; }

    [Option('w', "workitem", Required = false, HelpText = "New Work Item ID.")]
    public int? WorkItemId { get; set; }

    [Option('l', "length", Required = false, HelpText = "New duration in hours (e.g., 0.5 for half an hour).")]
    public decimal? ActivityLength { get; set; }

    [Option('t', "type", Required = false, HelpText = "New activity type. Use the 'activity-type' command to list available types.")]
    public string ActivityType { get; set; }

    [Option('c', "comment", Required = false, HelpText = "New comment.")]
    public string ActivityComment { get; set; }

    [Option('h', "hour", Required = false, HelpText = "New start time in the format HH:MM (e.g., 09:00).")]
    public string ActivityStartHour { get; set; }
}
