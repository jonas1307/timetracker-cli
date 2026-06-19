using System.Globalization;
using System.Text.RegularExpressions;

namespace Timetracker.Utils;

public static class ValidationUtils
{
    public static bool ValidDate(string date) => DateTime.TryParse(date, out _);

    public static bool ValidActivityDate(string date)
    {
        if (string.IsNullOrEmpty(date)) return false;
        if (date.Equals("today", StringComparison.OrdinalIgnoreCase)) return true;
        if (date.Equals("yesterday", StringComparison.OrdinalIgnoreCase)) return true;

        return Regex.IsMatch(date, @"^\d{4}/\d{2}/\d{2}$") && ValidDate(date);
    }

    public static bool ValidType(IEnumerable<string> activities, string type) => activities.Contains(type.ToUpper());

    public static bool ValidUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    public static DateTime ResolveDate(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Equals("today", StringComparison.OrdinalIgnoreCase))
            return DateTime.Today;

        if (input.Equals("yesterday", StringComparison.OrdinalIgnoreCase))
            return DateTime.Today.AddDays(-1);

        return DateTime.Parse(input);
    }

    public static (DateTime From, DateTime To) ResolveCurrentWeek()
    {
        var today = DateTime.Today;
        var diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        var monday = today.AddDays(-diff);
        return (monday, monday.AddDays(6));
    }

    public static (DateTime From, DateTime To) ResolveLastWeek()
    {
        var (thisMonday, _) = ResolveCurrentWeek();
        var lastMonday = thisMonday.AddDays(-7);
        return (lastMonday, lastMonday.AddDays(6));
    }

    public static bool TryResolveMonth(string input, out DateTime firstDay, out DateTime lastDay)
    {
        firstDay = default;
        lastDay = default;

        if (!DateTime.TryParseExact(input, "yyyy/MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var month))
            return false;

        firstDay = new DateTime(month.Year, month.Month, 1);
        lastDay = firstDay.AddMonths(1).AddDays(-1);
        return true;
    }
}
