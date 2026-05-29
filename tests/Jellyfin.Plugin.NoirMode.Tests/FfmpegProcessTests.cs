using Jellyfin.Plugin.NoirMode.Wrapper;
using System.Runtime.InteropServices;

namespace Jellyfin.Plugin.NoirMode.Tests;

public sealed class FfmpegProcessTests
{
    [Fact]
    public async Task MissingRealFfmpegReturnsProcessError()
    {
        var code = await FfmpegProcess.RunAsync(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing-ffmpeg"),
            [],
            false,
            CancellationToken.None);

        Assert.Equal(127, code);
    }

    [Fact]
    public async Task ExistingProcessReturnsExitCode()
    {
        var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe")
            : "/bin/sh";
        var args = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new[] { "/c", "exit", "0" }
            : ["-c", "exit 0"];

        var code = await FfmpegProcess.RunAsync(command, args, false, CancellationToken.None);

        Assert.Equal(0, code);
    }
}
