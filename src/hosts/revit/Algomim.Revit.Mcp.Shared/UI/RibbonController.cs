using System.Linq;
using System.Reflection;
using Autodesk.Revit.UI;

namespace Algomim.Revit.Mcp.App;

/// <summary>Builds the ribbon panel and the single connect/disconnect toggle button.</summary>
public sealed class RibbonController
{
    private const string PanelName = "revit-mcp";

    private readonly UIControlledApplication _application;
    private PushButton? _toggleButton;
    private PushButton? _updateButton;
    private string _updateText = "Update";
    private string _updateToolTip = "Check GitHub Releases for a newer revit-mcp MSI.";

    public RibbonController(UIControlledApplication application) => _application = application;

    public void Build()
    {
        var panel = _application.GetRibbonPanels().FirstOrDefault(p => p.Name == PanelName)
                    ?? _application.CreateRibbonPanel(PanelName);

        var toggleData = new PushButtonData(
            "revitMcpToggle",
            "Connect",
            Assembly.GetExecutingAssembly().Location,
            typeof(ToggleConnectionCommand).FullName)
        {
            ToolTip = "Start or stop the revit-mcp server for this Revit instance.",
        };

        var updateData = new PushButtonData(
            "revitMcpUpdate",
            "Update",
            Assembly.GetExecutingAssembly().Location,
            typeof(CheckForUpdatesCommand).FullName)
        {
            ToolTip = "Check GitHub Releases for a newer revit-mcp MSI.",
        };

        var statusData = new PushButtonData(
            "revitMcpStatus",
            "Status",
            Assembly.GetExecutingAssembly().Location,
            typeof(ShowStatusCommand).FullName)
        {
            ToolTip = "Show the MCP HTTP endpoint for Codex, Claude, and other clients.",
        };

        _toggleButton = panel.AddItem(toggleData) as PushButton;
        panel.AddItem(statusData);
        _updateButton = panel.AddItem(updateData) as PushButton;
        ApplyUpdateButtonState();
    }

    public void SetConnected(int port)
    {
        if (_toggleButton is not null)
        {
            _toggleButton.ItemText = $"Disconnect\n:{port}";
            _toggleButton.ToolTip = $"Stop the revit-mcp server. MCP URL: http://127.0.0.1:{port}/mcp";
        }
    }

    public void SetDisconnected()
    {
        if (_toggleButton is not null)
        {
            _toggleButton.ItemText = "Connect";
            _toggleButton.ToolTip = "Start the revit-mcp server for this Revit instance.";
        }
    }

    public void SetUpdateAvailable(string latestVersion)
        => SetUpdateButton(
            "Update\nAvailable",
            $"revit-mcp {latestVersion} is available. Click to open the release page.");

    public void SetUpToDate(string currentVersion)
        => SetUpdateButton(
            "Up to\ndate",
            $"revit-mcp is up to date ({currentVersion}). Click to check again.");

    public void SetUpdateUnknown()
        => SetUpdateButton(
            "Update",
            "Check GitHub Releases for a newer revit-mcp MSI.");

    private void SetUpdateButton(string text, string toolTip)
    {
        _updateText = text;
        _updateToolTip = toolTip;
        ApplyUpdateButtonState();
    }

    private void ApplyUpdateButtonState()
    {
        if (_updateButton is null) return;
        _updateButton.ItemText = _updateText;
        _updateButton.ToolTip = _updateToolTip;
    }
}
