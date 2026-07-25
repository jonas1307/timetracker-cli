using CommandLine;

namespace Timetracker.Options;

[Verb("import", HelpText = "Import multiple time entries from a JSON file.")]
public class ImportOptions
{
    [Option('f', "file", Required = true, HelpText = "Path to a JSON file containing an array of worklog objects (compatible with 'list --output json').")]
    public string FilePath { get; set; }

    [Option("dry-run", Required = false, HelpText = "Validate the entries against the API without submitting them.")]
    public bool DryRun { get; set; }
}
