using System.Net;
using System.Net.Sockets;

namespace Algomim.Aec.Mcp.Hosting;

/// <summary>
/// Picks the first free TCP port from a pool starting at 48884, so multiple host instances each get
/// their own port. Pure given an injected free-port predicate (for tests).
/// </summary>
public static class PortAllocator
{
    public const int DefaultStart = 48884;
    public const int DefaultCount = 16;

    public static int Allocate(int start = DefaultStart, int count = DefaultCount, Func<int, bool>? isFree = null)
    {
        isFree ??= IsPortFree;
        for (var port = start; port < start + count; port++)
        {
            if (isFree(port)) return port;
        }

        throw new InvalidOperationException($"No free port available in range {start}..{start + count - 1}.");
    }

    private static bool IsPortFree(int port)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
