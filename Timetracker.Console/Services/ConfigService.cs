using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Timetracker.Options;

namespace Timetracker.Services;

public record Config
{
    public string TimetrackerUrl { get; set; }
    public string TimetrackerBearerToken { get; set; }
    public string TimetrackerUserId { get; set; }
    public string DisplayName { get; set; }
    public string Email { get; set; }
    public string AccountName { get; set; }
    public bool TokenEncrypted { get; set; }
    public string TableBorder { get; set; }
}

public static class ConfigService
{
    private const string APPLICATION_NAME = "Timetracker.Console";
    private const string JSON_FILE_NAME = "config.json";

    private static string GetConfigPath()
    {
        var folderPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), APPLICATION_NAME)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", APPLICATION_NAME);

        return Path.Combine(folderPath, JSON_FILE_NAME);
    }

    public static bool ConfigExists() => File.Exists(GetConfigPath());

    public static void DeleteConfig()
    {
        var configPath = GetConfigPath();

        if (File.Exists(configPath))
            File.Delete(configPath);
    }

    public static Config LoadConfig()
    {
        var configPath = GetConfigPath();

        if (!File.Exists(configPath))
            throw new FileNotFoundException($"{JSON_FILE_NAME} does not exist. Make sure you already executed the config method.");

        var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));

        if (config.TokenEncrypted && OperatingSystem.IsWindows())
            config = config with { TimetrackerBearerToken = DecryptToken(config.TimetrackerBearerToken) };

        return config;
    }

    public static void SaveConfig(ConfigOptions opts, string userId, string displayName, string email, string accountName)
    {
        var configPath = GetConfigPath();
        var folderPath = Path.GetDirectoryName(configPath);

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string token = opts.TimetrackerBearerToken;
        bool tokenEncrypted = false;

        if (OperatingSystem.IsWindows())
        {
            token = EncryptToken(token);
            tokenEncrypted = true;
        }

        var config = new Config
        {
            TimetrackerUrl = opts.TimetrackerUrl,
            TimetrackerBearerToken = token,
            TimetrackerUserId = userId,
            DisplayName = displayName,
            Email = email,
            AccountName = accountName,
            TokenEncrypted = tokenEncrypted,
            TableBorder = opts.Border ?? GetTableBorder()
        };

        File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));

        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(configPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    /// <summary>Reads the configured table border without decrypting the token; null if unset.</summary>
    public static string GetTableBorder()
    {
        var configPath = GetConfigPath();
        if (!File.Exists(configPath))
            return null;

        try
        {
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath))?.TableBorder;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Updates only the table border, preserving the (encrypted) token and everything else.</summary>
    public static void SetTableBorder(string border)
    {
        var configPath = GetConfigPath();
        if (!File.Exists(configPath))
            throw new FileNotFoundException($"{JSON_FILE_NAME} does not exist. Make sure you already executed the config method.");

        var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath)) with { TableBorder = border };
        File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));

        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(configPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    [SupportedOSPlatform("windows")]
    private static string EncryptToken(string token)
    {
        var bytes = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(token),
            null,
            DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(bytes);
    }

    [SupportedOSPlatform("windows")]
    private static string DecryptToken(string encrypted)
    {
        var bytes = ProtectedData.Unprotect(
            Convert.FromBase64String(encrypted),
            null,
            DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }
}
