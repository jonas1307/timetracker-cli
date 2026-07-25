using Timetracker.Options;

namespace Timetracker.Tests;

/// <summary>Mutable <see cref="IPeriodOptions"/> for exercising <c>PeriodResolver</c>.</summary>
internal sealed class FakePeriodOptions : IPeriodOptions
{
    public string From { get; init; }
    public string To { get; init; }
    public string Period { get; init; }
    public bool Today { get; init; }
    public bool Yesterday { get; init; }
    public bool Week { get; init; }
    public bool LastWeek { get; init; }
    public bool Month { get; init; }
    public bool LastMonth { get; init; }
}
