namespace Algomim.Revit.Mcp.Harness;

/// <summary>Minimal logging abstraction for the plugin.</summary>
public interface ILogger
{
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? exception = null);
}

/// <summary>Logger that writes to the debug output and an optional log file under the add-in folder.</summary>
public sealed class RevitLogger : ILogger
{
    private readonly object _gate = new();
    private readonly string? _logFile;

    public RevitLogger(string? logFile = null)
    {
        _logFile = logFile;
        if (logFile is null) return;
        try
        {
            var dir = System.IO.Path.GetDirectoryName(logFile);
            if (dir != null) System.IO.Directory.CreateDirectory(dir);
        }
        catch
        {
            // Logging is best-effort.
        }
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null)
        => Write("ERROR", exception != null ? $"{message} :: {exception}" : message);

    private void Write(string level, string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss} [{level}] {message}";
        System.Diagnostics.Debug.WriteLine($"[revit-mcp] {line}");
        if (_logFile is null) return;
        try
        {
            lock (_gate)
            {
                System.IO.File.AppendAllText(_logFile, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never throw into Revit.
        }
    }
}
