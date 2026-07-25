using Spectre.Console;
using Timetracker.Services;

namespace Timetracker.Utils;

public static class TableHelper
{
    /// <summary>Creates a table with bold headers, sized to the terminal, using the configured border.</summary>
    public static Table NewTable(params string[] columns)
    {
        var table = new Table().Border(ResolveBorder());
        foreach (var column in columns)
            table.AddColumn(new TableColumn($"[bold]{Markup.Escape(column)}[/]") { NoWrap = true });
        return table;
    }

    /// <summary>
    /// Prints a muted line for context around a table (the period header and the totals
    /// footer), so the table itself stands out as the actual result.
    /// </summary>
    public static void WriteMuted(string text) => AnsiConsole.MarkupLine($"[grey]{Markup.Escape(text)}[/]");

    private static TableBorder ResolveBorder() => ConfigService.GetTableBorder()?.ToLowerInvariant() switch
    {
        "square" => TableBorder.Square,
        "markdown" => TableBorder.Markdown,
        _ => TableBorder.Minimal
    };
}
