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
    Console.Error.WriteLine($"NoirModeWrapper event=options-loaded configPath=\"{options.ConfigPath}\" usedConfigFile={options.UsedConfigFile} realFfmpegPath=\"{options.RealFfmpegPath}\" stateFilePath=\"{options.StateFilePath}\"");
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine($"NoirModeWrapper event=options-error message=\"{ex.Message}\"");
    return 127;
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
Console.Error.WriteLine($"NoirModeWrapper event=inject-decision reason={decision.Reason} modified={decision.Modified} applied={decision.Applied} inputPath=\"{decision.InputPath ?? "<none>"}\" argumentCount={args.Length}");

var exitCode = await FfmpegProcess.RunAsync(options.RealFfmpegPath, decision.Arguments, CancellationToken.None).ConfigureAwait(false);
Console.Error.WriteLine($"NoirModeWrapper event=real-ffmpeg-exit exitCode={exitCode}");
return exitCode;
