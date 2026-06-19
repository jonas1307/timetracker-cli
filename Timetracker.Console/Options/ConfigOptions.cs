using CommandLine;

namespace Timetracker.Options;

[Verb("config", HelpText = "Configure the CLI parameters for connecting to your Timetracker instance.")]
public class ConfigOptions
{
    [Option('u', "url", Required = false, HelpText = "Specify the URL for your Timetracker instance (e.g., https://<company>.timehub.7pace.com).")]
    public string TimetrackerUrl { get; set; }

    [Option('t', "token", Required = false, HelpText = "Provide the Bearer Token required for authentication with your Timetracker instance.")]
    public string TimetrackerBearerToken { get; set; }

    [Option("show", Required = false, HelpText = "Display the current configuration, masking the bearer token.")]
    public bool Show { get; set; }

    [Option("reset", Required = false, HelpText = "Remove all local configuration and activity cache files.")]
    public bool Reset { get; set; }
}
