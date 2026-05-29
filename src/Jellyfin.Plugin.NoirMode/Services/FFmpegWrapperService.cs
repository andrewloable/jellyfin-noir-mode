using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Jellyfin.Plugin.NoirMode.Models;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NoirMode.Services;

public sealed class FFmpegWrapperService
{
    private const string EncodingConfigurationKey = "encoding";

    private readonly IConfigurationManager _configurationManager;
    private readonly ILogger<FFmpegWrapperService> _logger;
    private readonly NoirPluginStateService _stateService;

    public FFmpegWrapperService(
        IConfigurationManager configurationManager,
        ILogger<FFmpegWrapperService> logger,
        NoirPluginStateService stateService)
    {
        _configurationManager = configurationManager;
        _logger = logger;
        _stateService = stateService;
    }

    public WrapperStatusDto GetStatus(bool enabled, string? realFfmpegPath, string? wrapperPath)
    {
        var currentFfmpegPath = GetCurrentFfmpegPath();
        wrapperPath = string.IsNullOrWhiteSpace(wrapperPath) ? DetectBundledWrapperPath() : wrapperPath;
        var stateFilePath = _stateService.StateFilePath;
        var stateFile = new FileInfo(stateFilePath);
        var wrapperConfigPath = GetWrapperConfigPath(wrapperPath);

        var status = new WrapperStatusDto
        {
            Enabled = enabled,
            CurrentJellyfinFFmpegPath = currentFfmpegPath,
            RealFFmpegPath = realFfmpegPath,
            WrapperPath = wrapperPath,
            JellyfinUsesWrapper = PathsEqual(currentFfmpegPath, wrapperPath),
            RealFFmpegExists = !string.IsNullOrWhiteSpace(realFfmpegPath) && File.Exists(realFfmpegPath),
            WrapperExists = !string.IsNullOrWhiteSpace(wrapperPath) && File.Exists(wrapperPath),
            WrapperConfigPath = wrapperConfigPath,
            WrapperConfigExists = !string.IsNullOrWhiteSpace(wrapperConfigPath) && File.Exists(wrapperConfigPath),
            StateFilePath = stateFilePath,
            StateFileExists = stateFile.Exists,
            StateFileModifiedAt = stateFile.Exists ? stateFile.LastWriteTimeUtc : null
        };

        _logger.LogInformation(
            "Noir Mode wrapper status: enabled={Enabled}; jellyfinUsesWrapper={JellyfinUsesWrapper}; currentFfmpegPath={CurrentFfmpegPath}; realFfmpegPath={RealFfmpegPath}; wrapperPath={WrapperPath}; wrapperExists={WrapperExists}; wrapperConfigPath={WrapperConfigPath}; wrapperConfigExists={WrapperConfigExists}; stateFilePath={StateFilePath}; stateFileExists={StateFileExists}",
            status.Enabled,
            status.JellyfinUsesWrapper,
            status.CurrentJellyfinFFmpegPath,
            status.RealFFmpegPath,
            status.WrapperPath,
            status.WrapperExists,
            status.WrapperConfigPath,
            status.WrapperConfigExists,
            status.StateFilePath,
            status.StateFileExists);

        return status;
    }

