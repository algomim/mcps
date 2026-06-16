using System.Text.Json;
using Algomim.Aec.Mcp.Hosting;
using Algomim.Aec.Mcp.Tooling;
using Xunit;

namespace Algomim.Revit.Mcp.Tests;

public class McpSerializationTests
{
    [Fact]
    public void TextContent_serializes_with_runtime_type_not_empty_object()
    {
        var json = JsonSerializer.Serialize(McpToolResult.Success("hello"), McpJson.Default);

        Assert.Contains("\"type\":\"text\"", json);
        Assert.Contains("\"text\":\"hello\"", json);
        Assert.DoesNotContain("[{}]", json);
    }

    [Fact]
    public void Error_result_sets_isError_true()
    {
        var json = JsonSerializer.Serialize(McpToolResult.Error("boom"), McpJson.Default);

        Assert.Contains("\"isError\":true", json);
        Assert.Contains("boom", json);
    }

    [Fact]
    public void Success_omits_isError_when_false()
    {
        var json = JsonSerializer.Serialize(McpToolResult.Success("ok"), McpJson.Default);

        Assert.Contains("\"isError\":false", json);
    }

    [Fact]
    public void AnnouncementEntry_serializes_algomim_discovery_shape()
    {
        var json = JsonSerializer.Serialize(new AnnouncementEntry
        {
            Id = "revit-123-48884",
            Owner = "revit",
            Name = "Tower.rvt",
            Pid = 123,
            Version = "2026",
            Document = "Tower.rvt",
            Port = 48884,
            Url = "http://127.0.0.1:48884/mcp",
            HealthUrl = "http://127.0.0.1:48884/health",
            StartedAt = DateTimeOffset.Parse("2026-06-14T00:00:00Z"),
            UpdatedAt = DateTimeOffset.Parse("2026-06-14T00:00:00Z"),
        });

        Assert.Contains("\"owner\":\"revit\"", json);
        Assert.Contains("\"url\":\"http://127.0.0.1:48884/mcp\"", json);
        Assert.Contains("\"healthUrl\":\"http://127.0.0.1:48884/health\"", json);
    }

    [Fact]
    public void ToolResponse_success_uses_standard_typed_tool_envelope()
    {
        var json = JsonSerializer.Serialize(ToolResponse.Success(new { count = 2 }, "done"), McpJson.Default);

        Assert.Contains("\"ok\":true", json);
        Assert.Contains("\"summary\":\"done\"", json);
        Assert.Contains("\"warnings\":[]", json);
        Assert.Contains("\"count\":2", json);
    }

    [Fact]
    public void ToolResponse_failure_uses_standard_error_envelope()
    {
        var json = JsonSerializer.Serialize(ToolResponse.Failure("NO_ACTIVE_DOCUMENT", "No active Revit document."), McpJson.Default);

        Assert.Contains("\"ok\":false", json);
        Assert.Contains("\"code\":\"NO_ACTIVE_DOCUMENT\"", json);
        Assert.Contains("\"message\":\"No active Revit document.\"", json);
        Assert.Contains("\"warnings\":[]", json);
    }
}
