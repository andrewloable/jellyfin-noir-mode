namespace Jellyfin.Plugin.NoirMode.Wrapper;

using System.Text.Json;

public sealed record WrapperOptions(
    string RealFfmpegPath,
    string RealFfprobePath,
    string StateFilePath,
    string ConfigPath,
    bool UsedConfigFile,
    bool DebugLogging)
{
    public static WrapperOptions FromEnvironmentOrConfig()
    {
        var configPath = Environment.GetEnvironmentVariable("NOIR_WRAPPER_CONFIG");
        if (string.IsNullOrWhiteSpace(configPath))
        {
            configPath = Path.Combine(AppContext.BaseDirectory, "jellyfin-noir-wrapper.json");
        }

        var config = ReadConfig(configPath);
        var realFfmpeg = Environment.GetEnvironmentVariable("NOIR_REAL_FFMPEG") ?? config?.RealFfmpegPath;
        var realFfprobe = Environment.GetEnvironmentVariable("NOIR_REAL_FFPROBE") ?? config?.RealFfprobePath;
        var stateFile = Environment.GetEnvironmentVariable("NOIR_STATE_FILE") ?? config?.StateFilePath;
        var debugLogging = ParseBoolean(Environment.GetEnvironmentVariable("NOIR_WRAPPER_DEBUG")) ?? config?.DebugLogging ?? false;

        if (string.IsNullOrWhiteSpace(realFfmpeg))
        {
            throw new InvalidOperationException("Real FFmpeg path is required. Configure the plugin wrapper setup or set NOIR_REAL_FFMPEG.");
        }

        if (string.IsNullOrWhiteSpace(stateFile))
        {
            throw new InvalidOperationException("Noir state file path is required. Configure the plugin wrapper setup or set NOIR_STATE_FILE.");
        }

        if (string.IsNullOrWhiteSpace(realFfprobe))
        {
            realFfprobe = GetSiblingToolPath(realFfmpeg, "ffprobe");
        }

        return new WrapperOptions(realFfmpeg, realFfprobe, stateFile, configPath, config is not null, debugLogging);
    }

    private static WrapperConfig? ReadConfig(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return null;
        }

        var json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<WrapperConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    private static string GetSiblingToolPath(string toolPath, string toolName)
    {
        var directory = Path.GetDirectoryName(toolPath);
        var extension = Path.GetExtension(toolPath);
        var fileName = string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase)
            ? $"{toolName}.exe"
            : toolName;

        return string.IsNullOrWhiteSpace(directory)
            ? fileName
            : Path.Combine(directory, fileName);
    }

    private static bool? ParseBoolean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (bool.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return value is "1" or "yes" or "on";
    }
}
