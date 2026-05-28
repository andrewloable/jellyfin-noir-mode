using Jellyfin.Plugin.NoirMode.Core;

namespace Jellyfin.Plugin.NoirMode.Tests;

public sealed class FfmpegArgumentParserTests
{
    [Fact]
    public void FindsPrimaryInputPath()
    {
        var input = FfmpegArgumentParser.FindPrimaryInputPath(["-hide_banner", "-i", "/media/movie.mkv", "out.m3u8"]);

        Assert.Equal("/media/movie.mkv", input);
    }

    [Fact]
    public void DetectsVideoCopy()
    {
        Assert.True(FfmpegArgumentParser.UsesVideoStreamCopy(["-i", "in.mkv", "-c:v", "copy", "out.m3u8"]));
        Assert.True(FfmpegArgumentParser.UsesVideoStreamCopy(["-i", "in.mkv", "-codec:v:0", "copy", "out.m3u8"]));
    }

    [Fact]
    public void DetectsHardwareFilterChains()
    {
        Assert.False(FfmpegArgumentParser.UsesLikelyHardwareFilterChain(["-init_hw_device", "cuda=cu:0", "-i", "in.mkv", "-vf", "scale=1280:-2"]));
        Assert.True(FfmpegArgumentParser.UsesLikelyHardwareFilterChain(["-vf", "scale_qsv=w=1280:h=720"]));
    }
}
