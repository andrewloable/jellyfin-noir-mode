namespace Jellyfin.Plugin.NoirMode.Core;

public sealed class NoirState
{
    public int SchemaVersion { get; set; } = 1;

    public bool Enabled { get; set; }

    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<NoirItemOverride> ItemOverrides { get; set; } = [];
}
