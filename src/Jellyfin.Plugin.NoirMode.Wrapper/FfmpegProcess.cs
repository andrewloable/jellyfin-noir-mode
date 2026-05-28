using System.Diagnostics;

namespace Jellyfin.Plugin.NoirMode.Wrapper;

public static class FfmpegProcess
{
    public static async Task<int> RunAsync(string realFfmpegPath, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        if (!File.Exists(realFfmpegPath))
        {
            Console.Error.WriteLine($"Noir Mode wrapper error: real FFmpeg not found at '{realFfmpegPath}'.");
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

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            Console.Error.WriteLine("Noir Mode wrapper error: failed to start real FFmpeg.");
            return 127;
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode;
    }
}
