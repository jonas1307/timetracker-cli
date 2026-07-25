namespace Timetracker.Tests;

/// <summary>
/// Points <c>ConfigService</c> at a throwaway directory via the TIMETRACKER_CONFIG_DIR
/// override for the lifetime of the instance, then removes it. Never touches the real store.
/// </summary>
internal sealed class TempConfigDir : IDisposable
{
    private const string EnvVar = "TIMETRACKER_CONFIG_DIR";
    private readonly string _previous;

    public string Path { get; }

    public TempConfigDir()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tt-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
        _previous = Environment.GetEnvironmentVariable(EnvVar);
        Environment.SetEnvironmentVariable(EnvVar, Path);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(EnvVar, _previous);
        try { Directory.Delete(Path, recursive: true); } catch { /* best-effort cleanup */ }
    }
}
