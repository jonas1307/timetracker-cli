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

    /// <summary>Prints a dim footer line (e.g., totals) below a table.</summary>
    public static void WriteFooter(string text) => AnsiConsole.MarkupLine($"[dim]{Markup.Escape(text)}[/]");

    private static TableBorder ResolveBorder() => ConfigService.GetTableBorder()?.ToLowerInvariant() switch
    {
        "square" => TableBorder.Square,
        "markdown" => TableBorder.Markdown,
        _ => TableBorder.Minimal
    };
}
