using System.IO;
using System.Text;
using System.Text.Json;
using Algomim.Aec.Mcp.Hosting;
using Algomim.Revit.Mcp.Harness;

namespace Algomim.Revit.Mcp.Hosting;

/// <summary>
/// Writes this Revit process's live MCP announcement under the local MCP runtime discovery directory.
/// Each process owns one file, so instances never race on a shared registry document.
/// </summary>
public sealed class AnnouncementWriter
{
    private static readonly string Directory_ = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Temp",
        "mcp-runtime",
        "announcements");

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly ILogger? _logger;
    private string? _filePath;

    public AnnouncementWriter(ILogger? logger = null)
    {
        _logger = logger;
        _logger?.Info($"MCP announcement directory: {Directory_}");
    }

    public void Write(AnnouncementEntry entry)
    {
        try
        {
            System.IO.Directory.CreateDirectory(Directory_);
            Prepare(entry);

            _filePath = Path.Combine(Directory_, $"{SafeFileName(entry.Id)}.json");
            var temp = _filePath + ".tmp";

            File.WriteAllText(temp, JsonSerializer.Serialize(entry, Options));
            File.Move(temp, _filePath, true);
            _logger?.Info($"MCP announcement written: {_filePath}");
        }
        catch (Exception ex)
        {
            _logger?.Error("MCP announcement write failed", ex);
            // Announcement is best-effort; never throw into Revit.
        }
    }

    public void Remove(int pid)
    {
        try
        {
            DeleteIfExists(_filePath);
            if (!System.IO.Directory.Exists(Directory_)) return;

            foreach (var file in System.IO.Directory.GetFiles(Directory_, $"revit-{pid}-*.json"))
                DeleteIfExists(file);

            _logger?.Info($"MCP announcement removed for pid {pid}");
        }
        catch (Exception ex)
        {
            _logger?.Error("MCP announcement removal failed", ex);
            // Announcement removal is best-effort; runtime discovery still handles stale files.
        }
    }

    private static void Prepare(AnnouncementEntry entry)
    {
        entry.Owner = string.IsNullOrWhiteSpace(entry.Owner) ? "revit" : entry.Owner.Trim();
        entry.Id = string.IsNullOrWhiteSpace(entry.Id) ? $"revit-{entry.Pid}-{entry.Port}" : entry.Id.Trim();
        entry.Name = string.IsNullOrWhiteSpace(entry.Name) ? $"Revit {entry.Pid}" : entry.Name.Trim();

        var now = DateTimeOffset.UtcNow;
        if (entry.StartedAt == default) entry.StartedAt = now;
        entry.UpdatedAt = now;
    }

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
            builder.Append(Array.IndexOf(invalid, ch) >= 0 ? '-' : ch);
        return builder.ToString();
    }

    private static void DeleteIfExists(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup.
        }
    }
}
