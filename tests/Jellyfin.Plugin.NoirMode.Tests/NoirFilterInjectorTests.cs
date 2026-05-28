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

        var decision = injector.Inject(["-i", "movie.mkv", "-vf", "scale_cuda=1280:-2", "out.m3u8"], state);

        Assert.False(decision.Modified);
        Assert.Equal("hardware-filter-chain-unsupported", decision.Reason);
    }

    [Fact]
    public void AppendsJellyfinCudaDeviceCommandWithSoftwareVideoFilter()
    {
        var injector = new NoirFilterInjector();
        var state = StateForPath("/movies/the.legend.of.aang.the.last.airbender.mp4", "film-noir");

        var decision = injector.Inject(
            [
                "-analyzeduration",
                "200M",
                "-probesize",
                "1G",
                "-init_hw_device",
                "cuda=cu:0",
                "-filter_hw_device",
                "cu",
                "-i",
                "file:/movies/the.legend.of.aang.the.last.airbender.mp4",
                "-codec:v:0",
                "h264_nvenc",
                "-vf",
                "setparams=color_primaries=bt709:color_trc=bt709:colorspace=bt709,scale=trunc(min(max(iw\\,ih*a)\\,960)/2)*2:trunc(ow/a/2)*2,format=yuv420p",
                "-f",
                "hls",
                "out.m3u8"
            ],
            state);

        Assert.True(decision.Applied);
        Assert.Equal("appended-vf", decision.Reason);
        Assert.Contains(decision.Arguments, arg => arg.Contains("hue=s=0,eq=contrast=1.35:brightness=-0.03", StringComparison.Ordinal));
    }

    private static NoirState StateForPath(string path, string presetId = "classic-bw")
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
                    PresetId = presetId
                }
            ]
        };
    }
}
