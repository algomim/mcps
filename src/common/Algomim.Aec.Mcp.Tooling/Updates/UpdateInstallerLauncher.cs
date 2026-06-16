using System.Diagnostics;
using System.Reflection;

namespace Algomim.Aec.Mcp.Tooling.Updates;

public sealed record UpdateInstallerLaunchResult(bool Started, string Message)
{
    public static UpdateInstallerLaunchResult StartedSuccessfully(string hostName)
        => new(
            true,
            $"The update installer is downloading in the background. Close {hostName} to finish installing.");

    public static UpdateInstallerLaunchResult NotStarted(string message)
        => new(false, message);
}

public static class UpdateInstallerLauncher
{
    public const string HelperExecutableName = "Algomim.Aec.Mcp.Updater.exe";

    public static UpdateInstallerLaunchResult Launch(
        ReleaseUpdateInfo update,
        string hostName,
        string? helperDirectory = null)
    {
        if (!update.IsUpdateAvailable)
            return UpdateInstallerLaunchResult.NotStarted("No update is available.");

        if (string.IsNullOrWhiteSpace(update.InstallerUrl) || string.IsNullOrWhiteSpace(update.InstallerName))
            return UpdateInstallerLaunchResult.NotStarted("No host-specific MSI was attached to the release.");

        helperDirectory ??= Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory;
        var helperPath = Path.Combine(helperDirectory, HelperExecutableName);
        if (!File.Exists(helperPath))
            return UpdateInstallerLaunchResult.NotStarted($"Updater helper was not found at '{helperPath}'.");

        var startInfo = new ProcessStartInfo(helperPath)
        {
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add("--host-name");
        startInfo.ArgumentList.Add(hostName);
        startInfo.ArgumentList.Add("--latest-version");
        startInfo.ArgumentList.Add(update.LatestVersion);
        startInfo.ArgumentList.Add("--installer-url");
        startInfo.ArgumentList.Add(update.InstallerUrl);
        startInfo.ArgumentList.Add("--installer-name");
        startInfo.ArgumentList.Add(update.InstallerName);
        startInfo.ArgumentList.Add("--wait-pid");
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString());

        Process.Start(startInfo);
        return UpdateInstallerLaunchResult.StartedSuccessfully(hostName);
    }
}
