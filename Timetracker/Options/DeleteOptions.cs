using CommandLine;

namespace Timetracker.Options;

[Verb("delete", HelpText = "Delete one or more time entries by ID.")]
public class DeleteOptions
{
    [Option('i', "id", Required = true, Separator = ',', HelpText = "ID(s) of the time entries to delete. Repeat the flag or separate with commas: -i id1 -i id2 or -i id1,id2.")]
    public IEnumerable<string> WorkLogIds { get; set; }

    [Option("force", Required = false, HelpText = "Skip the confirmation prompt before deleting.")]
    public bool Force { get; set; }
}
