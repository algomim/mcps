using Algomim.Aec.Mcp.Hosting;

namespace Algomim.AutoCad.Mcp.Harness;

/// <summary>Best-effort file/debug logger for the AutoCAD plugin.</summary>
public sealed class AutoCadLogger : IMcpLogger
{
    private readonly object _gate = new();
    private readonly string? _logFile;

    public AutoCadLogger(string? logFile = null)
    {
        _logFile = logFile;
        if (logFile is null) return;

        try
        {
            var dir = Path.GetDirectoryName(logFile);
            if (dir != null) Directory.CreateDirectory(dir);
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
        System.Diagnostics.Debug.WriteLine($"[autocad-mcp] {line}");

        if (_logFile is null) return;
        try
        {
            lock (_gate)
            {
                File.AppendAllText(_logFile, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never throw into AutoCAD.
        }
    }
}
