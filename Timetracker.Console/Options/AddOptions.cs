using CommandLine;

namespace Timetracker.Options;

[Verb("add", HelpText = "Add an activity to the staging area.")]
public class AddOptions
{
    [Option('d', "date", Required = true, HelpText = "Sets the date for the activity.")]
    public string ActivityDate { get; set; }

    [Option('w', "workitem", Required = true, HelpText = "Sets the Work Item ID for the activity.")]
    public int WorkItem { get; set; }

    [Option('l', "length", Required = true, HelpText = "Sets the length in hours for the activity.")]
    public decimal ActivityLenght { get; set; }

    [Option('t', "type", Required = true, HelpText = "Sets the type of the activity.")]
    public string ActivityType { get; set; }

    [Option('c', "comment", Required = false, HelpText = "Sets a comment for the activity.")]
    public string ActivityComment { get; set; }
}
