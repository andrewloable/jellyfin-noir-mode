namespace Jellyfin.Plugin.NoirMode.Tests;

public sealed class NoirModeConfigPageTests
{
    [Fact]
    public void ConfigPageReadsApiResponsesCaseInsensitively()
    {
        var page = File.ReadAllText(FindConfigPagePath());

        Assert.Contains("const read = (value, camelName, fallback)", page);
        Assert.Contains("dataType: 'json'", page);
        Assert.Contains("const asArray = value =>", page);
        Assert.Contains("read(config, 'enabled', false)", page);
        Assert.DoesNotContain("config.enabled", page);
        Assert.DoesNotContain("status.jellyfinUsesWrapper", page);
        Assert.DoesNotContain("replaceChildren", page);
    }

    [Fact]
    public void VideoPageSelectorReadsApiResponsesCaseInsensitively()
    {
        var script = File.ReadAllText(FindConfigurationFilePath("noirVideoPage.js"));

        Assert.Contains("const read = (value, camelName, fallback)", script);
        Assert.Contains("dataType: 'json'", script);
        Assert.Contains("const asArray = value =>", script);
        Assert.Contains("const findInsertionAnchor = page =>", script);
        Assert.Contains("const findModernMediaAnchor = page =>", script);
        Assert.Contains("const presetItems = asArray(presets)", script);
        Assert.Contains("className = 'noirModeNativeSelect'", script);
        Assert.Contains("event.stopPropagation()", script);
        Assert.Contains("select.onmousedown = stopSelectEvent", script);
        Assert.Contains("select.ontouchstart = stopSelectEvent", script);
        Assert.Contains("read(override, 'mode', 0)", script);
        Assert.Contains("read(preset, 'id', '')", script);
        Assert.Contains("routeMatch[1].replaceAll('-', '')", script);
        Assert.Contains("const [presets, override] = await Promise.all", script);
        Assert.Contains("const container = ensureContainer(page, anchor);", script);
        Assert.True(
            script.IndexOf("const [presets, override] = await Promise.all", StringComparison.Ordinal)
                < script.IndexOf("const container = ensureContainer(page, anchor);", StringComparison.Ordinal),
            "The selector should not be inserted until its API data has loaded.");
        Assert.DoesNotContain("ApiClient.getItem", script);
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
