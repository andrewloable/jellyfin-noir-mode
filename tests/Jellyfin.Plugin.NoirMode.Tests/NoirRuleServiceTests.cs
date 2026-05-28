using Jellyfin.Plugin.NoirMode.Core;

namespace Jellyfin.Plugin.NoirMode.Tests;

public sealed class NoirRuleServiceTests
{
    [Fact]
    public void DisabledPluginReturnsDisabled()
    {
        var service = new NoirRuleService();
        var state = new NoirState { Enabled = false };

        var result = service.Resolve(state, new NoirMediaLookup("abc", "movie.mkv"));

        Assert.False(result.ShouldApply);
        Assert.Equal("plugin-disabled", result.Reason);
    }

    [Fact]
    public void MissingOverrideDefaultsToDisabled()
    {
        var service = new NoirRuleService();
        var state = new NoirState { Enabled = true };

        var result = service.Resolve(state, new NoirMediaLookup("abc", "movie.mkv"));

        Assert.False(result.ShouldApply);
        Assert.Equal("no-item-override", result.Reason);
    }

    [Fact]
    public void ItemPresetOverrideAppliesByItemId()
    {
        var service = new NoirRuleService();
        var state = new NoirState
        {
            Enabled = true,
            ItemOverrides =
            [
                new NoirItemOverride
                {
                    ItemId = "abc",
                    Mode = NoirOverrideMode.Preset,
                    PresetId = "classic-bw"
                }
            ]
        };

        var result = service.Resolve(state, new NoirMediaLookup("abc", null));

        Assert.True(result.ShouldApply);
        Assert.Equal("classic-bw", result.Preset?.Id);
    }

    [Fact]
    public void ItemPresetOverrideAppliesByNormalizedPathHash()
    {
        var path = NoirPath.Normalize(@"C:\Media\Movie.mkv");
        var service = new NoirRuleService();
        var state = new NoirState
        {
            Enabled = true,
            ItemOverrides =
            [
                new NoirItemOverride
                {
                    ItemId = "abc",
                    MediaPathHash = NoirPath.Hash(path),
                    Mode = NoirOverrideMode.Preset,
                    PresetId = "film-noir"
                }
            ]
        };

        var result = service.Resolve(state, new NoirMediaLookup(null, "c:/media/movie.mkv"));

        Assert.True(result.ShouldApply);
        Assert.Equal("film-noir", result.Preset?.Id);
    }

    [Fact]
    public void OffOverrideReturnsDisabled()
    {
        var service = new NoirRuleService();
        var state = new NoirState
        {
            Enabled = true,
            ItemOverrides =
            [
                new NoirItemOverride
                {
                    ItemId = "abc",
                    Mode = NoirOverrideMode.Off,
                    PresetId = "film-noir"
                }
            ]
        };

        var result = service.Resolve(state, new NoirMediaLookup("abc", null));

        Assert.False(result.ShouldApply);
        Assert.Equal("item-disabled", result.Reason);
    }
}
