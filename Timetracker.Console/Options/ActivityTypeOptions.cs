using CommandLine;

namespace Timetracker.Options;

[Verb("activity-type", HelpText = "Display a list of available activity types.")]
public class ActivityTypeOptions
{
    [Option('s', "sync", Required = false, HelpText = "Synchronize activity types with the server before displaying the list.")]
    public bool SyncActivities { get; set; }
}