    public WrapperStatusDto ConfigureBundledWrapper(bool enabled, string? existingRealFfmpegPath)
    {
        var wrapperPath = DetectBundledWrapperPath();
        _logger.LogInformation(
            "Noir Mode bundled wrapper setup requested: detectedWrapperPath={WrapperPath}; existingRealFfmpegPath={ExistingRealFfmpegPath}",
            wrapperPath,
            existingRealFfmpegPath);

        if (string.IsNullOrWhiteSpace(wrapperPath) || !File.Exists(wrapperPath))
        {
            _logger.LogWarning("Noir Mode bundled wrapper setup failed: wrapper was not found for this server OS at {WrapperPath}", wrapperPath);
            return GetStatus(enabled, existingRealFfmpegPath, wrapperPath).WithMessage("Bundled wrapper was not found for this server OS.");
        }

        EnsureWrapperExecutable(wrapperPath);

        var currentFfmpegPath = GetCurrentFfmpegPath();
        _logger.LogInformation("Noir Mode current Jellyfin FFmpeg path before setup: {CurrentFfmpegPath}", currentFfmpegPath);
        var realFfmpegPath = existingRealFfmpegPath;
        if (!PathsEqual(currentFfmpegPath, wrapperPath) && !string.IsNullOrWhiteSpace(currentFfmpegPath))
        {
            realFfmpegPath = currentFfmpegPath;
        }

        if (string.IsNullOrWhiteSpace(realFfmpegPath) || PathsEqual(realFfmpegPath, wrapperPath))
        {
            _logger.LogWarning(
                "Noir Mode bundled wrapper setup failed: unable to determine real FFmpeg path. currentFfmpegPath={CurrentFfmpegPath}; wrapperPath={WrapperPath}; realFfmpegPath={RealFfmpegPath}",
                currentFfmpegPath,
                wrapperPath,
                realFfmpegPath);
            return GetStatus(enabled, realFfmpegPath, wrapperPath).WithMessage("Unable to determine the real FFmpeg path. Set it manually first, then run wrapper setup.");
        }

        try
        {
            WriteWrapperConfig(wrapperPath, realFfmpegPath, _stateService.StateFilePath);
            SetCurrentFfmpegPath(wrapperPath);
            _logger.LogInformation(
                "Noir Mode bundled wrapper setup completed: realFfmpegPath={RealFfmpegPath}; wrapperPath={WrapperPath}; stateFilePath={StateFilePath}",
                realFfmpegPath,
                wrapperPath,
                _stateService.StateFilePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _logger.LogError(
                ex,
                "Noir Mode bundled wrapper setup failed while writing config or updating Jellyfin FFmpeg path. realFfmpegPath={RealFfmpegPath}; wrapperPath={WrapperPath}",
                realFfmpegPath,
                wrapperPath);
            return GetStatus(enabled, realFfmpegPath, wrapperPath).WithMessage($"Failed to configure bundled wrapper: {ex.Message}");
        }

        var status = GetStatus(enabled, realFfmpegPath, wrapperPath);
        status.Message = "Jellyfin FFmpeg path now points to the bundled Noir Mode wrapper. Restart Jellyfin before testing playback.";
        return status;
    }

    public WrapperStatusDto RestoreRealFfmpeg(bool enabled, string? realFfmpegPath, string? wrapperPath)
    {
        _logger.LogInformation(
            "Noir Mode wrapper rollback requested: realFfmpegPath={RealFfmpegPath}; wrapperPath={WrapperPath}",
            realFfmpegPath,
            wrapperPath);

        if (string.IsNullOrWhiteSpace(realFfmpegPath) || !File.Exists(realFfmpegPath))
        {
            _logger.LogWarning("Noir Mode wrapper rollback failed: saved real FFmpeg path is missing or does not exist at {RealFfmpegPath}", realFfmpegPath);
            return GetStatus(enabled, realFfmpegPath, wrapperPath).WithMessage("Saved real FFmpeg path is missing or does not exist.");
        }

        try
        {
            SetCurrentFfmpegPath(realFfmpegPath);
            _logger.LogInformation("Noir Mode wrapper rollback completed: Jellyfin FFmpeg path restored to {RealFfmpegPath}", realFfmpegPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _logger.LogError(ex, "Noir Mode wrapper rollback failed while restoring Jellyfin FFmpeg path to {RealFfmpegPath}", realFfmpegPath);
            return GetStatus(enabled, realFfmpegPath, wrapperPath).WithMessage($"Failed to restore real FFmpeg path: {ex.Message}");
        }

        var status = GetStatus(enabled, realFfmpegPath, wrapperPath);
        status.Message = "Jellyfin FFmpeg path was restored to the real FFmpeg binary. Restart Jellyfin before testing playback.";
        return status;
    }

    public async Task<(bool Success, string Output)> ProbeAsync(string? wrapperPath, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Noir Mode wrapper probe requested: wrapperPath={WrapperPath}", wrapperPath);

        if (string.IsNullOrWhiteSpace(wrapperPath) || !File.Exists(wrapperPath))
        {
            _logger.LogWarning("Noir Mode wrapper probe failed: wrapper path is missing or does not exist at {WrapperPath}", wrapperPath);
            return (false, "Wrapper path is missing or does not exist.");
        }

        EnsureWrapperExecutable(wrapperPath);

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
            _logger.LogWarning("Noir Mode wrapper probe failed: process did not start for {WrapperPath}", wrapperPath);
            return (false, "Failed to start wrapper process.");
        }

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var result = string.Concat(output, error).Trim();
        _logger.LogInformation(
            "Noir Mode wrapper probe completed: wrapperPath={WrapperPath}; exitCode={ExitCode}; output={Output}",
            wrapperPath,
            process.ExitCode,
            result);

        return (process.ExitCode == 0, result);
    }

    public string? DetectBundledWrapperPath()
    {
        var pluginPath = Plugin.Instance?.AssemblyFilePath;
        if (string.IsNullOrWhiteSpace(pluginPath))
        {
            _logger.LogWarning("Noir Mode wrapper detection failed: plugin assembly path is unavailable.");
            return null;
        }

        var pluginDirectory = Path.GetDirectoryName(pluginPath);
        if (string.IsNullOrWhiteSpace(pluginDirectory))
        {
            _logger.LogWarning("Noir Mode wrapper detection failed: plugin directory is unavailable for assembly path {PluginPath}", pluginPath);
            return null;
        }

        var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "jellyfin-noir-wrapper.exe" : "jellyfin-noir-wrapper";
        var runtime = GetRuntimeIdentifier();
        if (runtime is null)
        {
            _logger.LogWarning("Noir Mode wrapper detection failed: unsupported server OS or architecture {OSDescription} {Architecture}.", RuntimeInformation.OSDescription, RuntimeInformation.ProcessArchitecture);
            return null;
        }

        var wrapperPath = Path.Combine(pluginDirectory, "wrapper", runtime, executableName);
        _logger.LogDebug(
            "Noir Mode wrapper detection: pluginDirectory={PluginDirectory}; runtime={Runtime}; wrapperPath={WrapperPath}",
            pluginDirectory,
            runtime,
            wrapperPath);
        return wrapperPath;
    }

    private string? GetCurrentFfmpegPath()
    {
        dynamic encodingOptions = _configurationManager.GetConfiguration(EncodingConfigurationKey);
        return encodingOptions.EncoderAppPath as string
            ?? encodingOptions.EncoderAppPathDisplay as string;
    }

    private void SetCurrentFfmpegPath(string ffmpegPath)
    {
        dynamic encodingOptions = _configurationManager.GetConfiguration(EncodingConfigurationKey);
        encodingOptions.EncoderAppPath = ffmpegPath;
        encodingOptions.EncoderAppPathDisplay = ffmpegPath;
        _configurationManager.SaveConfiguration(EncodingConfigurationKey, encodingOptions);
    }

    private void WriteWrapperConfig(string wrapperPath, string realFfmpegPath, string stateFilePath)
    {
        var wrapperConfigPath = GetWrapperConfigPath(wrapperPath)
            ?? throw new InvalidOperationException("Unable to determine wrapper config path.");
        var realFfprobePath = GetSiblingToolPath(realFfmpegPath, "ffprobe");
        var json = JsonSerializer.Serialize(new
        {
            realFfmpegPath,
            realFfprobePath,
            stateFilePath,
            debugLogging = false
        }, new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(wrapperConfigPath, json);
        _logger.LogInformation(
            "Noir Mode wrapper config written: wrapperConfigPath={WrapperConfigPath}; realFfmpegPath={RealFfmpegPath}; realFfprobePath={RealFfprobePath}; stateFilePath={StateFilePath}",
            wrapperConfigPath,
            realFfmpegPath,
            realFfprobePath,
            stateFilePath);
    }

    private static string? GetWrapperConfigPath(string? wrapperPath)
    {
        if (string.IsNullOrWhiteSpace(wrapperPath))
        {
            return null;
        }

        var wrapperDirectory = Path.GetDirectoryName(wrapperPath);
        return string.IsNullOrWhiteSpace(wrapperDirectory)
            ? null
            : Path.Combine(wrapperDirectory, "jellyfin-noir-wrapper.json");
    }

    private void EnsureWrapperExecutable(string wrapperPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        foreach (var path in GetWrapperExecutablePaths(wrapperPath))
        {
            if (!File.Exists(path))
            {
                _logger.LogWarning("Noir Mode wrapper executable permission check skipped: companion executable is missing at {WrapperPath}", path);
                continue;
            }

            var mode = File.GetUnixFileMode(path);
            var executableMode = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
            if ((mode & executableMode) == executableMode)
            {
                continue;
            }

            File.SetUnixFileMode(path, mode | executableMode);
            _logger.LogInformation("Noir Mode wrapper executable permissions set: wrapperPath={WrapperPath}", path);
        }
    }

    private static string? GetRuntimeIdentifier()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win-x64";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux-x64";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
        }

        return null;
    }

    private static bool PathsEqual(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        return string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    private static IEnumerable<string> GetWrapperExecutablePaths(string wrapperPath)
    {
        yield return wrapperPath;

        var directory = Path.GetDirectoryName(wrapperPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            yield break;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            yield return Path.Combine(directory, "ffprobe.exe");
            yield return Path.Combine(directory, "ffprobe.Wrapper.exe");
        }
        else
        {
            yield return Path.Combine(directory, "ffprobe");
            yield return Path.Combine(directory, "ffprobe.Wrapper");
        }
    }

    private static string GetSiblingToolPath(string toolPath, string toolName)
    {
        var directory = Path.GetDirectoryName(toolPath);
        var extension = Path.GetExtension(toolPath);
        var fileName = string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase)
            ? $"{toolName}.exe"
            : toolName;

        return string.IsNullOrWhiteSpace(directory)
            ? fileName
            : Path.Combine(directory, fileName);
    }
}

internal static class WrapperStatusExtensions
{
    public static WrapperStatusDto WithMessage(this WrapperStatusDto status, string message)
    {
        status.Message = message;
        return status;
    }
}
