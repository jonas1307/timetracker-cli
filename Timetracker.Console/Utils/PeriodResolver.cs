using Timetracker.Options;

namespace Timetracker.Utils;

/// <summary>
/// Resolves the date range from the period options shared by the reporting commands.
/// </summary>
public static class PeriodResolver
{
    public static bool TryResolve(IPeriodOptions opts, out DateTime from, out DateTime to, out string error)
    {
        from = to = DateTime.Today;
        error = null;

        var periodFlags = new[]
        {
            opts.Today, opts.Yesterday, opts.Week, opts.LastWeek, opts.Month, opts.LastMonth,
            !string.IsNullOrEmpty(opts.Period)
        }.Count(x => x);

        if (periodFlags > 1)
        {
            error = "--today, --yesterday, --week, --last-week, --month, --last-month and --period are mutually exclusive.";
            return false;
        }

        var usesShortcut = opts.Today || opts.Yesterday || opts.Week || opts.LastWeek || opts.Month || opts.LastMonth;

        if (usesShortcut && (!string.IsNullOrEmpty(opts.From) || !string.IsNullOrEmpty(opts.To)))
        {
            error = "Period shortcuts cannot be used together with --from or --to.";
            return false;
        }

        if (opts.Today)
        {
            from = to = DateTime.Today;
            return true;
        }

        if (opts.Yesterday)
        {
            from = to = DateTime.Today.AddDays(-1);
            return true;
        }

        if (opts.Week)
        {
            (from, to) = ValidationUtils.ResolveCurrentWeek();
            return true;
        }

        if (opts.LastWeek)
        {
            (from, to) = ValidationUtils.ResolveLastWeek();
            return true;
        }

        if (opts.Month)
        {
            (from, to) = ValidationUtils.ResolveCurrentMonth();
            return true;
        }

        if (opts.LastMonth)
        {
            (from, to) = ValidationUtils.ResolveLastMonth();
            return true;
        }

        if (!string.IsNullOrEmpty(opts.Period))
        {
            if (!string.IsNullOrEmpty(opts.From) || !string.IsNullOrEmpty(opts.To))
            {
                error = "--period cannot be used together with --from or --to.";
                return false;
            }

            if (!ValidationUtils.TryResolveMonth(opts.Period, out from, out to))
            {
                error = "Invalid period format. Use YYYY/MM (e.g., 2026/06).";
                return false;
            }

            return true;
        }

        from = ValidationUtils.ResolveDate(opts.From);
        to = ValidationUtils.ResolveDate(opts.To);

        if (from > to)
        {
            error = "The 'from' date must be earlier than or equal to the 'to' date.";
            return false;
        }

        return true;
    }
}
