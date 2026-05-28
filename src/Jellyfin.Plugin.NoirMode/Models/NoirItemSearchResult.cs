namespace Jellyfin.Plugin.NoirMode.Models;

public sealed class NoirItemSearchResult
{
    public string ItemId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? MediaPath { get; set; }
}
