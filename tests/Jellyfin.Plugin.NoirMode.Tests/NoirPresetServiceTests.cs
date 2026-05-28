using Jellyfin.Plugin.NoirMode.Core;

namespace Jellyfin.Plugin.NoirMode.Tests;

public sealed class NoirPresetServiceTests
{
    [Fact]
    public void BuiltInPresetsResolveById()
    {
        var service = new NoirPresetService();

        var preset = service.GetRequired("film-noir");

        Assert.Equal("Film Noir", preset.Label);
        Assert.Contains("hue=s=0", preset.Filter, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownPresetIsRejected()
    {
        var service = new NoirPresetService();

        Assert.Throws<ArgumentException>(() => service.GetRequired("unsafe-custom"));
    }
}
