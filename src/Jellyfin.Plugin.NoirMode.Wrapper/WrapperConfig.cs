namespace Jellyfin.Plugin.NoirMode.Wrapper;

public sealed class WrapperConfig
{
    public string? RealFfmpegPath { get; set; }

    public string? RealFfprobePath { get; set; }

    public string? StateFilePath { get; set; }

    public bool? DebugLogging { get; set; }
}
