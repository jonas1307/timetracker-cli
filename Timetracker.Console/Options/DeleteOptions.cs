using CommandLine;

namespace Timetracker.Options;

[Verb("delete", HelpText = "Delete a time entry by its ID.")]
public class DeleteOptions
{
    [Option('i', "id", Required = true, HelpText = "The ID of the time entry to delete.")]
    public string WorkLogId { get; set; }

    [Option("force", Required = false, HelpText = "Skip the confirmation prompt before deleting.")]
    public bool Force { get; set; }
}
