namespace Timetracker.Options;

/// <summary>
/// Shared period selection used by the reporting commands (list, summary).
/// </summary>
public interface IPeriodOptions
{
    string From { get; }
    string To { get; }
    string Period { get; }
    bool Today { get; }
    bool Yesterday { get; }
    bool Week { get; }
    bool LastWeek { get; }
    bool Month { get; }
    bool LastMonth { get; }
}
