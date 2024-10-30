using CommandLine;

namespace Timetracker.Options;

[Verb("config", HelpText = "Configs the parameters for the CLI.")]
public class ConfigOptions
{
    [Option('u', "url", Required = true, HelpText = "The URL for the Timetracker instance.")]
    public string TimetrackerUrl { get; set; }

    [Option('t', "token", Required = true, HelpText = "The Bearer Token for the Timetracker instance.")]
    public string TimetrackerBearerToken { get; set; }
}
