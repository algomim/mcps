using System.Linq;
using Autodesk.Windows;
using Algomim.Aec.Mcp.Hosting;
using Algomim.AutoCad.Mcp.App;

namespace Algomim.AutoCad.Mcp.UI;

/// <summary>Builds the AutoCAD ribbon panel for connecting and disconnecting the MCP host.</summary>
public sealed class RibbonController
{
    private const string FallbackTabId = "Algomim.AutoCad.Mcp.AddIns.Tab";
    private const string PanelId = "Algomim.AutoCad.Mcp.Panel";
    private const string ButtonId = "Algomim.AutoCad.Mcp.Toggle";
    private const string StatusButtonId = "Algomim.AutoCad.Mcp.Status";
    private const string UpdateButtonId = "Algomim.AutoCad.Mcp.Update";
    private const string AddInsTabTitle = "Add-ins";
    private const string PanelTitle = "autocad-mcp";

    private readonly IMcpLogger _logger;
    private bool _missingRibbonLogged;
    private RibbonButton? _toggleButton;
    private RibbonButton? _statusButton;
    private RibbonButton? _updateButton;
    private string _updateText = "Update";
    private string _updateToolTip = "Check GitHub Releases for a newer autocad-mcp MSI.";

    public RibbonController(IMcpLogger logger)
    {
        _logger = logger;
    }

    public bool IsBuilt => _toggleButton is not null;

    public bool Build()
    {
        try
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon is null)
            {
                if (!_missingRibbonLogged)
                {
                    _logger.Warn("AutoCAD ribbon is not available yet.");
                    _missingRibbonLogged = true;
                }

                return false;
            }

            var tab = FindAddInsTab(ribbon);
            if (tab is null)
            {
                tab = new RibbonTab { Id = FallbackTabId, Title = AddInsTabTitle };
                ribbon.Tabs.Add(tab);
            }

            var panel = tab.Panels.FirstOrDefault(item => item.Source.Id == PanelId);
            if (panel is null)
            {
                var source = new RibbonPanelSource { Id = PanelId, Title = PanelTitle };
                panel = new RibbonPanel { Source = source };
                tab.Panels.Add(panel);
            }

            _toggleButton = panel.Source.Items
                .OfType<RibbonButton>()
                .FirstOrDefault(item => item.Id == ButtonId);

            if (_toggleButton is null)
            {
                _toggleButton = new RibbonButton
                {
                    Id = ButtonId,
                    Text = "Connect",
                    ShowText = true,
                    Size = RibbonItemSize.Large,
                    CommandHandler = new RibbonCommandHandler(() => AutoCadMcpApp.Instance?.Toggle()),
                    ToolTip = "Start or stop the autocad-mcp server for this AutoCAD instance.",
                };

                panel.Source.Items.Add(_toggleButton);
            }

            _statusButton = panel.Source.Items
                .OfType<RibbonButton>()
                .FirstOrDefault(item => item.Id == StatusButtonId);

            if (_statusButton is null)
            {
                _statusButton = new RibbonButton
                {
                    Id = StatusButtonId,
                    Text = "Status",
                    ShowText = true,
                    Size = RibbonItemSize.Standard,
                    CommandHandler = new RibbonCommandHandler(() => AutoCadMcpApp.Instance?.ShowStatus()),
                    ToolTip = "Show the MCP HTTP endpoint for Codex, Claude, and other clients.",
                };

                panel.Source.Items.Add(_statusButton);
            }

            _updateButton = panel.Source.Items
                .OfType<RibbonButton>()
                .FirstOrDefault(item => item.Id == UpdateButtonId);

            if (_updateButton is null)
            {
                _updateButton = new RibbonButton
                {
                    Id = UpdateButtonId,
                    Text = "Update",
                    ShowText = true,
                    Size = RibbonItemSize.Standard,
                    CommandHandler = new RibbonCommandHandler(() => AutoCadMcpApp.Instance?.CheckForUpdates()),
                    ToolTip = "Check GitHub Releases for a newer autocad-mcp MSI.",
                };

                panel.Source.Items.Add(_updateButton);
            }

            ApplyUpdateButtonState();
            return _toggleButton is not null;
        }
        catch (Exception ex)
        {
            _logger.Error("AutoCAD ribbon build failed", ex);
            return false;
        }
    }

    public void SetConnected(int port)
    {
        if (_toggleButton is null) return;
        _toggleButton.Text = $"Disconnect :{port}";
        _toggleButton.ToolTip = $"Stop the autocad-mcp server. MCP URL: http://127.0.0.1:{port}/mcp";
    }

    public void SetDisconnected()
    {
        if (_toggleButton is null) return;
        _toggleButton.Text = "Connect";
        _toggleButton.ToolTip = "Start the autocad-mcp server for this AutoCAD instance.";
    }

    public void SetUpdateAvailable(string latestVersion)
        => SetUpdateButton(
            "Update Available",
            $"autocad-mcp {latestVersion} is available. Click to open the release page.");

    public void SetUpToDate(string currentVersion)
        => SetUpdateButton(
            "Up to date",
            $"autocad-mcp is up to date ({currentVersion}). Click to check again.");

    public void SetUpdateUnknown()
        => SetUpdateButton(
            "Update",
            "Check GitHub Releases for a newer autocad-mcp MSI.");

    private void SetUpdateButton(string text, string toolTip)
    {
        _updateText = text;
        _updateToolTip = toolTip;
        ApplyUpdateButtonState();
    }

    private void ApplyUpdateButtonState()
    {
        if (_updateButton is null) return;
        _updateButton.Text = _updateText;
        _updateButton.ToolTip = _updateToolTip;
    }

    private static RibbonTab? FindAddInsTab(RibbonControl ribbon)
        => ribbon.Tabs.FirstOrDefault(IsAddInsTab);

    private static bool IsAddInsTab(RibbonTab tab)
    {
        if (tab.Title.Equals(AddInsTabTitle, StringComparison.OrdinalIgnoreCase) ||
            tab.Title.Equals("Add-Ins", StringComparison.OrdinalIgnoreCase))
            return true;

        var id = tab.Id.ToLowerInvariant();
        return id.Contains("addin") || id.Contains("addins") || id.Contains("add-ins");
    }
}
