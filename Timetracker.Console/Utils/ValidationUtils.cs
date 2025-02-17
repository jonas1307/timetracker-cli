namespace Timetracker.Utils;

public static class ValidationUtils
{
    public static bool ValidDate(string date) => DateTime.TryParse(date, out _);

    public static bool ValidType(IEnumerable<string> activities, string type) => activities.Contains(type.ToUpper());

    public static bool ValidUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out _);

}
