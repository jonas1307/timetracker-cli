using CommandLine;

namespace Timetracker.Options;

[Verb("add", HelpText = "Add a new activity directly to the server.")]
public class AddOptions
{
    [Option('d', "date", Required = true, HelpText = "Specify the date for the activity in the format YYYY/MM/DD (e.g., 2025/12/31).")]
    public string ActivityDate { get; set; }

    [Option('w', "workitem", Required = true, HelpText = "Specify the Work Item ID associated with the activity.")]
    public int WorkItemId { get; set; }

    [Option('l', "length", Required = true, HelpText = "Specify the duration of the activity in hours (e.g., 0.5 for half an hour).")]
    public decimal ActivityLength { get; set; }

    [Option('t', "type", Required = true, HelpText = "Specify the type of activity. Use the 'activity-type' command to list available types.")]
    public string ActivityType { get; set; }

    [Option('c', "comment", Required = false, HelpText = "Provide a comment for the activity.")]
    public string ActivityComment { get; set; }

    [Option('h', "hour", Required = false, HelpText = "Specify the start time of the activity in the format HH:MM (e.g., 09:00 or 21:00)", Default = "09:00")]
    public string ActivityStartHour { get; set; }
}

