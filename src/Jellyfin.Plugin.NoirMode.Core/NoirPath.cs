using System.Security.Cryptography;
using System.Text;

namespace Jellyfin.Plugin.NoirMode.Core;

public static class NoirPath
{
    public static string? Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var value = path.Trim().Trim('"');
        if (value.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.IsFile)
        {
            value = uri.LocalPath;
        }

        value = value.Replace('\\', '/');

        while (value.Contains("//", StringComparison.Ordinal))
        {
            value = value.Replace("//", "/", StringComparison.Ordinal);
        }

        return value.TrimEnd('/').ToUpperInvariant();
    }

    public static string? Hash(string? normalizedPath)
    {
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return null;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedPath));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
