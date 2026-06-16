using System.Text.Json;
using System.Text.Json.Serialization;

namespace Algomim.Aec.Mcp.Protocol;

/// <summary>Base for all JSON-RPC 2.0 messages.</summary>
public abstract class JsonRpcMessage
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
}

/// <summary>A JSON-RPC request (has an id).</summary>
public sealed class JsonRpcRequest : JsonRpcMessage
{
    [JsonPropertyName("id")]
    public JsonElement Id { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Params { get; set; }
}

/// <summary>A JSON-RPC response (result xor error).</summary>
public sealed class JsonRpcResponse : JsonRpcMessage
{
    [JsonPropertyName("id")]
    public JsonElement Id { get; set; }

    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonRpcError? Error { get; set; }
}

/// <summary>A JSON-RPC error object with the standard codes.</summary>
public sealed class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }

    public static JsonRpcError ParseError(string message) => new() { Code = -32700, Message = message };
    public static JsonRpcError InvalidRequest(string message) => new() { Code = -32600, Message = message };
    public static JsonRpcError MethodNotFound(string method) => new() { Code = -32601, Message = $"Method not found: {method}" };
    public static JsonRpcError InvalidParams(string message) => new() { Code = -32602, Message = message };
    public static JsonRpcError InternalError(string message) => new() { Code = -32603, Message = message };
}
