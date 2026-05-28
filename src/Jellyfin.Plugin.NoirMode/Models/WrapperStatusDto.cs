namespace Jellyfin.Plugin.NoirMode.Models;

public sealed class WrapperStatusDto
{
    public bool Enabled { get; set; }

    public string? RealFFmpegPath { get; set; }

    public string? WrapperPath { get; set; }

    public bool RealFFmpegExists { get; set; }

    public bool WrapperExists { get; set; }

    public string StateFilePath { get; set; } = string.Empty;

    public bool StateFileExists { get; set; }

    public DateTimeOffset? StateFileModifiedAt { get; set; }

    public string[] RequiredEnvironment { get; set; } = [];
}
