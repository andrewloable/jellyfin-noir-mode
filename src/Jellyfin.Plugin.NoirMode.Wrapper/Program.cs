using Jellyfin.Plugin.NoirMode.Core;
using Jellyfin.Plugin.NoirMode.Wrapper;

if (args.Length == 1 && args[0].Equals("--noir-probe", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("jellyfin-noir-wrapper");
    return 0;
}

WrapperOptions options;
try
{
    options = WrapperOptions.FromEnvironmentOrConfig();
    if (options.DebugLogging)
    {
        Console.Error.WriteLine($"NoirModeWrapper event=options-loaded configPath=\"{options.ConfigPath}\" usedConfigFile={options.UsedConfigFile} realFfmpegPath=\"{options.RealFfmpegPath}\" realFfprobePath=\"{options.RealFfprobePath}\" stateFilePath=\"{options.StateFilePath}\"");
    }
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine($"NoirModeWrapper event=options-error message=\"{ex.Message}\"");
    return 127;
}

if (IsFfprobeInvocation())
{
    if (options.DebugLogging)
    {
        Console.Error.WriteLine($"NoirModeWrapper event=ffprobe-delegate realFfprobePath=\"{options.RealFfprobePath}\" argumentCount={args.Length}");
    }

    return await FfmpegProcess.RunAsync(options.RealFfprobePath, args, options.DebugLogging, CancellationToken.None).ConfigureAwait(false);
}

NoirState state;
try
{
    state = NoirStateFile.Read(options.StateFilePath);
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
{
    Console.Error.WriteLine($"NoirModeWrapper event=state-read-error stateFilePath=\"{options.StateFilePath}\" error=\"{ex.Message}\"");
    state = new NoirState { Enabled = false };
}

var injector = new NoirFilterInjector();
var decision = injector.Inject(args, state);
if (options.DebugLogging)
{
    Console.Error.WriteLine($"NoirModeWrapper event=inject-decision reason={decision.Reason} modified={decision.Modified} applied={decision.Applied} inputPath=\"{decision.InputPath ?? "<none>"}\" argumentCount={args.Length}");
}

var exitCode = await FfmpegProcess.RunAsync(options.RealFfmpegPath, decision.Arguments, options.DebugLogging, CancellationToken.None).ConfigureAwait(false);
if (options.DebugLogging)
{
    Console.Error.WriteLine($"NoirModeWrapper event=real-ffmpeg-exit exitCode={exitCode}");
}

return exitCode;

static bool IsFfprobeInvocation()
{
    var processName = Environment.GetCommandLineArgs().FirstOrDefault();
    var executableName = string.IsNullOrWhiteSpace(processName)
        ? null
        : Path.GetFileNameWithoutExtension(processName);

    return string.Equals(executableName, "ffprobe", StringComparison.OrdinalIgnoreCase);
}
