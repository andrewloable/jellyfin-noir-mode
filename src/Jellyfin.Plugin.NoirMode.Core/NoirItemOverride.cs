namespace Jellyfin.Plugin.NoirMode.Core;

public sealed class NoirItemOverride
{
    public string ItemId { get; set; } = string.Empty;

    public string? MediaPath { get; set; }

    public string? NormalizedMediaPath { get; set; }

    public string? MediaPathHash { get; set; }

    public NoirOverrideMode Mode { get; set; } = NoirOverrideMode.Disabled;

    public string? PresetId { get; set; }
}
