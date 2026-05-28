namespace Jellyfin.Plugin.NoirMode.Tests;

public sealed class NoirModeControllerBehaviorTests
{
    [Fact]
    public void GetOverrideOnlyReturnsDefaultsForVideoItems()
    {
        var source = File.ReadAllText(FindSourceFilePath("NoirModeController.cs"));
        var actionStart = source.IndexOf("public ActionResult<NoirItemOverride> GetOverride", StringComparison.Ordinal);
        var nextActionStart = source.IndexOf("[HttpPut(\"items/{itemId}/override\")]", StringComparison.Ordinal);

        Assert.True(actionStart >= 0);
        Assert.True(nextActionStart > actionStart);

        var action = source[actionStart..nextActionStart];
        Assert.Contains("var item = TryGetItem(itemId);", action);
        Assert.Contains("if (item is null)", action);
        Assert.Contains("if (!IsVideoItem(item))", action);
        Assert.Contains("Noir Mode is configured per episode/video, not at the series level.", action);
        Assert.True(
            action.IndexOf("if (!IsVideoItem(item))", StringComparison.Ordinal)
                < action.IndexOf("var config = GetConfiguration();", StringComparison.Ordinal));
    }

    private static string FindSourceFilePath(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, "src", "Jellyfin.Plugin.NoirMode", "Controllers", fileName);
            if (File.Exists(path))
            {
                return path;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(fileName);
    }
}
