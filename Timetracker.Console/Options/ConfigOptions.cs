using CommandLine;

namespace Timetracker.Options;

[Verb("config", HelpText = "Configure the CLI parameters for connecting to your Timetracker instance.")]
public class ConfigOptions
{
    [Option('u', "url", Required = true, HelpText = "Specify the URL for your Timetracker instance (e.g., https://<company>.timehub.7pace.com).")]
    public string TimetrackerUrl { get; set; }

    [Option('t', "token", Required = true, HelpText = "Provide the Bearer Token required for authentication with your Timetracker instance.")]
    public string TimetrackerBearerToken { get; set; }
}
