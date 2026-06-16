using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using Algomim.Aec.Mcp.Hosting;
using Algomim.Aec.Mcp.Protocol;
using Algomim.Aec.Mcp.Tooling;

namespace Algomim.Aec.Mcp.Tooling.Hosting;

/// <summary>In-process Streamable HTTP MCP host using loopback-only <see cref="HttpListener"/>.</summary>
public sealed class StreamableHttpMcpHost : IDisposable
{
    private const long MaxBodySize = 10 * 1024 * 1024;

    private readonly JsonRpcMcpDispatcher _dispatcher;
    private readonly IToolCatalog _catalog;
    private readonly McpHostProfile _profile;
    private readonly string _version;
    private readonly IMcpLogger _logger;

    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private DateTime _startedUtc;

    public StreamableHttpMcpHost(
        JsonRpcMcpDispatcher dispatcher,
        IToolCatalog catalog,
        McpHostProfile profile,
        string version,
        IMcpLogger? logger = null)
    {
        _dispatcher = dispatcher;
        _catalog = catalog;
        _profile = profile;
        _version = version;
        _logger = logger ?? NullMcpLogger.Instance;
    }

    public bool IsRunning { get; private set; }
    public int Port { get; private set; }

    public void Start(int port)
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();

        Port = port;
        _startedUtc = DateTime.UtcNow;
        IsRunning = true;
        _loop = Task.Run(() => ListenLoopAsync(_cts.Token));
        _logger.Info($"{_profile.ServerName} listening on http://127.0.0.1:{port}/mcp");
    }

    public void Stop()
    {
        if (!IsRunning) return;

        try
        {
            _cts?.Cancel();
            _listener?.Stop();
            _listener?.Close();
            _loop?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
        {
            _logger.Warn($"MCP host stop: {ex.Message}");
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _listener = null;
            _loop = null;
            IsRunning = false;
            _logger.Info($"{_profile.ServerName} stopped.");
        }
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener?.IsListening == true)
        {
            try
            {
                var context = await _listener.GetContextAsync().ConfigureAwait(false);
                _ = Task.Run(() => HandleRequestAsync(context));
            }
            catch (HttpListenerException) when (ct.IsCancellationRequested) { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested) _logger.Warn($"listener error: {ex.Message}");
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            var origin = ValidateOrigin(request, response, out var ok);
            if (!ok) return;

            if (request.HttpMethod == "OPTIONS")
            {
                response.Headers.Add("Access-Control-Allow-Origin", origin ?? "*");
                response.Headers.Add("Access-Control-Allow-Methods", "POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");
                response.StatusCode = 204;
                response.Close();
                return;
            }

            var path = request.Url?.AbsolutePath ?? "/";
            if (path == "/mcp")
            {
                if (request.HttpMethod == "POST")
                    await HandleMcpPostAsync(context, origin);
                else
                    Close(response, 405);
                return;
            }

            if (path is "/" or "/health")
            {
                await HandleHealthAsync(response);
                return;
            }

            Close(response, 404);
        }
        catch (Exception ex)
        {
            _logger.Warn($"request error: {ex.Message}");
            try { Close(response, 500); } catch { /* ignore */ }
        }
    }

    private async Task HandleMcpPostAsync(HttpListenerContext context, string? origin)
    {
        var request = context.Request;
        var response = context.Response;

        var accept = request.Headers["Accept"] ?? string.Empty;
        if (!accept.Contains("application/json"))
        {
            Close(response, 406);
            return;
        }

        response.Headers.Add("Access-Control-Allow-Origin", origin ?? "*");

        if (request.ContentLength64 > MaxBodySize)
        {
            Close(response, 413);
            return;
        }

        string body;
        using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
            body = await reader.ReadToEndAsync();

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        var method = root.TryGetProperty("method", out var m) ? m.GetString() : null;
        var hasId = root.TryGetProperty("id", out var idElement) &&
                    idElement.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined);
        var parameters = root.TryGetProperty("params", out var p) ? p : default;

        object? result = null;
        JsonRpcError? error = null;
        try
        {
            result = await _dispatcher.DispatchAsync(method, parameters);
        }
        catch (McpException mex)
        {
            error = new JsonRpcError { Code = mex.Code, Message = mex.Message };
        }
        catch (Exception ex)
        {
            error = JsonRpcError.InternalError(ex.Message);
        }

        if (!hasId)
        {
            Close(response, 202);
            return;
        }

        var rpcResponse = new JsonRpcResponse
        {
            Id = idElement.Clone(),
            Result = error is null ? result : null,
            Error = error,
        };

        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(rpcResponse, McpJson.Default));
        response.ContentType = "application/json";
        response.StatusCode = 200;
        await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        await response.OutputStream.FlushAsync();
        response.Close();
    }

    private async Task HandleHealthAsync(HttpListenerResponse response)
    {
        var health = JsonSerializer.Serialize(new
        {
            status = "ok",
            server = _profile.ServerName,
            version = _version,
            transport = "streamable-http",
            protocolVersion = McpConstants.ProtocolVersion,
            toolCount = _catalog.Count,
            port = Port,
            pid = Environment.ProcessId,
            uptimeSeconds = (int)(DateTime.UtcNow - _startedUtc).TotalSeconds,
        });

        var bytes = Encoding.UTF8.GetBytes(health);
        response.ContentType = "application/json";
        response.StatusCode = 200;
        await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        response.Close();
    }

    private string? ValidateOrigin(HttpListenerRequest request, HttpListenerResponse response, out bool ok)
    {
        ok = true;
        var origin = request.Headers["Origin"];
        if (string.IsNullOrEmpty(origin)) return null;

        if (Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
            uri.Host is "127.0.0.1" or "localhost")
        {
            return origin;
        }

        _logger.Warn($"rejected non-loopback origin: {origin}");
        Close(response, 403);
        ok = false;
        return null;
    }

    private static void Close(HttpListenerResponse response, int statusCode)
    {
        response.StatusCode = statusCode;
        response.Close();
    }

    public void Dispose() => Stop();
}
