using Algomim.Aec.Mcp.Hosting;
using Algomim.Aec.Mcp.Tooling.Hosting;
using Algomim.Aec.Mcp.Tooling.Registry;
using Algomim.Aec.Mcp.Tooling.Updates;
using Algomim.Rhino.Mcp.Harness;
using Algomim.Rhino.Mcp.Hosting;
using Algomim.Rhino.Mcp.Tools;
using Rhino;
using Rhino.PlugIns;

namespace Algomim.Rhino.Mcp.App;

/// <summary>Rhino plugin composition root for the in-process MCP host.</summary>
public sealed class RhinoMcpPlugin : PlugIn
{
    public static RhinoMcpPlugin? Instance { get; private set; }

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "rhino-mcp",
        "rhino-mcp.log");

    private readonly RhinoLogger _logger = new(LogPath);
    private RuntimeAnnouncementWriter? _announcements;
    private StreamableHttpMcpHost? _host;
    private McpHostProfile? _profile;
    private int _startupUpdateCheckStarted;

    public bool IsConnected => _host?.IsRunning == true;
    public int? Port => _host?.IsRunning == true ? _host.Port : null;

    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
        Instance = this;

        try
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            _profile = RhinoMcpServerProfile.Create();
            _announcements = new RuntimeAnnouncementWriter(_logger);

            var tools = RhinoToolCatalog.Create();
            var nameErrors = ToolCatalogValidator.ValidateNames(tools.Select(tool => tool.Name));
            if (nameErrors.Count > 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, nameErrors));

            var catalog = new McpToolCatalog(tools);
            var version = typeof(RhinoMcpPlugin).Assembly.GetName().Version?.ToString() ?? "0.1.0";
            var requestDispatcher = new JsonRpcMcpDispatcher(catalog, _profile, version);
            _host = new StreamableHttpMcpHost(requestDispatcher, catalog, _profile, version, _logger);

            RhinoApp.WriteLine("rhino-mcp loaded. Use Algomim to connect, view status, or update.");
            _logger.Info("rhino-mcp initialized.");
            StartStartupUpdateCheck();
            return LoadReturnCode.Success;
        }
        catch (Exception ex)
        {
            _logger.Error("Rhino MCP load failed", ex);
            errorMessage = ex.Message;
            Instance = null;
            return LoadReturnCode.ErrorShowDialog;
        }
    }

    protected override void OnShutdown()
    {
        try
        {
            Disconnect();
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
            Instance = null;
            _logger.Info("rhino-mcp terminated.");
        }
        catch (Exception ex)
        {
            _logger.Error("Rhino MCP shutdown failed", ex);
        }

        base.OnShutdown();
    }

    public void Toggle(RhinoDoc? document)
    {
        if (IsConnected) Disconnect();
        else Connect(document);
    }

    public void Connect(RhinoDoc? document)
    {
        if (_host is null || _profile is null || IsConnected) return;

        var port = PortAllocator.Allocate();
        _host.Start(port);

        var documentName = GetDocumentName(document);
        var rhinoVersion = GetRhinoVersion();
        var url = $"http://127.0.0.1:{port}/mcp";
        _announcements?.Write(new AnnouncementEntry
        {
            Id = $"{_profile.Owner}-{Environment.ProcessId}-{port}",
            Owner = _profile.Owner,
            Name = string.IsNullOrWhiteSpace(documentName) ? $"Rhino {rhinoVersion}" : documentName,
            Pid = Environment.ProcessId,
            Version = rhinoVersion,
            Document = documentName,
            Port = port,
            Url = url,
            HealthUrl = $"http://127.0.0.1:{port}/health",
        });

        RhinoApp.WriteLine($"rhino-mcp connected on {url}");
    }

    public void Disconnect()
    {
        if (_host?.IsRunning == true)
            _host.Stop();

        _announcements?.Remove(RhinoMcpServerProfile.Owner, Environment.ProcessId);
        RhinoApp.WriteLine("rhino-mcp disconnected.");
    }

    public string GetStatusText()
    {
        if (IsConnected && Port is { } port)
            return $"rhino-mcp is connected.\n\nMCP URL: http://127.0.0.1:{port}/mcp\nHealth: http://127.0.0.1:{port}/health";

        return "rhino-mcp is disconnected.\n\nRun Algomim and choose Connect to start the MCP server.";
    }

    public void ShowStatus()
    {
        RhinoApp.WriteLine(GetStatusText());
    }

    public void CheckForUpdates()
    {
        try
        {
            var update = CheckForUpdatesCore();

            if (!string.IsNullOrWhiteSpace(update.Message))
            {
                RhinoApp.WriteLine($"{update.Message} Release page: {update.ReleaseUrl}");
                return;
            }

            if (!update.IsUpdateAvailable)
            {
                RhinoApp.WriteLine($"rhino-mcp is up to date ({update.CurrentVersion}).");
                return;
            }

            var installerText = update.InstallerName is null
                ? "No host-specific MSI was found. Opening the release page."
                : $"Installer: {update.InstallerName}";
            RhinoApp.WriteLine(
                $"rhino-mcp {update.LatestVersion} is available. You have {update.CurrentVersion}. " +
                $"{installerText}");
            OpenUrl(update.InstallerUrl ?? update.ReleaseUrl);
        }
        catch (Exception ex)
        {
            _logger.Warn($"Update check failed: {ex.Message}");
            RhinoApp.WriteLine($"Update check failed: {ex.Message}");
        }
    }

    private void StartStartupUpdateCheck()
    {
        if (System.Threading.Interlocked.Exchange(ref _startupUpdateCheckStarted, 1) == 1)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                if (!ReferenceEquals(Instance, this))
                    return;

                var update = CheckForUpdatesCore();
                if (!ReferenceEquals(Instance, this) || !update.IsUpdateAvailable || !string.IsNullOrWhiteSpace(update.Message))
                    return;

                RhinoApp.InvokeOnUiThread((Action)(() =>
                {
                    RhinoApp.WriteLine(
                        $"rhino-mcp {update.LatestVersion} is available. You have {update.CurrentVersion}. " +
                        "Run Algomim and choose Update when you are ready to open the release page.");
                }));
            }
            catch (Exception ex)
            {
                _logger.Warn($"Startup update check failed: {ex.Message}");
            }
        });
    }

    private static ReleaseUpdateInfo CheckForUpdatesCore()
    {
        var currentVersion = typeof(RhinoMcpPlugin).Assembly.GetName().Version?.ToString() ?? "0.0.0";
        return new GitHubReleaseUpdateChecker("algomim", "mcps")
            .CheckAsync(currentVersion, "rhino-mcp-")
            .GetAwaiter()
            .GetResult();
    }

    private static string? GetDocumentName(RhinoDoc? document)
    {
        var path = document?.Path;
        if (!string.IsNullOrWhiteSpace(path))
            return Path.GetFileName(path);

        var name = document?.Name;
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private static string GetRhinoVersion()
        => RhinoApp.Version.ToString();

    private static void OpenUrl(string url)
        => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name).Name;
        if (name is null) return null;

        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (dir is null) return null;

        var path = Path.Combine(dir, $"{name}.dll");
        return File.Exists(path) ? Assembly.LoadFrom(path) : null;
    }
}
