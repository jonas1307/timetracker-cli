using CommandLine;

namespace Timetracker.Options;

[Verb("activities", HelpText = "Display a list of available activity types.")]
public class ActivitiesOptions
{
    [Option("sync", Required = false, HelpText = "Synchronize activity types with the server before displaying the list.")]
    public bool SyncActivities { get; set; }
}
