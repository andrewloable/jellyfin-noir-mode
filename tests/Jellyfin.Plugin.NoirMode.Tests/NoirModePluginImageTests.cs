namespace Jellyfin.Plugin.NoirMode.Tests;

public sealed class NoirModePluginImageTests
{
    [Fact]
    public void PluginImageAssetExists()
    {
        var imagePath = FindRepositoryFilePath(
            "src",
            "Jellyfin.Plugin.NoirMode",
            "Images",
            "plugin.png");

        var bytes = File.ReadAllBytes(imagePath);

        Assert.True(bytes.Length > 0);
        byte[] pngHeader = [0x89, 0x50, 0x4e, 0x47];
        Assert.True(bytes.Take(4).SequenceEqual(pngHeader));
    }

    [Fact]
    public void PackageScriptWritesLocalPluginManifestImagePath()
    {
        var packageScript = File.ReadAllText(FindRepositoryFilePath("scripts", "package.ps1"));

        Assert.Contains("meta.json", packageScript);
        Assert.Contains("imagePath = 'plugin.png'", packageScript);
        Assert.Contains("'Jellyfin.Plugin.NoirMode.dll'", packageScript);
        Assert.Contains("'Jellyfin.Plugin.NoirMode.Core.dll'", packageScript);
    }

    [Fact]
    public void PackageScriptPublishesPluginImagePath()
    {
        var packageScript = File.ReadAllText(FindRepositoryFilePath("scripts", "package.ps1"));

        Assert.Contains("Images/plugin.png", packageScript);
        Assert.Contains("plugin.png", packageScript);
        Assert.Contains("imageUrl = $pluginImageUrl", packageScript);
        Assert.Contains("Destination (Join-Path $root $manifestName)", packageScript);
    }

    private static string FindRepositoryFilePath(params string[] pathParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var combinedPathParts = new string[pathParts.Length + 1];
            combinedPathParts[0] = directory.FullName;
            Array.Copy(pathParts, 0, combinedPathParts, 1, pathParts.Length);

            var path = Path.Combine(combinedPathParts);
            if (File.Exists(path))
            {
                return path;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate {Path.Combine(pathParts)}.");
    }
}
