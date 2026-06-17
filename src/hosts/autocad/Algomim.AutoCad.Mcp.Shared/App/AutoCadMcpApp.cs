using System.Diagnostics;
using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.Runtime;
using Algomim.Aec.Mcp.Hosting;
using Algomim.Aec.Mcp.Tooling.Hosting;
using Algomim.Aec.Mcp.Tooling.Registry;
using Algomim.Aec.Mcp.Tooling.Updates;
using Algomim.AutoCad.Mcp.Harness;
using Algomim.AutoCad.Mcp.Hosting;
using Algomim.AutoCad.Mcp.Tools;
using Algomim.AutoCad.Mcp.UI;
using AutoCadApplication = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Algomim.AutoCad.Mcp.App;

/// <summary>AutoCAD plugin composition root for the in-process MCP host.</summary>
public sealed class AutoCadMcpApp : IExtensionApplication
{
    public static AutoCadMcpApp? Instance { get; private set; }

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "autocad-mcp",
        "autocad-mcp.log");

    private readonly AutoCadLogger _logger = new(LogPath);
    private RuntimeAnnouncementWriter? _announcements;
    private StreamableHttpMcpHost? _host;
    private RibbonController? _ribbon;
    private McpHostProfile? _profile;
    private bool _isRibbonBuildDeferred;
    private int _startupUpdateCheckStarted;
    private ReleaseUpdateInfo? _pendingStartupUpdate;

    public bool IsConnected => _host?.IsRunning == true;
    public int? Port => _host?.IsRunning == true ? _host.Port : null;

    public void Initialize()
    {
        Instance = this;

        try
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            _profile = AutoCadMcpServerProfile.Create();
            _announcements = new RuntimeAnnouncementWriter(_logger);

            var commandDispatcher = new AutoCadCommandDispatcher(_logger);
            var services = new AutoCadToolServices(commandDispatcher, _logger);
            var tools = AutoCadToolCatalog.Create(services);
            var nameErrors = ToolCatalogValidator.ValidateNames(tools.Select(tool => tool.Name));
            if (nameErrors.Count > 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, nameErrors));

            var catalog = new McpToolCatalog(tools);
            var version = typeof(AutoCadMcpApp).Assembly.GetName().Version?.ToString() ?? "0.1.0";
            var requestDispatcher = new JsonRpcMcpDispatcher(catalog, _profile, version);
            _host = new StreamableHttpMcpHost(requestDispatcher, catalog, _profile, version, _logger);

            _ribbon = new RibbonController(_logger);
            BuildRibbonOrDefer();

            WriteEditorMessage("autocad-mcp loaded. Use ALGOMIM_MCP_TOGGLE or the Add-ins ribbon panel to connect.");
            _logger.Info("autocad-mcp initialized.");
            StartStartupUpdateCheck();
        }
        catch (System.Exception ex)
        {
            _logger.Error("AutoCAD MCP initialize failed", ex);
            WriteEditorMessage($"autocad-mcp failed to initialize: {ex.Message}");
        }
    }

    public void Terminate()
    {
        try
        {
            Disconnect();
            AutoCadApplication.Idle -= OnAutoCadIdle;
            AutoCadApplication.Idle -= OnStartupUpdateIdle;
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
            Instance = null;
            _logger.Info("autocad-mcp terminated.");
        }
        catch (System.Exception ex)
        {
            _logger.Error("AutoCAD MCP terminate failed", ex);
        }
    }

    public void Toggle()
    {
        if (IsConnected) Disconnect();
        else Connect();
    }

    public void Connect()
    {
        if (_host is null || _profile is null || IsConnected) return;

        BuildRibbonOrDefer();

        var port = PortAllocator.Allocate();
        _host.Start(port);

        var document = GetDocumentName();
        var autoCadVersion = GetAutoCadVersion();
        var url = $"http://127.0.0.1:{port}/mcp";
        _announcements?.Write(new AnnouncementEntry
        {
            Id = $"{_profile.Owner}-{Environment.ProcessId}-{port}",
            Owner = _profile.Owner,
            Name = string.IsNullOrWhiteSpace(document) ? $"AutoCAD {autoCadVersion}" : document,
            Pid = Environment.ProcessId,
            Version = autoCadVersion,
            Document = document,
            Port = port,
            Url = url,
            HealthUrl = $"http://127.0.0.1:{port}/health",
        });

        _ribbon?.SetConnected(port);
        WriteEditorMessage($"autocad-mcp connected on {url}");
    }

    public void Disconnect()
    {
        if (_host?.IsRunning == true)
            _host.Stop();

        _announcements?.Remove(AutoCadMcpServerProfile.Owner, Environment.ProcessId);
        _ribbon?.SetDisconnected();
        WriteEditorMessage("autocad-mcp disconnected.");
    }

    public string GetStatusText()
    {
        if (IsConnected && Port is { } port)
            return $"autocad-mcp is connected.\n\nMCP URL: http://127.0.0.1:{port}/mcp\nHealth: http://127.0.0.1:{port}/health";

        return "autocad-mcp is disconnected.\n\nClick Connect to start the MCP server.";
    }

    public void ShowStatus()
    {
        AutoCadApplication.ShowAlertDialog(GetStatusText());
    }

    public void CheckForUpdates()
    {
        try
        {
            var update = CheckForUpdatesCore();
            ApplyUpdateButtonState(update);

            if (!string.IsNullOrWhiteSpace(update.Message))
            {
                AutoCadApplication.ShowAlertDialog($"{update.Message}\n\nRelease page: {update.ReleaseUrl}");
                return;
            }

            if (!update.IsUpdateAvailable)
            {
                AutoCadApplication.ShowAlertDialog($"autocad-mcp is up to date ({update.CurrentVersion}).");
                return;
            }

            var installerText = update.InstallerName is null
                ? "No host-specific MSI was found. Opening the release page."
                : $"Installer: {update.InstallerName}";
            AutoCadApplication.ShowAlertDialog(
                $"autocad-mcp {update.LatestVersion} is available. You have {update.CurrentVersion}.\n\n" +
                $"{installerText}\n\nOpening the release page so you can download and install it manually.");
            OpenUrl(update.InstallerUrl ?? update.ReleaseUrl);
        }
        catch (System.Exception ex)
        {
            _logger.Warn($"Update check failed: {ex.Message}");
            AutoCadApplication.ShowAlertDialog($"Update check failed: {ex.Message}");
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
                if (!ReferenceEquals(Instance, this))
                    return;

                _pendingStartupUpdate = update;
                AutoCadApplication.Idle += OnStartupUpdateIdle;
            }
            catch (System.Exception ex)
            {
                _logger.Warn($"Startup update check failed: {ex.Message}");
            }
        });
    }

    private static ReleaseUpdateInfo CheckForUpdatesCore()
    {
        var currentVersion = typeof(AutoCadMcpApp).Assembly.GetName().Version?.ToString() ?? "0.0.0";
        return new GitHubReleaseUpdateChecker("algomim", "mcps")
            .CheckAsync(currentVersion, "autocad-mcp-")
            .GetAwaiter()
            .GetResult();
    }

    private void ApplyUpdateButtonState(ReleaseUpdateInfo update)
    {
        if (!string.IsNullOrWhiteSpace(update.Message))
        {
            _ribbon?.SetUpdateUnknown();
            return;
        }

        if (update.IsUpdateAvailable)
            _ribbon?.SetUpdateAvailable(update.LatestVersion);
        else
            _ribbon?.SetUpToDate(update.CurrentVersion);
    }

    private static string? GetDocumentName()
    {
        var name = AutoCadApplication.DocumentManager.MdiActiveDocument?.Name;
        return string.IsNullOrWhiteSpace(name) ? null : Path.GetFileName(name);
    }

    private static string GetAutoCadVersion()
    {
        try
        {
            return Convert.ToString(AutoCadApplication.GetSystemVariable("ACADVER")) ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    private static void WriteEditorMessage(string message)
    {
        try
        {
            AutoCadApplication.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\n[Algomim MCP] {message}");
        }
        catch
        {
            // AutoCAD may be shutting down or may not have an active document.
        }
    }

    private static void OpenUrl(string url)
        => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    private void BuildRibbonOrDefer()
    {
        if (_ribbon is null || _ribbon.IsBuilt) return;
        if (_ribbon.Build())
        {
            AutoCadApplication.Idle -= OnAutoCadIdle;
            _isRibbonBuildDeferred = false;
            return;
        }

        if (_isRibbonBuildDeferred) return;
        AutoCadApplication.Idle += OnAutoCadIdle;
        _isRibbonBuildDeferred = true;
    }

    private void OnAutoCadIdle(object? sender, EventArgs e)
    {
        if (_ribbon is null || _ribbon.IsBuilt)
        {
            AutoCadApplication.Idle -= OnAutoCadIdle;
            _isRibbonBuildDeferred = false;
            return;
        }

        if (!_ribbon.Build()) return;

        AutoCadApplication.Idle -= OnAutoCadIdle;
        _isRibbonBuildDeferred = false;
        if (IsConnected && Port is { } port)
            _ribbon.SetConnected(port);
    }

    private void OnStartupUpdateIdle(object? sender, EventArgs e)
    {
        AutoCadApplication.Idle -= OnStartupUpdateIdle;

        var update = _pendingStartupUpdate;
        _pendingStartupUpdate = null;
        if (update is null)
            return;

        ApplyUpdateButtonState(update);
        if (!update.IsUpdateAvailable || !string.IsNullOrWhiteSpace(update.Message))
            return;

        AutoCadApplication.ShowAlertDialog(
            $"autocad-mcp {update.LatestVersion} is available. You have {update.CurrentVersion}.\n\n" +
            "Use the Update button to open the release page when you are ready to install it.");
    }

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
