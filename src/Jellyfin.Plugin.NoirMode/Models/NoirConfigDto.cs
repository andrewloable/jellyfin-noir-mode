using Jellyfin.Plugin.NoirMode.Core;

namespace Jellyfin.Plugin.NoirMode.Models;

public sealed class NoirConfigDto
{
    public bool Enabled { get; set; }

    public bool AllowCustomFilters { get; set; }

    public bool ForceTranscodeNoticeShown { get; set; }

    public string? RealFFmpegPath { get; set; }

    public string? WrapperPath { get; set; }

    public IReadOnlyCollection<NoirItemOverride> ItemOverrides { get; set; } = [];
}
