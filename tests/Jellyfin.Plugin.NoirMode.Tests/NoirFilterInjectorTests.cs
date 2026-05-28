using Jellyfin.Plugin.NoirMode.Core;

namespace Jellyfin.Plugin.NoirMode.Tests;

public sealed class NoirFilterInjectorTests
{
    [Fact]
    public void DisabledModePassesThroughUnchanged()
    {
        var injector = new NoirFilterInjector();

        var decision = injector.Inject(["-i", "movie.mkv", "out.m3u8"], new NoirState { Enabled = true });

        Assert.False(decision.Modified);
        Assert.Equal("no-item-override", decision.Reason);
    }

    [Fact]
    public void AppendsExistingVideoFilter()
    {
        var injector = new NoirFilterInjector();
        var state = StateForPath("movie.mkv");

        var decision = injector.Inject(["-i", "movie.mkv", "-vf", "scale=1280:-2", "out.m3u8"], state);

        Assert.True(decision.Applied);
        Assert.Contains("scale=1280:-2,hue=s=0", decision.Arguments);
    }

    [Fact]
    public void InsertsVideoFilterBeforeOutputPath()
    {
        var injector = new NoirFilterInjector();
        var state = StateForPath("movie.mkv");

        var decision = injector.Inject(["-i", "movie.mkv", "-f", "hls", "out.m3u8"], state);

        Assert.True(decision.Applied);
        Assert.Equal(["-i", "movie.mkv", "-f", "hls", "-vf", "hue=s=0", "out.m3u8"], decision.Arguments);
    }

    [Fact]
    public void SkipsFilterComplex()
    {
        var injector = new NoirFilterInjector();
        var state = StateForPath("movie.mkv");

        var decision = injector.Inject(["-i", "movie.mkv", "-filter_complex", "[0:v]scale=1280:-2[v]", "out.m3u8"], state);

        Assert.False(decision.Modified);
        Assert.Equal("filter-complex-unsupported", decision.Reason);
    }

    [Fact]
    public void SkipsVideoStreamCopy()
    {
        var injector = new NoirFilterInjector();
        var state = StateForPath("movie.mkv");

        var decision = injector.Inject(["-i", "movie.mkv", "-c:v", "copy", "out.m3u8"], state);

        Assert.False(decision.Modified);
        Assert.Equal("video-stream-copy-unsupported", decision.Reason);
    }

    [Fact]
    public void SkipsHardwareFilterChains()
    {
        var injector = new NoirFilterInjector();
        var state = StateForPath("movie.mkv");

        var decision = injector.Inject(["-hwaccel", "vaapi", "-i", "movie.mkv", "out.m3u8"], state);

        Assert.False(decision.Modified);
        Assert.Equal("hardware-filter-chain-unsupported", decision.Reason);
    }

    private static NoirState StateForPath(string path)
    {
        return new NoirState
        {
            Enabled = true,
            ItemOverrides =
            [
                new NoirItemOverride
                {
                    ItemId = "abc",
                    MediaPath = path,
                    Mode = NoirOverrideMode.Preset,
                    PresetId = "classic-bw"
                }
            ]
        };
    }
}
