using Jellyfin.Plugin.NoirMode.Core;

namespace Jellyfin.Plugin.NoirMode.Tests;

public sealed class NoirStateFileTests
{
    [Fact]
    public void StateRoundTripsThroughJsonFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "state.json");
        var state = new NoirState
        {
            Enabled = true,
            ItemOverrides =
            [
                new NoirItemOverride
                {
                    ItemId = "abc",
                    MediaPath = "/media/movie.mkv",
                    Mode = NoirOverrideMode.Preset,
                    PresetId = "film-noir"
                }
            ]
        };

        NoirStateFile.WriteAtomic(path, state);
        var loaded = NoirStateFile.Read(path);

        Assert.True(loaded.Enabled);
        Assert.Single(loaded.ItemOverrides);
        Assert.Equal("abc", loaded.ItemOverrides[0].ItemId);
    }

    [Fact]
    public void MissingStateFileFailsClosed()
    {
        var loaded = NoirStateFile.Read(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.json"));

        Assert.False(loaded.Enabled);
    }
}
