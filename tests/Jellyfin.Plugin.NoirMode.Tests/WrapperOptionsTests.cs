using System.Text.Json;
using Jellyfin.Plugin.NoirMode.Wrapper;

namespace Jellyfin.Plugin.NoirMode.Tests;

public sealed class WrapperOptionsTests
{
    [Fact]
    public void ReadsOptionsFromConfigFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var configPath = Path.Combine(directory, "jellyfin-noir-wrapper.json");
        File.WriteAllText(configPath, JsonSerializer.Serialize(new WrapperConfig
        {
            RealFfmpegPath = "/usr/bin/ffmpeg",
            StateFilePath = "/config/plugins/configurations/jellyfin-noir-mode-state.json"
        }));

        var oldConfig = Environment.GetEnvironmentVariable("NOIR_WRAPPER_CONFIG");
        var oldRealFfmpeg = Environment.GetEnvironmentVariable("NOIR_REAL_FFMPEG");
        var oldStateFile = Environment.GetEnvironmentVariable("NOIR_STATE_FILE");
        try
        {
            Environment.SetEnvironmentVariable("NOIR_WRAPPER_CONFIG", configPath);
            Environment.SetEnvironmentVariable("NOIR_REAL_FFMPEG", null);
            Environment.SetEnvironmentVariable("NOIR_STATE_FILE", null);

            var options = WrapperOptions.FromEnvironmentOrConfig();

            Assert.Equal("/usr/bin/ffmpeg", options.RealFfmpegPath);
            Assert.Equal("/config/plugins/configurations/jellyfin-noir-mode-state.json", options.StateFilePath);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NOIR_WRAPPER_CONFIG", oldConfig);
            Environment.SetEnvironmentVariable("NOIR_REAL_FFMPEG", oldRealFfmpeg);
            Environment.SetEnvironmentVariable("NOIR_STATE_FILE", oldStateFile);
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void EnvironmentOverridesConfigFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var configPath = Path.Combine(directory, "jellyfin-noir-wrapper.json");
        File.WriteAllText(configPath, JsonSerializer.Serialize(new WrapperConfig
        {
            RealFfmpegPath = "/usr/bin/ffmpeg",
            StateFilePath = "/state.json"
        }));

        var oldConfig = Environment.GetEnvironmentVariable("NOIR_WRAPPER_CONFIG");
        var oldRealFfmpeg = Environment.GetEnvironmentVariable("NOIR_REAL_FFMPEG");
        var oldStateFile = Environment.GetEnvironmentVariable("NOIR_STATE_FILE");
        try
        {
            Environment.SetEnvironmentVariable("NOIR_WRAPPER_CONFIG", configPath);
            Environment.SetEnvironmentVariable("NOIR_REAL_FFMPEG", "/custom/ffmpeg");
            Environment.SetEnvironmentVariable("NOIR_STATE_FILE", "/custom/state.json");

            var options = WrapperOptions.FromEnvironmentOrConfig();

            Assert.Equal("/custom/ffmpeg", options.RealFfmpegPath);
            Assert.Equal("/custom/state.json", options.StateFilePath);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NOIR_WRAPPER_CONFIG", oldConfig);
            Environment.SetEnvironmentVariable("NOIR_REAL_FFMPEG", oldRealFfmpeg);
            Environment.SetEnvironmentVariable("NOIR_STATE_FILE", oldStateFile);
            Directory.Delete(directory, recursive: true);
        }
    }
}
