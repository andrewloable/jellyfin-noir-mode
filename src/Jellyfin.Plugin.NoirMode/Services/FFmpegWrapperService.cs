using System.Diagnostics;
using Jellyfin.Plugin.NoirMode.Models;

namespace Jellyfin.Plugin.NoirMode.Services;

public sealed class FFmpegWrapperService
{
    private readonly NoirPluginStateService _stateService;

    public FFmpegWrapperService(NoirPluginStateService stateService)
    {
        _stateService = stateService;
    }

    public WrapperStatusDto GetStatus(bool enabled, string? realFfmpegPath, string? wrapperPath)
    {
        var stateFilePath = _stateService.StateFilePath;
        var stateFile = new FileInfo(stateFilePath);

        return new WrapperStatusDto
        {
            Enabled = enabled,
            RealFFmpegPath = realFfmpegPath,
            WrapperPath = wrapperPath,
            RealFFmpegExists = !string.IsNullOrWhiteSpace(realFfmpegPath) && File.Exists(realFfmpegPath),
            WrapperExists = !string.IsNullOrWhiteSpace(wrapperPath) && File.Exists(wrapperPath),
            StateFilePath = stateFilePath,
            StateFileExists = stateFile.Exists,
            StateFileModifiedAt = stateFile.Exists ? stateFile.LastWriteTimeUtc : null,
            RequiredEnvironment =
            [
                $"NOIR_REAL_FFMPEG={realFfmpegPath ?? "<path-to-real-ffmpeg>"}",
                $"NOIR_STATE_FILE={stateFilePath}"
            ]
        };
    }

    public async Task<(bool Success, string Output)> ProbeAsync(string? wrapperPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(wrapperPath) || !File.Exists(wrapperPath))
        {
            return (false, "Wrapper path is missing or does not exist.");
        }

        var startInfo = new ProcessStartInfo(wrapperPath)
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        startInfo.ArgumentList.Add("--noir-probe");

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return (false, "Failed to start wrapper process.");
        }

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        return (process.ExitCode == 0, string.Concat(output, error).Trim());
    }
}
