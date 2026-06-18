namespace Timetracker.Utils;

public static class ConsoleHelper
{
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
}
