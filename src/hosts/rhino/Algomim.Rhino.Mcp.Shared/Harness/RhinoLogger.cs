using Algomim.Aec.Mcp.Hosting;
using Rhino;

namespace Algomim.Rhino.Mcp.Harness;

/// <summary>Best-effort file/debug logger for the Rhino plugin.</summary>
public sealed class RhinoLogger : IMcpLogger
{
    private readonly object _gate = new();
    private readonly string? _logFile;

    public RhinoLogger(string? logFile = null)
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
        Debug.WriteLine($"[rhino-mcp] {line}");

        try
        {
            RhinoApp.WriteLine($"[Algomim MCP] {message}");
        }
        catch
        {
            // Rhino may be shutting down or may not have a command line yet.
        }

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
            // Logging must never throw into Rhino.
        }
    }
}
