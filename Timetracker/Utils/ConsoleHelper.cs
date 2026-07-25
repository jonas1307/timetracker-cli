namespace Timetracker.Utils;

public static class ConsoleHelper
{
    // Shared user-facing strings reused across commands. Centralized here as a
    // first step toward future localization.
    public const string ConfigNotFound = "Configuration not found. Please run the 'config' command first.";
    public const string OperationCancelled = "Operation cancelled.";
    public const string NoTimeEntries = "No time entries found for the selected period.";

    public static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(message);
        Console.ResetColor();
    }

    public static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
