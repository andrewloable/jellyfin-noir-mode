namespace Jellyfin.Plugin.NoirMode.Wrapper;

using System.Text.Json;

public sealed record WrapperOptions(string RealFfmpegPath, string StateFilePath, string ConfigPath, bool UsedConfigFile)
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
        var stateFile = Environment.GetEnvironmentVariable("NOIR_STATE_FILE") ?? config?.StateFilePath;

        if (string.IsNullOrWhiteSpace(realFfmpeg))
        {
            throw new InvalidOperationException("Real FFmpeg path is required. Configure the plugin wrapper setup or set NOIR_REAL_FFMPEG.");
        }

        if (string.IsNullOrWhiteSpace(stateFile))
        {
            throw new InvalidOperationException("Noir state file path is required. Configure the plugin wrapper setup or set NOIR_STATE_FILE.");
        }

        return new WrapperOptions(realFfmpeg, stateFile, configPath, config is not null);
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
}
