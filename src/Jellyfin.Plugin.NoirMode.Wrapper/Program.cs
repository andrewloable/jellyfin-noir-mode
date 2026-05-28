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
    options = WrapperOptions.FromEnvironment();
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine($"Noir Mode wrapper error: {ex.Message}");
    return 127;
}

NoirState state;
try
{
    state = NoirStateFile.Read(options.StateFilePath);
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
{
    Console.Error.WriteLine($"Noir Mode wrapper warning: failed to read state file '{options.StateFilePath}': {ex.Message}");
    state = new NoirState { Enabled = false };
}

var injector = new NoirFilterInjector();
var decision = injector.Inject(args, state);
Console.Error.WriteLine($"Noir Mode wrapper: reason={decision.Reason}; modified={decision.Modified}; applied={decision.Applied}; input={decision.InputPath ?? "<none>"}");

return await FfmpegProcess.RunAsync(options.RealFfmpegPath, decision.Arguments, CancellationToken.None).ConfigureAwait(false);
