namespace Jellyfin.Plugin.NoirMode.Tests;

public sealed class NoirModeConfigPageTests
{
    [Fact]
    public void ConfigPageReadsApiResponsesCaseInsensitively()
    {
        var page = File.ReadAllText(FindConfigPagePath());

        Assert.Contains("const read = (value, camelName, fallback)", page);
        Assert.Contains("dataType: 'json'", page);
        Assert.Contains("read(config, 'enabled', false)", page);
        Assert.DoesNotContain("config.enabled", page);
        Assert.DoesNotContain("status.jellyfinUsesWrapper", page);
        Assert.DoesNotContain("replaceChildren", page);
        Assert.DoesNotContain("Per-video override", page);
        Assert.DoesNotContain("ItemSearch", page);
        Assert.DoesNotContain("PresetId", page);
        Assert.DoesNotContain("SaveItemOverride", page);
        Assert.DoesNotContain("ClearItemOverride", page);
        Assert.DoesNotContain("TestWrapper", page);
        Assert.DoesNotContain("Test wrapper", page);
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
        Assert.Contains("className = 'selectContainer noirModeSelectionContainer trackSelectionFieldContainer flex-shrink-zero'", script);
        Assert.Contains("document.createElement('select', { is: 'emby-select' })", script);
        Assert.Contains("className = 'selectNoirMode detailTrackSelect emby-select-withcolor emby-select noirModeNativeSelect'", script);
        Assert.Contains("select.setAttribute('is', 'emby-select')", script);
        Assert.Contains("select.setAttribute('label', '')", script);
        Assert.Contains("className = 'selectArrowContainer'", script);
        Assert.Contains("className = 'selectArrow material-icons keyboard_arrow_down'", script);
        Assert.Contains("event.stopPropagation()", script);
        Assert.Contains("select.onmousedown = stopSelectEvent", script);
        Assert.Contains("select.ontouchstart = stopSelectEvent", script);
        Assert.Contains("const isSelectorInteractionActive = () =>", script);
        Assert.Contains("if (isSelectorInteractionActive())", script);
        Assert.Contains("select.onfocus = pauseSelectorRefresh", script);
        Assert.Contains("select.onblur = () =>", script);
        Assert.Contains("isSelectorInteractionActive() ? 750 : 150", script);
        Assert.Contains("const isPresetMode = mode =>", script);
        Assert.Contains("mode.toLowerCase() === 'preset'", script);
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
        Assert.DoesNotContain("min-height: 2em", script);
        Assert.DoesNotContain("grid-template-columns", script);
        Assert.DoesNotContain("getBoundingClientRect", script);
    }

    [Fact]
    public void WrapperServiceRestoresExecutableBitForBundledWrappers()
    {
        var service = File.ReadAllText(FindProjectFilePath("Services", "FFmpegWrapperService.cs"));

        Assert.Contains("EnsureWrapperExecutable(wrapperPath)", service);
        Assert.Contains("File.GetUnixFileMode(wrapperPath)", service);
        Assert.Contains("File.SetUnixFileMode(wrapperPath", service);
        Assert.Contains("UnixFileMode.UserExecute", service);
    }

    private static string FindConfigPagePath()
        => FindConfigurationFilePath("configPage.html");

    private static string FindConfigurationFilePath(string fileName)
        => FindProjectFilePath("Configuration", fileName);

    private static string FindProjectFilePath(string projectDirectory, string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = Path.Combine(
                directory.FullName,
                "src",
                "Jellyfin.Plugin.NoirMode",
                projectDirectory,
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
