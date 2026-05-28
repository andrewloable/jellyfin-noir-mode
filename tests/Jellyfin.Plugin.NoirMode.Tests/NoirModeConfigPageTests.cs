namespace Jellyfin.Plugin.NoirMode.Tests;

public sealed class NoirModeConfigPageTests
{
    [Fact]
    public void ConfigPageReadsApiResponsesCaseInsensitively()
    {
        var page = File.ReadAllText(FindConfigPagePath());

        Assert.Contains("const read = (value, camelName, fallback)", page);
        Assert.Contains("read(config, 'enabled', false)", page);
        Assert.DoesNotContain("config.enabled", page);
        Assert.DoesNotContain("status.jellyfinUsesWrapper", page);
    }

    [Fact]
    public void VideoPageSelectorReadsApiResponsesCaseInsensitively()
    {
        var script = File.ReadAllText(FindConfigurationFilePath("noirVideoPage.js"));

        Assert.Contains("const read = (value, camelName, fallback)", script);
        Assert.Contains("read(override, 'mode', 0)", script);
        Assert.Contains("read(preset, 'id', '')", script);
        Assert.Contains("const [presets, override] = await Promise.all", script);
        Assert.Contains("const container = ensureContainer(page);", script);
        Assert.True(
            script.IndexOf("const [presets, override] = await Promise.all", StringComparison.Ordinal)
                < script.IndexOf("const container = ensureContainer(page);", StringComparison.Ordinal),
            "The selector should not be inserted until its API data has loaded.");
        Assert.DoesNotContain("override.mode", script);
        Assert.DoesNotContain("preset.id", script);
    }

    private static string FindConfigPagePath()
        => FindConfigurationFilePath("configPage.html");

    private static string FindConfigurationFilePath(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = Path.Combine(
                directory.FullName,
                "src",
                "Jellyfin.Plugin.NoirMode",
                "Configuration",
                fileName);

            if (File.Exists(path))
            {
                return path;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate {fileName}.");
    }
}
