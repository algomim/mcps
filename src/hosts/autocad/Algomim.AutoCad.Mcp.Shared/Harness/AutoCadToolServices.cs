using Algomim.Aec.Mcp.Hosting;

namespace Algomim.AutoCad.Mcp.Harness;

/// <summary>Services passed to AutoCAD tool modules as the tool catalog grows.</summary>
public sealed record AutoCadToolServices(
    AutoCadCommandDispatcher Dispatcher,
    IMcpLogger Logger);
