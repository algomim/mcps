namespace Algomim.Aec.Mcp.Core;

/// <summary>
/// An error with a stable code and a human-readable message.
/// </summary>
public sealed record Error
{
    public string Code { get; }
    public string Message { get; }
    public Exception? Exception { get; }

    private Error(string code, string message, Exception? exception = null)
    {
        Code = code;
        Message = message;
        Exception = exception;
    }

    public static Error Of(string code, string message, Exception? exception = null) => new(code, message, exception);

    public static Error InvalidOperation(string message) => new("INVALID_OPERATION", message);
    public static Error NoActiveDocument() => new("NO_DOCUMENT", "No active document found.");
    public static Error NoActiveView() => new("NO_VIEW", "No active view found.");
    public static Error InvalidParameter(string parameterName, string reason)
        => new("INVALID_PARAMETER", $"Invalid parameter '{parameterName}': {reason}");

    public override string ToString()
        => Exception != null ? $"[{Code}] {Message} - {Exception.Message}" : $"[{Code}] {Message}";
}
