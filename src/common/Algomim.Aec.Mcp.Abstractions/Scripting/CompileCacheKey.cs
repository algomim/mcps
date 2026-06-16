using System.Security.Cryptography;
using System.Text;

namespace Algomim.Aec.Mcp.Scripting;

/// <summary>Content-addressed cache key for compiled scripts: SHA-256 of the source text.</summary>
public static class CompileCacheKey
{
    public static string Sha256(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes);
    }
}
