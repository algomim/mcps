namespace Algomim.Aec.Mcp.Hosting;

/// <summary>Small host-neutral logger contract for in-process MCP hosts.</summary>
public interface IMcpLogger
{
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? exception = null);
}

/// <summary>No-op logger for tests and hosts that do not want file logging.</summary>
public sealed class NullMcpLogger : IMcpLogger
{
    public static readonly NullMcpLogger Instance = new();

    private NullMcpLogger() { }

    public void Info(string message) { }
    public void Warn(string message) { }
    public void Error(string message, Exception? exception = null) { }
}
