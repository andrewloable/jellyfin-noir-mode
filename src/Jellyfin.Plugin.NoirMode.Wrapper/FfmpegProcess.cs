using System.Diagnostics;

namespace Jellyfin.Plugin.NoirMode.Wrapper;

public static class FfmpegProcess
{
    public static async Task<int> RunAsync(string realFfmpegPath, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        if (!File.Exists(realFfmpegPath))
        {
            Console.Error.WriteLine($"NoirModeWrapper event=real-ffmpeg-missing realFfmpegPath=\"{realFfmpegPath}\"");
            return 127;
        }

        var startInfo = new ProcessStartInfo(realFfmpegPath)
        {
            UseShellExecute = false,
            RedirectStandardError = false,
            RedirectStandardOutput = false
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        Console.Error.WriteLine($"NoirModeWrapper event=real-ffmpeg-start realFfmpegPath=\"{realFfmpegPath}\" argumentCount={args.Count}");
        using var process = Process.Start(startInfo);
        if (process is null)
        {
            Console.Error.WriteLine("NoirModeWrapper event=real-ffmpeg-start-failed");
            return 127;
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode;
    }
}
