using System.Globalization;

namespace Timetracker.Utils;

public static class ValidationUtils
{
    public static bool ValidDate(string date) => DateTime.TryParse(date, out _);

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
