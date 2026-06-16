using System.Diagnostics;
using System.Net.Http.Headers;
using System.Windows.Forms;

namespace Algomim.Aec.Mcp.Updater;

internal static class Program
{
    private const string Title = "Algomim MCP Update";

    [STAThread]
    private static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            var options = UpdateOptions.Parse(args);
            RunAsync(options).GetAwaiter().GetResult();
            return 0;
        }
        catch (Exception ex)
        {
            Log($"Update failed: {ex}");
            MessageBox.Show(
                $"Update failed: {ex.Message}",
                Title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return 1;
        }
    }

    private static async Task RunAsync(UpdateOptions options)
    {
        Log($"Starting update for {options.HostName} {options.LatestVersion}.");

        var installerPath = await DownloadInstallerAsync(options).ConfigureAwait(false);
        Log($"Installer ready: {installerPath}");

        WaitForHostToExit(options);
        StartInstaller(installerPath);
    }

    private static async Task<string> DownloadInstallerAsync(UpdateOptions options)
    {
        if (!Uri.TryCreate(options.InstallerUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Installer URL must be an HTTPS URL.");

        var installerName = GetSafeInstallerName(options, uri);
        var downloadDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Algomim",
            "AecMcp",
            "Updates",
            SanitizePathSegment(options.HostName),
            SanitizePathSegment(options.LatestVersion));

        Directory.CreateDirectory(downloadDirectory);
        var installerPath = Path.Combine(downloadDirectory, installerName);

        if (File.Exists(installerPath) && new FileInfo(installerPath).Length > 0)
            return installerPath;

        var tempPath = $"{installerPath}.download";
        if (File.Exists(tempPath))
            File.Delete(tempPath);

        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("algomim-aec-mcp-updater");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using (var source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
        await using (var target = File.Create(tempPath))
        {
            await source.CopyToAsync(target).ConfigureAwait(false);
        }

        File.Move(tempPath, installerPath, overwrite: true);
        return installerPath;
    }

    private static string GetSafeInstallerName(UpdateOptions options, Uri uri)
    {
        var installerName = string.IsNullOrWhiteSpace(options.InstallerName)
            ? Path.GetFileName(uri.LocalPath)
            : options.InstallerName;

        installerName = Path.GetFileName(installerName);
        if (string.IsNullOrWhiteSpace(installerName) ||
            !installerName.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Installer asset must be an MSI file.");
        }

        return installerName;
    }

    private static void WaitForHostToExit(UpdateOptions options)
    {
        if (options.WaitPid is null)
            return;

        Process? process;
        try
        {
            process = Process.GetProcessById(options.WaitPid.Value);
        }
        catch (ArgumentException)
        {
            return;
        }

        using (process)
        {
            if (process.HasExited)
                return;

            MessageBox.Show(
                $"{options.HostName} update {options.LatestVersion} is ready.\n\n" +
                $"Close {options.HostName}; the installer will start automatically after it closes.",
                Title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            process.WaitForExit();
        }
    }

    private static void StartInstaller(string installerPath)
    {
        Log($"Starting msiexec for {installerPath}.");

        var startInfo = new ProcessStartInfo("msiexec.exe", $"/i \"{installerPath}\"")
        {
            UseShellExecute = true,
            Verb = "runas",
            WorkingDirectory = Path.GetDirectoryName(installerPath) ?? string.Empty,
        };

        _ = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start Windows Installer.");
    }

    private static string SanitizePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value
            .Select(ch => invalid.Contains(ch) ? '-' : ch)
            .ToArray();

        var sanitized = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "update" : sanitized;
    }

    private static void Log(string message)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Algomim",
                "AecMcp",
                "Updater");
            Directory.CreateDirectory(directory);
            File.AppendAllText(
                Path.Combine(directory, "updater.log"),
                $"{DateTimeOffset.Now:u} {message}{Environment.NewLine}");
        }
        catch
        {
            // Logging must not block installation.
        }
    }

    private sealed record UpdateOptions(
        string HostName,
        string LatestVersion,
        string InstallerUrl,
        string? InstallerName,
        int? WaitPid)
    {
        public static UpdateOptions Parse(string[] args)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < args.Length; i++)
            {
                var key = args[i];
                if (!key.StartsWith("--", StringComparison.Ordinal))
                    throw new ArgumentException($"Unexpected argument '{key}'.");

                if (i + 1 >= args.Length)
                    throw new ArgumentException($"Missing value for '{key}'.");

                values[key[2..]] = args[++i];
            }

            var hostName = Require(values, "host-name");
            var latestVersion = Require(values, "latest-version");
            var installerUrl = Require(values, "installer-url");
            values.TryGetValue("installer-name", out var installerName);

            int? waitPid = null;
            if (values.TryGetValue("wait-pid", out var waitPidValue))
            {
                if (!int.TryParse(waitPidValue, out var parsedPid) || parsedPid <= 0)
                    throw new ArgumentException("wait-pid must be a positive integer.");

                waitPid = parsedPid;
            }

            return new UpdateOptions(hostName, latestVersion, installerUrl, installerName, waitPid);
        }

        private static string Require(IReadOnlyDictionary<string, string> values, string key)
        {
            if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"Missing required argument '--{key}'.");

            return value;
        }
    }
}
