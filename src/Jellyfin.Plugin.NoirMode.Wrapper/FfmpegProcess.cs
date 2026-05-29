using System.Diagnostics;

namespace Jellyfin.Plugin.NoirMode.Wrapper;

public static class FfmpegProcess
{
    public static async Task<int> RunAsync(string executablePath, IReadOnlyList<string> args, bool debugLogging, CancellationToken cancellationToken)
    {
        if (!File.Exists(executablePath))
        {
            Console.Error.WriteLine($"NoirModeWrapper event=real-tool-missing executablePath=\"{executablePath}\"");
            return 127;
        }

        var startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = false,
            RedirectStandardError = false,
            RedirectStandardOutput = false
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        if (debugLogging)
        {
            Console.Error.WriteLine($"NoirModeWrapper event=real-tool-start executablePath=\"{executablePath}\" argumentCount={args.Count}");
        }

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            Console.Error.WriteLine("NoirModeWrapper event=real-tool-start-failed");
            return 127;
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode;
    }
}
