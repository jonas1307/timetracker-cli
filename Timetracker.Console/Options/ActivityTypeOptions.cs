using CommandLine;

namespace Timetracker.Options;

[Verb("activity-type", HelpText = "Lists available activity types.")]
public class ActivityTypeOptions
{
    [Option('s', "sync", Required = false, HelpText = "Synchronize activity types before listing.")]
    public bool SyncActivities { get; set; }
}