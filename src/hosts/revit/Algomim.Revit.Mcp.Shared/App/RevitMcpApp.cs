using System.Diagnostics;
using System.IO;
using System.Reflection;
using Autodesk.Revit.UI;
using Algomim.Aec.Mcp.Hosting;
using Algomim.Aec.Mcp.Scripting;
using Algomim.Aec.Mcp.Tooling.Updates;
using Algomim.Revit.Mcp.Discovery;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Hosting;
using Algomim.Revit.Mcp.Scripting;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;
using Result = Autodesk.Revit.UI.Result;

namespace Algomim.Revit.Mcp.App;

/// <summary>
/// Revit add-in entry point and composition root — the one place everything is wired together. Revit
/// owns this class; it builds the harness, tools, and in-Revit MCP host, and forwards the ribbon
/// toggle to connect/disconnect. The host starts on connect and stops on disconnect (or shutdown).
/// </summary>
public sealed class RevitMcpApp : IExternalApplication
{
    public static RevitMcpApp? Instance { get; private set; }

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "revit-mcp", "revit-mcp.log");

    private readonly ILogger _logger = new RevitLogger(LogPath);
    private UiThreadDispatcher? _dispatcher;
    private HttpListenerMcpHost? _host;
    private AnnouncementWriter? _announcements;
    private RibbonController? _ribbon;
    private int _startupUpdateCheckStarted;

    public bool IsConnected => _host?.IsRunning == true;
    public int? Port => _host?.IsRunning == true ? _host.Port : null;

    public Result OnStartup(UIControlledApplication application)
    {
        Instance = this;
        try
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            _dispatcher = new UiThreadDispatcher();
            _announcements = new AnnouncementWriter(_logger);

            IScriptCompiler compiler = new CollectibleScriptCache(new RoslynScriptCompiler());
            var services = new RevitToolServices(_dispatcher, compiler, _logger, new RevitDocumentContextStore());
            var tools = RevitToolCatalog.Create(services);
            var catalog = new McpToolRegistry(tools);
            var version = typeof(RevitMcpApp).Assembly.GetName().Version?.ToString() ?? "0.1.0";
            var requestDispatcher = new McpRequestDispatcher(catalog, version);
            _host = new HttpListenerMcpHost(requestDispatcher, catalog, version, _logger);

            ReflectionApiDiscovery.WarmUp();

            _ribbon = new RibbonController(application);
            _ribbon.Build();

            _logger.Info("revit-mcp started.");
            StartStartupUpdateCheck();
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.Error("OnStartup failed", ex);
            return Result.Failed;
        }
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        try
        {
            Disconnect();
            _dispatcher?.Dispose();
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
            Instance = null;
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.Error("OnShutdown failed", ex);
            return Result.Failed;
        }
    }

    /// <summary>Ribbon toggle entry: connect if disconnected, disconnect if connected.</summary>
    public void Toggle(UIApplication uiApp)
    {
        if (IsConnected) Disconnect();
        else Connect(uiApp);
    }

    public void CheckForUpdates()
    {
        try
        {
            var update = CheckForUpdatesCore();
            ApplyUpdateButtonState(update);

            if (!string.IsNullOrWhiteSpace(update.Message))
            {
                System.Windows.Forms.MessageBox.Show(
                    $"{update.Message}\n\nRelease page: {update.ReleaseUrl}",
                    "Algomim MCP Update",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
                return;
            }

            if (!update.IsUpdateAvailable)
            {
                System.Windows.Forms.MessageBox.Show(
                    $"revit-mcp is up to date ({update.CurrentVersion}).",
                    "Algomim MCP Update",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
                return;
            }

            var installerText = update.InstallerName is null
                ? "No host-specific MSI was found. The release page can be opened instead."
                : $"Installer: {update.InstallerName}";
            var result = System.Windows.Forms.MessageBox.Show(
                $"revit-mcp {update.LatestVersion} is available. You have {update.CurrentVersion}.\n\n" +
                $"{installerText}\n\nOpen the release page to download and install it manually?",
                "Algomim MCP Update",
                System.Windows.Forms.MessageBoxButtons.YesNo,
                System.Windows.Forms.MessageBoxIcon.Information);

            if (result != System.Windows.Forms.DialogResult.Yes)
                return;

            OpenUrl(update.InstallerUrl ?? update.ReleaseUrl);
        }
        catch (Exception ex)
        {
            _logger.Warn($"Update check failed: {ex.Message}");
            System.Windows.Forms.MessageBox.Show(
                $"Update check failed: {ex.Message}",
                "Algomim MCP Update",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Warning);
        }
    }

    public string GetStatusText()
    {
        if (IsConnected && Port is { } port)
            return $"revit-mcp is connected.\n\nMCP URL: http://127.0.0.1:{port}/mcp\nHealth: http://127.0.0.1:{port}/health";

        return "revit-mcp is disconnected.\n\nClick Connect to start the MCP server.";
    }

    public void ShowStatus()
    {
        System.Windows.Forms.MessageBox.Show(
            GetStatusText(),
            "Algomim MCP Status",
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Information);
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
                var dispatcher = _dispatcher;
                if (dispatcher is null || !ReferenceEquals(Instance, this))
                    return;

                var notifyTask = dispatcher.InvokeOnUiThreadAsync(_ =>
                {
                    if (!ReferenceEquals(Instance, this))
                        return;

                    ApplyUpdateButtonState(update);
                    if (!update.IsUpdateAvailable || !string.IsNullOrWhiteSpace(update.Message))
                        return;

                    System.Windows.Forms.MessageBox.Show(
                        $"revit-mcp {update.LatestVersion} is available. You have {update.CurrentVersion}.\n\n" +
                        "Use the Update button to open the release page when you are ready to install it.",
                        "Algomim MCP Update",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Information);
                });

                _ = notifyTask.ContinueWith(
                    task => _logger.Warn($"Startup update notification failed: {task.Exception?.GetBaseException().Message}"),
                    TaskContinuationOptions.OnlyOnFaulted);
            }
            catch (Exception ex)
            {
                _logger.Warn($"Startup update check failed: {ex.Message}");
            }
        });
    }

    private static ReleaseUpdateInfo CheckForUpdatesCore()
    {
        var currentVersion = typeof(RevitMcpApp).Assembly.GetName().Version?.ToString() ?? "0.0.0";
        return new GitHubReleaseUpdateChecker("algomim", "mcps")
            .CheckAsync(currentVersion, "revit-mcp-")
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

    private void Connect(UIApplication uiApp)
    {
        if (_host is null || IsConnected) return;

        var port = PortAllocator.Allocate();
        _host.Start(port);

        var version = uiApp.Application.VersionNumber;
        var document = uiApp.ActiveUIDocument?.Document?.Title;
        var url = $"http://127.0.0.1:{port}/mcp";
        _announcements?.Write(new AnnouncementEntry
        {
            Id = $"revit-{Environment.ProcessId}-{port}",
            Owner = "revit",
            Name = string.IsNullOrWhiteSpace(document) ? $"Revit {version}" : document,
            Pid = Environment.ProcessId,
            Version = version,
            Document = document,
            Port = port,
            Url = url,
            HealthUrl = $"http://127.0.0.1:{port}/health",
        });

        _ribbon?.SetConnected(port);
    }

    private void Disconnect()
    {
        if (_host?.IsRunning == true)
            _host.Stop();

        _announcements?.Remove(Environment.ProcessId);
        _ribbon?.SetDisconnected();
    }

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
