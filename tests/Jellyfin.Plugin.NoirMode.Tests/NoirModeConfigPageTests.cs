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
    public void VideoPageMoreMenuReadsApiResponsesCaseInsensitively()
    {
        var script = File.ReadAllText(FindConfigurationFilePath("noirVideoPage.js"));

        Assert.Contains("const read = (value, camelName, fallback)", script);
        Assert.Contains("dataType: 'json'", script);
        Assert.Contains("const asArray = value =>", script);
        Assert.Contains("const getMenuItemId = target =>", script);
        Assert.Contains("'.btnMoreCommands, [data-action=\"menu\"]'", script);
        Assert.Contains("const isSupportedVideoItem = item =>", script);
        Assert.Contains("itemType === 'Movie'", script);
        Assert.Contains("itemType === 'Episode'", script);
        Assert.Contains("mediaType === 'Video'", script);
        Assert.Contains("const injectNoirMenuItem = () =>", script);
        Assert.Contains("classList.add(menuItemClass)", script);
        Assert.Contains("actionSheetMenuItem", script);
        Assert.Contains("showNoirModeDialog(itemId)", script);
        Assert.Contains("const setChildren = (element, children) =>", script);
        Assert.Contains("const presetItems = asArray(presets)", script);
        Assert.Contains("const isPresetMode = mode =>", script);
        Assert.Contains("mode.toLowerCase() === 'preset'", script);
        Assert.Contains("read(override, 'mode', 0)", script);
        Assert.Contains("read(preset, 'id', '')", script);
        Assert.Contains("routeMatch[1].replace(/-/g, '')", script);
        Assert.Contains("const [presets, override] = await Promise.all", script);
        Assert.Contains("document.addEventListener('click', handleMoreMenuClick, true)", script);
        Assert.True(
            script.IndexOf("const [presets, override] = await Promise.all", StringComparison.Ordinal)
                < script.IndexOf("setChildren(scroller, buttons);", StringComparison.Ordinal),
            "The preset menu should not be populated until its API data has loaded.");
        Assert.Contains("ApiClient.getItem", script);
        Assert.DoesNotContain("override.mode", script);
        Assert.DoesNotContain("preset.id", script);
        Assert.DoesNotContain("replaceChildren", script);
        Assert.DoesNotContain("replaceAll", script);
        Assert.DoesNotContain("trackSelections", script);
        Assert.DoesNotContain("selectContainer", script);
        Assert.DoesNotContain("document.createElement('select'", script);
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
